using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class StageManager : Singleton<StageManager>, IValidatable
{
    const int c_maxChapter = 2;
    const int c_maxStage = 2;

    [SerializeField]
    bool m_stopStageStart = true;

    LoadData_Stage m_loadData;
    public LoadData_Stage data => m_loadData;
    List<Character_Enemy> m_enemyList = new();

    StageComponent m_stage;

    long m_tickStart;
    CancellationTokenSource m_cts;

    public bool isStageFailed { get; set; }
    public bool isClearFirstStage => m_loadData.level > 1;

    protected override void OnAwake()
    {
        if (BossRaidWorker.instance.isRunning == false)
        {
            if (PPWorker.HasKey(PlayerPrefsType.CHAPTER_STAGE_INFO))
                m_loadData = PPWorker.Get<LoadData_Stage>(PlayerPrefsType.CHAPTER_STAGE_INFO);
            else
            {
                m_loadData = new()
                {
                    level = 1,
                    chapterNumber = 1,
                    stageNumber = 1,
                };
                SaveData();
            }
        }

        for (int i = 0; i < m_element.chapter.childCount; i++)
            Destroy(m_element.chapter.GetChild(i).gameObject);
    }

    protected override void OnDestroy()
    {
        m_cts = m_cts.ReleaseCTS();
        OnDestroy_BossRaid();
        OnDestroy_DailyDungeon();
        OnDestroy_StoryMode();

        base.OnDestroy();
    }

    public async UniTask TestDevSelectAsync()
    {
        m_stopStageStart = true;
        var resultIdx = await PopupManager.instance.OpenTalkSelectAsync(
            "개발용으로 진행할거야.",
            "멈춤 없이 정상적으로 진행하자."
            );

        m_stopStageStart = resultIdx == 1;
    }

    void SaveData()
        => PPWorker.Set(PlayerPrefsType.CHAPTER_STAGE_INFO, m_loadData);

    AsyncOperationHandle<GameObject> m_handlerStage;
    public async UniTask<bool> LoadStageAsync()
    {
        // 챕터 초기화
        for (int i = 0; i < m_element.chapter.childCount; i++)
            Destroy(m_element.chapter.GetChild(i).gameObject);

        if (m_stage != null)
        {
            m_handlerStage.Release();
            m_stage = null;
        }
        string key = $"Stage/{m_loadData.chapterNumber}_{m_loadData.stageNumber}.prefab";

        long tickStart = m_tickStart;
        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            if (tickStart != m_tickStart)
                return;

            //무조건 하나야 이건
            foreach (var s in _result)
            {
                m_handlerStage = s.Value;
                m_stage = Instantiate(m_handlerStage.Result, m_element.chapter).GetComponent<StageComponent>();
                m_stage.SetData(m_loadData);
                m_stage.name = s.Key;
            }
        }, null, key);

        return m_stage == null;
    }

    public async UniTask StartStageAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var ctsToken = m_cts.Token;

        if (m_stage == null || m_stage.IsNow(m_loadData) == false)
            await LoadStageAsync();

        TeamManager.instance.StartStage();
        bool isDisableStart = false;

        var tickStart = m_tickStart = DateTime.Now.Ticks;

        while (true)
        {
#if UNITY_EDITOR
            m_element.chapter.name = $"Chapter_{m_loadData.chapterNumber}";
#endif

            var phases = new Queue<Transform>(m_stage.element.phase.Cast<Transform>());

            foreach (var p in phases)
                p.gameObject.SetActive(false);

            MapManager.instance.FadeDimm(false);
            Signal.instance.StartStage.Emit(m_loadData);

            while (phases.Count > 0)
            {
                var phaseIdx = m_stage.element.phase.childCount - phases.Count;
                var phase = phases.Dequeue();
                var isLastPhase = phases.Count == 0;

                // 보스 대기중이라면 
                if (m_loadData.isBossWait == true && isLastPhase == true)
                    continue;

                if (isDisableStart == false)
                {
                    PopupManager.instance.ShowDimm(false);
                    isDisableStart = true;
                }

                bool isFlip = TeamManager.instance.mainHero.transform.position.x > 0;
                var ctsCloseToken = m_stage.StartPhase(phaseIdx, isFlip);

                m_enemyList.Clear();

                while (phase.childCount > 0)
                {
                    var e = phase.GetChild(0).GetComponent<Character_Enemy>();
                    e.gameObject.layer = m_element.indexLayerEnemy;
                    e.SetHeroData(e.name);
                    e.transform.SetParent(MapManager.instance.element.pEnemy);
                    e.move.SetFlip(isFlip);
                    e.SetColorParts(Color.white);
                    e.Respawn();

                    var scale = e.transform.localScale;
                    if (scale.x < 0)
                    {
                        scale.x *= -1;
                        e.transform.localScale = scale;
                    }

                    m_enemyList.Add(e);
                }

#if UNITY_EDITOR
                if (m_stopStageStart == true)
                    await UniTask.WaitUntil(() => m_stopStageStart == false || Input.GetKey(KeyCode.Backspace));
#endif

                Signal.instance.StartPhase.Emit(m_stage.element.phase.childCount - phases.Count);
                TeamManager.instance.StartPhase(isFlip);
                SetState(CharacterStateType.Wait);

                // 클리어 할 때까지 대기
                bool isClear = false;
                isStageFailed = false;
                while (isClear == false && isStageFailed == false)
                {
                    isClear = true;
                    for (int i = 0; i < m_enemyList.Count; i++)
                    {
                        if (m_enemyList[i].isLive)
                        {
                            isClear = false;
                            break;
                        }
                    }
                    await UniTask.WaitForEndOfFrame(cancellationToken: ctsToken);
                }
                // 클리어 할 때까지 대기

                // 실패 했다면?
                if (isStageFailed == true)
                {
                    if (m_loadData.isBossWait == true)
                    {
                        if (m_loadData.level > 1)
                            m_loadData.level--;
                    }
                    else
                    {
                        PopupManager.instance.AlertShow("잠시 후퇴!!_전량상_후퇴일뿐입니다.");
                        m_loadData.isBossWait = true;
                    }
                    break;
                }

                // 클리어하면 원상 복구 시킨다.
                for (int i = 0; i < m_enemyList.Count; i++)
                    m_enemyList[i].transform.SetParent(phase);

                // 끝났을 때 연출해준다.

                // 스토리가 있는지 여부 확인한다.
                if (m_loadData.isBossWait == false && m_loadData.level == 1)
                {
                    // TODO
                    // 시나리오 아이콘에 레드닷을 달아주자.
                    // 분리하는 걸로 갈거야.
                    //await ScenarioManager.instance.StartAsync(phaseIdx, false);
                }

                //보스 잡은거면 그냥 조금있다가 다음으로 넘어가면 됨
                if (isLastPhase == true)
                    await UniTask.WaitForSeconds(1f, cancellationToken: ctsToken);
                else
                {
                    // 쓰러지고 5초후에 초기화 해주자
                    Utils.AfterSecond(() =>
                    {
                        m_stage?.ClosePhase(phaseIdx);
                    }, 5f, ctsCloseToken);

                    await UniTask.WaitForSeconds(0.5f, cancellationToken: ctsToken);
                }

                TeamManager.instance.PhaseFinished();

                // 대기중이라면 계속 돌려막아야 해.
                if (m_loadData.isBossWait == true && phases.Count == 1)
                    phases = new Queue<Transform>(m_stage.element.phase.Cast<Transform>());
            }

            // 보스까지 다 깻으면
            if (m_loadData.isBossWait == false)
            {
                MapManager.instance.FadeDimm(true, _token: m_cts);
                await UniTask.WaitForSeconds(0.2f, cancellationToken: ctsToken);

                m_loadData.stageNumber++;
                await LoadStageAsync();

                if (m_stage == null)
                {
                    m_loadData.chapterNumber++;
                    m_loadData.stageNumber = 1;

                    await LoadStageAsync();
                }

                if (m_stage == null)
                {
                    m_loadData.chapterNumber = 1;
                    await LoadStageAsync();
                    m_loadData.level++;
                }

                SaveData();

                //스테이지를 새로 시작하기 떄문에 위치 조정을 해준다.
                TeamManager.instance.StartStage();
            }
            else if (isStageFailed == true)
            {
                await MapManager.instance.FadeDimmAsync(true, 2f, _token: m_cts);
                RestartStage();
                SaveData();
                return;
            }

            if (tickStart != m_tickStart)
                break;
            // CLEAR STAGE

        }
    }

    public void RestartStage(bool _isRestart = true)
    {
        m_cts = m_cts.ReleaseCTS();

        MapManager.instance.FadeDimm(true, 0f);

        for (var i = 0; i < m_enemyList.Count; i++)
            Destroy(m_enemyList[i].gameObject);
        m_enemyList.Clear();

        EffectWorker.instance.ResetEffect();

        TeamManager.instance.RestartStage();

        if (m_stage != null)
        {
            m_handlerStage.Release();

            Destroy(m_stage.gameObject);
            m_stage = null;
        }

        if (_isRestart == true)
            StartStageAsync().Forget();
    }

    public void RechallengeBoss()
    {
        m_loadData.isBossWait = false;
        SaveData();
        RestartStage();
    }

    public IReadOnlyList<CharacterComponent> enemyList => m_enemyList.ToList();
    public IReadOnlyList<CharacterComponent> liveEnemyList => m_enemyList.FindAll(_x => _x.isLive).ToList();

    public Vector3 centerPosition
    {
        get
        {
            var enemies = liveEnemyList
                .SortBy(_x => (_x.transform.position - TeamManager.instance.mainHero.transform.position).sqrMagnitude).Take(4).ToList();

            Vector3 sum = Vector3.zero;

            for (int i = 0; i < enemies.Count; i++)
                sum += enemies[i].transform.position;

            return sum / enemies.Count;
        }
    }

    public CharacterComponent GetNearestEnemy(Vector3 _position)
    {
        CharacterComponent result = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < m_enemyList.Count; i++)
        {
            var enemy = m_enemyList[i];

            if (enemy == null || enemy.isLive == false)
                continue;

            float sqrDist = (enemy.transform.position - _position).sqrMagnitude;
            if (sqrDist < minDist)
            {
                minDist = sqrDist;
                result = enemy;
            }
        }

        return result;
    }

    public void SetState(CharacterStateType _stateType)
    {
        foreach (var enemy in m_enemyList)
        {
            if (enemy.isLive)
                enemy.SetState(_stateType);
        }
    }

    public void BossKillAllDieEnemy()
    {
        foreach (var enemy in m_enemyList)
        {
            if (enemy.isBoss == false)
                enemy.OnDamage(null, enemy.stat.healthMax);
        }
    }

    public void AddEnemyList(Character_Enemy _enemy)
        => m_enemyList.Add(_enemy);
    public void ClearEnemyList()
        => m_enemyList.Clear();

    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform chapter;
        public int indexLayerEnemy;

        public void Initialize(Transform _transform)
        {
            chapter = _transform.Find("Chapter");
            indexLayerEnemy = LayerMask.NameToLayer("Enemy");
        }
    }

    public struct LoadData_Stage
    {
        public int level;
        public int chapterNumber;
        public int stageNumber;
        public bool isBossWait;

        public string GetKey_Scenario(int _phaseIdx, bool _isStart)
            => $"{chapterNumber}_{stageNumber}_{_phaseIdx + 1}_{DataManager.userInfo.region.ToString().ToUpper()}_{(_isStart ? "START" : "END")}";

        //public string GetKey_Tutorial(int _phaseIdx, bool _isStart)
        //    => $"{chapterNumber}.{stageNumber}.{_phaseIdx + 1}_{(_isStart ? "START" : "END")}";

        public bool IsEquals(LoadData_Stage _stage)
        {
            if (level != _stage.level ||
                chapterNumber != _stage.chapterNumber ||
                stageNumber != _stage.stageNumber)
                return false;
            return true;
        }
    }
}