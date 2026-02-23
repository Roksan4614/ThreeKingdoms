using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeamManager : Singleton<TeamManager>, IValidatable
{
    Dictionary<TeamPositionType, CharacterComponent> m_member = new();

    public CharacterStateType teamState { get; private set; } = CharacterStateType.Wait;
    public IReadOnlyDictionary<TeamPositionType, CharacterComponent> members => m_member;

    public Team_HeroInfo m_heroInfo;
    public Dictionary<TeamPositionType, Vector3> m_dbPostion = new();

    //public IReadOnlyList<CharacterComponent> myHero
    //    => m_member.Values.ToList();

    protected override void OnAwake()
    {
        for (int i = 0; i < transform.childCount; i++)
            Destroy(transform.GetChild(i).gameObject);

        for (int i = 0; i < transform.childCount; i++)
        {
            m_dbPostion.Add(TeamPositionType.NONE + i + 1,
                transform.GetChild(i).position - m_element.startPos);
        }
    }

    private void Start()
    {
        m_heroInfo = new(m_element.heroInfo);
        Signal.instance.UpdateHP.connect = m_heroInfo.UpdateHP;
    }

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
#endif

    public async UniTask SpawnUpdateAsync()
    {
        m_heroInfo.DisableAll();

        List<HeroInfoData> myHero = DataManager.userInfo.myHero.Where(x => x.isBatch).ToList();

        var remove = m_member
            .Where(x => myHero.Any(y => y.key == x.Value.data.key) == false)
            .Select(x => x.Key).ToList();

        for (int i = 0; i < remove.Count; i++)
        {
            m_member[remove[i]].DestroyCharacter();
            m_member.Remove(remove[i]);
        }

        for (int i = 0; i < myHero.Count; i++)
        {
            if (m_member.Values.Any(x => x.data.key.Equals(myHero[i].key)))
                continue;

            var heroCharacter = (await AddressableManager.instance.GetHeroCharacter(myHero[i].skin)).GetComponent<CharacterComponent>();

            var hero = Instantiate(heroCharacter, MapManager.instance.element.pHero);
            hero.SetHeroData(myHero[i].key);
            hero.name = myHero[i].key;

            m_member.Add(TeamPositionType.MAX + i, hero);
        }

        SetTeamPosition(m_member.Values.ToList());
    }

    public void SetTeamPosition(List<CharacterComponent> _members)
    {
        m_member.Clear();

        var mainIndex = _members.FindIndex(x => x.data.key.Equals(DataManager.userInfo.myHero[0].key));
        // 일단 주장은 무조건 전방으로 해보자
        m_member.Add(DataManager.option.mainTeamPosition, _members[mainIndex]);
        _members.RemoveAt(mainIndex);

        // 세명이면 전방이후방을 정해야 한다.
        if (_members.Count == 3)
        {
            CharacterComponent character = null;
            // 주장이 전방일 경우 궁장,책사 중 공격력이 가장 강한 영웅이 뒤로 간다
            if (DataManager.option.mainTeamPosition == TeamPositionType.Front)
            {
                var backs = _members.Where(x => x.data.classType == HeroClassType.Strategist).ToList();

                if (backs.Count == 0)
                    backs = _members.Where(x => x.data.classType == HeroClassType.Archer).ToList();

                character = backs.Count > 0
                    ? backs.OrderByDescending(x => x.data.attackPower).First()
                    : _members.OrderBy(x => x.data.healthMax).First();

                m_member.Add(TeamPositionType.Back, character);
            }
            // 주장이 후방일 경우 용장이거나 체력이 가장 높은 영웅을 앞으로 보낸다
            else
            {
                var fronts = _members.Where(x => x.data.classType == HeroClassType.Champion).ToList();

                if (fronts.Count == 0)
                    fronts = _members;

                character = fronts.OrderByDescending(x => x.data.health).First();

                m_member.Add(TeamPositionType.Front, character);
            }

            _members.Remove(character);
        }

        //나머진 위에서 부터 채워주자
        for (int i = 0; i < _members.Count; i++)
            m_member.Add(TeamPositionType.Top + i, _members[i]);

        m_member = m_member.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

        int index = 0;

        foreach (var member in m_member)
        {
            member.Value.SetHeroData(member.Value.data.key);
            member.Value.SetTeamPosition(member.Key, m_dbPostion[TeamPositionType.Front] + m_dbPostion[member.Key]);
            member.Value.SetMain(0 == index++);
            member.Value.SetFaction(FactionType.Alliance);
        }

        Signal.instance.ConnectMainHero.Emit(mainHero);
        CameraManager.instance.SetCameraPosTarget();
        m_heroInfo.SetTeamPosition();
    }

    public float teamMoveSpeed => mainHero.data.moveSpeed;
    public CharacterComponent mainHero => m_member[DataManager.option.mainTeamPosition];


    public void RestartStage()
    {
        StartStage();

        teamState = CharacterStateType.Wait;
        foreach (var member in m_member.Values)
            member.Respawn();
        m_heroInfo.StartStage();
    }

    public void StartStage()
    {
        mainHero.transform.position = m_element.startPos;
        mainHero.move.SetFlip(true);
        m_heroInfo.StartStage();

        RepositionToMain(0, true);

        CameraManager.instance.SetCameraPosTarget();
    }

    public void StartPhase(bool _isFlip)
    {
        SetState(CharacterStateType.Wait);
        mainHero.move.SetFlip(_isFlip == false);

        RepositionToMain();

        teamState = CharacterStateType.SearchEnemy;
        foreach (var member in m_member.Values)
        {
            if (member.isLive == false)
                m_heroInfo.StopRespawn(member);

            member.Respawn();
            m_heroInfo.UpdateHP(member);
        }
    }

    public void PhaseFinished()
    {
        //mainHero.move.SetFlip(true);
        //RepositionToMain();
        SetState(CharacterStateType.Wait);
    }

    public void SetState(CharacterStateType _stateType)
    {
        foreach (var member in m_member.Values)
            member.SetState(_stateType);

        teamState = _stateType;
    }

    public Vector3 GetPositionByType(TeamPositionType _teamPosition)
    {
        if (m_member.ContainsKey(_teamPosition) == false)
            return Vector3.zero;

        return m_member.Values.First().transform.position - m_dbPostion[_teamPosition];
    }

    public CharacterComponent GetHero(TeamPositionType _teamPosition)
     => m_member.ContainsKey(_teamPosition) == false ? null : m_member[_teamPosition];

    public void RepositionToMain(float _duration = .5f, bool _isForce = false)
    {
        var main = mainHero;
        var startPosMain = main.transform.position;

        bool isFlip = main.move.isFlip;

        foreach (var member in m_member)
        {
            var hero = member.Value;

            if ((hero.isLive == false || hero.isMain == true) && _isForce == false)
                continue;

            hero.move.SetFlip(isFlip);

            var targetPos = m_dbPostion[member.Key];
            if (isFlip == false)
                targetPos.x *= -1;
            targetPos += main.transform.position;

            if (_duration == 0)
            {
                hero.transform.position = targetPos;
            }
            else
            {
                var sqr = Vector3.SqrMagnitude(targetPos - hero.transform.position);
                if (sqr > 1)
                {
                    hero.anim.Play(CharacterAnimType.Idle);
                    var tween = hero.transform.DOLocalMove(targetPos, _duration);

                    DateTime dt = DateTime.Now;
                    tween.OnUpdate(() =>
                    {
                        if (startPosMain != main.transform.position)
                        {
                            var targetPos = m_dbPostion[member.Key];
                            if (isFlip == false)
                                targetPos.x *= -1;
                            targetPos += main.transform.position;

                            tween.ChangeValues(hero.transform.position, targetPos, _duration - (float)(DateTime.Now - dt).TotalSeconds);
                        }
                    });
                }
            }
        }
    }

    public void SetRespawn(TeamPositionType _position)
    {
        m_member[_position].Respawn();
    }

    public CharacterComponent GetNearestHero(Vector3 _position)
    {
        CharacterComponent result = null;
        float minDist = float.MaxValue;

        foreach (var hero in m_member.Values)
        {
            if (hero.isLive == false)
                continue;

            float sqrDist = (hero.transform.position - _position).sqrMagnitude;
            if (sqrDist < minDist)
            {
                minDist = sqrDist;
                result = hero;
            }
        }

        return result;
    }

    public bool IsAllDie()
    {
        var members = m_member.Values.ToList();
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i].isLive == true)
                return false;
        }

        m_heroInfo.StopRespawn();

        StageManager.instance.isStageFailed = true;
        return true;
    }

    [SerializeField]
    //[SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public Vector3 startPos;

        public Transform heroInfo;

        public void Initialize(Transform _transform)
        {
            startPos = _transform.GetChild(0).position;
            heroInfo = GameObject.Find("Canvas/HeroInfo").transform;
        }
    }
}
