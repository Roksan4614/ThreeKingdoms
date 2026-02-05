using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.U2D.Animation;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.EventSystems.EventTrigger;

public class TeamManager : MonoSingleton<TeamManager>
{
    Dictionary<TeamPositionType, Vector3> m_dbPostion = new();
    Dictionary<TeamPositionType, CharacterComponent> m_member = new();

    public Team_HeroInfo heroInfo { get; private set; }

    public CharacterStateType teamState { get; private set; } = CharacterStateType.Wait;
    public IReadOnlyDictionary<TeamPositionType, CharacterComponent> members => m_member;

    protected override void OnAwake()
    {
        for (int i = 0; i < transform.childCount; i++)
            m_dbPostion.Add(TeamPositionType.None + i + 1,
                transform.GetChild(i).position - transform.GetChild(0).position);

        heroInfo = new(ControllerManager.instance.transform.parent.Find("HeroInfo"));
    }

    public void SetTeamPosition(List<CharacterComponent> _members)
    {
        m_member.Clear();

        // 주장이 책사, 궁장이면 후방위치한다
        //m_member.Add(_members[0].data.classType switch
        //{
        //    CharacterClassType.Archer => TeamPositionType.Back,
        //    CharacterClassType.Strategist => TeamPositionType.Back,
        //    _ => TeamPositionType.Front
        //}, _members[0]);

        // 일단 주장은 무조건 전방으로 해보자
        m_member.Add(TeamPositionType.Front, _members[0]);
        _members.RemoveAt(0);

        // 세명이면 전방이후방을 정해야 한다.
        if (_members.Count == 3)
        {
            CharacterComponent character = null;
            // 주장이 전방일 경우 궁장,책사 중 공격력이 가장 강한 영웅이 뒤로 간다
            //if (m_member.ContainsKey(TeamPositionType.Front) == true)
            {
                var backs = _members.Where(x => x.data.classType == CharacterClassType.Strategist).ToList();

                if (backs.Count == 0)
                {
                    backs = _members.Where(x => x.data.classType == CharacterClassType.Archer).ToList();

                    // 두 클래스 다 없으면 나머지 중에서 고르자
                    if (backs.Count == 0)
                        backs = _members;
                }

                character = backs.OrderByDescending(x => x.data.attackPower).First();

                m_member.Add(TeamPositionType.Back, character);
            }
            // 주장이 후방일 경우 용장이거나 체력이 가장 높은 영웅을 앞으로 보낸다
            //else
            //{
            //    var fronts = _members.Where(x => x.data.classType == CharacterClassType.Champion).ToList();

            //    if (fronts.Count == 0)
            //    {
            //        fronts = _members;
            //    }

            //    character = fronts.OrderByDescending(x => x.data.health).First();

            //    m_member.Add(TeamPositionType.Front, character);
            //}

            _members.Remove(character);
        }

        //나머진 위에서 부터 채워주자
        for (int i = 0; i < _members.Count; i++)
            m_member.Add(TeamPositionType.Top + i, _members[i]);

        int index = 0;
        foreach (var member in m_member)
        {
            member.Value.SetTeamPosition(member.Key, mainHero.transform.position + m_dbPostion[member.Key]);
            member.Value.SetMain(0 == index++);
            member.Value.SetFaction(FactionType.Alliance);
        }

        heroInfo.SetTeamPosition();
    }

    public float teamMoveSpeed => mainHero.data.moveSpeed;
    public CharacterComponent mainHero => m_member.Values.First();

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

    public void RepositionToMain(float _duration = .5f)
    {
        var main = mainHero;
        var startPosMain = main.transform.position;

        bool isFlip = main.move.isFlip;

        foreach (var member in m_member)
        {
            var hero = member.Value;

            if (hero.isLive == false || hero.isMain == true)
                continue;

            hero.move.SetFlip(isFlip);

            var targetPos = m_dbPostion[member.Key];
            if (isFlip == false)
                targetPos.x *= -1;
            targetPos += main.transform.position;

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

}
