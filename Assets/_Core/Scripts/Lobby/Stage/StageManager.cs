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
    public LoadData_Stage data => m_loadData;
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
                chapterNumber = 1,
                stageNumber = 1,
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
        string key = $"Stage/{m_loadData.chapterNumber}_{m_loadData.stageNumber}.prefab";

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

                bool isFlip = TeamManager.instance.mainHero.transform.position.x > 0;
                var ctsClose = m_stage.StartPhase(phaseIdx, isFlip);

                m_enemyList.Clear();

                while (phase.childCount > 0)
                {
                    var e = phase.GetChild(0).GetComponent<Character_Enemy>();
                    e.gameObject.layer = m_element.indexLayerEnemy;
                    e.SetHeroData(e.name);
                    e.transform.SetParent(MapManager.instance.element.pEnemy);
                    e.move.SetFlip(isFlip);

                    var scale = e.transform.localScale;
                    if (scale.x < 0)
                    {
                        scale.x *= -1;
                        e.transform.localScale = scale;
                    }

                    m_enemyList.Add(e);
                }

                await UniTask.WaitUntil(() => Input.GetKey(KeyCode.A));

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
                    await UniTask.WaitForEndOfFrame(cancellationToken: m_cts.Token);
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
                        m_loadData.isBossWait = true;
                    break;
                }

                // 클리어하면 원상 복구 시킨다.
                for (int i = 0; i < m_enemyList.Count; i++)
                    m_enemyList[i].transform.SetParent(phase);

                //보스 잡은거면 그냥 조금있다가 다음으로 넘어가면 됨
                if (isLastPhase == true)
                    await UniTask.WaitForSeconds(1f, cancellationToken: m_cts.Token);
                else
                {
                    // 쓰러지고 5초후에 초기화 해주자
                    Utils.AfterSecond(() => m_stage.ClosePhase(phaseIdx), 5f, ctsClose);

                    await UniTask.WaitForSeconds(0.5f, cancellationToken: m_cts.Token);
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
                await UniTask.WaitForSeconds(0.2f, cancellationToken: m_cts.Token);

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

    public void RestartStage()
    {
        MapManager.instance.FadeDimm(true, 0f);

        for (var i = 0; i < m_enemyList.Count; i++)
            Destroy(m_enemyList[i].gameObject);
        m_enemyList.Clear();

        EffectWorker.instance.ResetEffect();

        TeamManager.instance.RestartStage();
        m_stage = null;

        StartStageAsync().Forget();
    }

    public void RechallengeBoss()
    {
        m_loadData.isBossWait = false;
        SaveData();
        RestartStage();
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

    public void BossKillAllDieEnemy()
    {
        foreach (var enemy in m_enemyList)
        {
            if (enemy.isBoss == false)
                enemy.OnDamage(enemy.data.healthMax);
        }
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

    public struct LoadData_Stage
    {
        public int level;
        public int chapterNumber;
        public int stageNumber;
        public bool isBossWait;
    }
}