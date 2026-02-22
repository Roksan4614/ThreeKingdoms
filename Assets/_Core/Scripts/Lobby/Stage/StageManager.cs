using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class StageManager : Singleton<StageManager>, IValidatable
{
    LoadData_Stage m_loadData;
    List<Character_Enemy> m_enemyList = new();

    StageComponent m_stage;

    const int c_maxChapter = 2;
    const int c_maxStage = 2;

    long m_tickStart;
    CancellationTokenSource m_cts;

    public bool isStageFailed { get; set; }

    protected override void OnAwake()
    {
        if (PPWorker.HasKey(PlayerPrefsType.CHAPTER_STAGE_INFO))
            m_loadData = PPWorker.Get<LoadData_Stage>(PlayerPrefsType.CHAPTER_STAGE_INFO);
        else
        {
            m_loadData = new()
            {
                level = 1,
                chapterIdx = 1,
                stageIdx = 1,
            };
            SaveData();
        }
    }

    void SaveData()
        => PPWorker.Set(PlayerPrefsType.CHAPTER_STAGE_INFO, m_loadData);

    public async UniTask<bool> LoadStageAsync()
    {
        // 챕터 초기화
        for (int i = 0; i < m_element.chapter.childCount; i++)
            Destroy(m_element.chapter.GetChild(i).gameObject);

        m_stage = null;
        string key = $"Stage/{m_loadData.chapterIdx}_{m_loadData.stageIdx}.prefab";

        long tickStart = m_tickStart;
        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            if (tickStart != m_tickStart)
                return;

            foreach (var s in _result)
            {
                m_stage = Instantiate(s.Value.Result, m_element.chapter).GetComponent<StageComponent>();
                m_stage.SetData(m_loadData);
                m_stage.name = s.Key;
                s.Value.Release();
            }
        }, null, key);

        return m_stage == null;
    }

    public async UniTask StartStageAsync()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
        }
        m_cts = new();

        if (m_stage == null || m_stage.IsNow(m_loadData) == false)
            await LoadStageAsync();

        TeamManager.instance.StartStage();
        var tickStart = m_tickStart = DateTime.Now.Ticks;
        while (true)
        {
#if UNITY_EDITOR
            m_element.chapter.name = $"Chapter_{m_loadData.chapterIdx}";
#endif

            var phases = new Queue<Transform>(m_stage.element.phase.Cast<Transform>());

            foreach (var p in phases)
                p.gameObject.SetActive(false);

            MapManager.instance.FadeDimm(false).Forget();

            while (phases.Count > 0)
            {
                var phase = phases.Dequeue();
                var isLastPhase = phases.Count == 0;

                // 보스 대기중이라면 
                if (m_loadData.isBossWait == true && isLastPhase == true)
                    continue;

                phase.gameObject.SetActive(true);

                // 위치 세팅한다
                var posMainHero = TeamManager.instance.mainHero.transform.position;
                var pos = phase.position;
                pos.x = posMainHero.x;
                phase.position = pos;

                bool isFlip = posMainHero.x > 0;
                if (isFlip == phase.localScale.x > 0)
                {
                    var scale = phase.localScale;
                    scale.x *= -1;
                    phase.localScale = scale;
                }

                m_enemyList.Clear();
                for (int i = 0; i < phase.childCount; i++)
                {
                    var e = phase.GetChild(i).GetComponent<Character_Enemy>();
                    e.gameObject.layer = m_element.indexLayerEnemy;
                    m_enemyList.Add(e);
                }

                var prevPosition = m_enemyList.Select(x => x.transform.position).ToList();

                foreach (var e in m_enemyList)
                {
                    e.SetHeroData(e.name);
                    if (m_loadData.isBossWait)
                        e.SetDebuffStat(0.1f);

                    if (e.transform.localScale.x < 0)
                    {
                        var scale = e.transform.localScale;
                        scale.x *= -1;
                        e.transform.localScale = scale;
                    }

                    e.move.SetFlip(isFlip);
                    e.SetState(CharacterStateType.Wait);
                }

                TeamManager.instance.PhaseStart(isFlip);
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
                    await UniTask.WaitForEndOfFrame(cancellationToken: m_cts.Token);
                }

                if (isStageFailed == true)
                {
                    if (m_loadData.isBossWait == true)
                    {
                        if (m_loadData.level > 1)
                            m_loadData.level--;
                    }
                    else
                        m_loadData.isBossWait = true;
                    break;
                }

                TeamManager.instance.PhaseFinished();

                // 클리어하면 원상 복구 시킨다.
                for (int i = 0; i < m_enemyList.Count; i++)
                    m_enemyList[i].transform.SetParent(phase);

                if (isLastPhase == true)
                    await UniTask.WaitForSeconds(1f, cancellationToken: m_cts.Token);
                else
                {
                    Utils.AfterSecond(() =>
                    {
                        if (phase != null)
                        {
                            for (int i = 0; i < phase.childCount; i++)
                            {
                                var e = phase.GetChild(i);
                                e.position = prevPosition[i];
                                var parts = e.Find("Character/Panel/Parts");
                                e.GetComponent<Collider2D>("Character").enabled = true;
                                parts.Find("Sub").gameObject.SetActive(true);
                                parts.Find("Weapon").gameObject.SetActive(true);
                            }

                            phase.gameObject.SetActive(false);
                        }
                    }, 5f, m_cts);

                    await UniTask.WaitForSeconds(0.5f, cancellationToken: m_cts.Token);
                }
            }

            // 보스까지 다 깻으면
            if (m_loadData.isBossWait == false)
            {
                MapManager.instance.FadeDimm(true, _token: m_cts).Forget();
                await UniTask.WaitForSeconds(0.2f, cancellationToken: m_cts.Token);

                m_loadData.stageIdx++;
                await LoadStageAsync();

                if (m_stage == null)
                {
                    m_loadData.chapterIdx++;
                    m_loadData.stageIdx = 1;

                    await LoadStageAsync();
                }

                if (m_stage == null)
                {
                    m_loadData.chapterIdx = 1;
                    await LoadStageAsync();
                    m_loadData.level++;
                }

                SaveData();

                //스테이지를 새로 시작하기 떄문에 위치 조정을 해준다.
                TeamManager.instance.StartStage();
            }
            else if (isStageFailed == true)
            {
                await MapManager.instance.FadeDimm(true, 2f, _token: m_cts);
                RestartStage();
                SaveData();
                return;
            }

            if (tickStart != m_tickStart)
                break;
            // CLEAR STAGE

        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
        }
    }

    void RestartStage()
    {
        MapManager.instance.FadeDimm(true, 0f).Forget();

        for (var i = 0; i < m_enemyList.Count; i++)
            Destroy(m_enemyList[i].gameObject);
        m_enemyList.Clear();

        EffectWorker.instance.ResetEffect();

        TeamManager.instance.RestartStage();
        m_stage = null;
        StartStageAsync().Forget();
    }

    public IReadOnlyList<CharacterComponent> enemyList => m_enemyList.Where(_x => _x.isLive).ToList();

    public Vector3 centerPosition
    {
        get
        {
            var enemies = enemyList
                .OrderBy(_x => (_x.transform.position - TeamManager.instance.mainHero.transform.position).sqrMagnitude).Take(4).ToList();

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

            if (enemy.isLive == false)
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
            enemy.SetState(_stateType);
    }

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
}

public struct LoadData_Stage
{
    public int level;
    public int chapterIdx;
    public int stageIdx;
    public bool isBossWait;
}