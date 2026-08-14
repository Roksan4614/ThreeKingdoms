using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Events;

public class Data_DailyDungeon
{
    DailyDungeonData m_data;
    public DailyDungeonData data => m_data;

    List<DailyDungeonRecordData> m_recordData;
    const string c_recordKey = "pp_daily_dungeon_record";

    public WeekdayType enterWeekday
    {
        get => m_data.enterWeekday;
        set => m_data.enterWeekday = value;
    }

    public bool isRunning => m_data.enterWeekday > WeekdayType.None;
    public TableDailyDungeonBossData bossData => TableManager.dailyDungeonBoss.Get(m_data.enterWeekday);
    public GradeType curGradeType
    {
        get => m_data.curGradeType;
        set => m_data.curGradeType = value;
    }

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_recordData = PPWorker.Get<List<DailyDungeonRecordData>>(c_recordKey);
        if (m_recordData == null)
            m_recordData = new();

        m_data.Default();
    }

    public async UniTask<bool> ShowAdsAsync()
    {
        if (m_data.adCount == 0)
            return false;

        if (await AdsManager.instance.ShowAsync())
        {
            m_data.adCount--;
            m_data.count++;
            return true;
        }

        return false;
    }

    // Åä¹ú
    public async UniTask SweepAsync(WeekdayType _weekType, UnityAction _onUpdate)
    {
        DailyDungeonRecordData recordData = DataManager.dailyDungeon.GetRecordGradeType(_weekType);
        recordData.percent = 0;

        if (recordData.gradeType > GradeType.Normal)
        {
            TutorialManager.instance.Action_DailyDungeonPlay();

            m_data.count--;
            _onUpdate();

            recordData.isSweep = true;
            await PopupManager.instance.OpenPopupAndWait(PopupType.DailyDungeonResult, recordData);
        }
    }

    public async UniTask<bool> EnterAsync(WeekdayType _weekType, bool _isForce = false)
    {
        if (m_data.enterWeekday == _weekType && _isForce == false)
            return false;

        TutorialManager.instance.Action_DailyDungeonPlay();

        m_data.enterWeekday = _weekType;
        m_data.curGradeType = GradeType.Normal;

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();
        //LobbyScreenManager.instance.CloseScreen(LobbyScreenType.None);

        TeamManager.instance.SetState(CharacterStateType.None);
        StageManager.instance.SetState(CharacterStateType.None);

        await UniTask.NextFrame();

        AddressableManager.instance.LoadScene("03_DailyDungeon");
        return true;
    }

    public void Start()
    {
        TeamManager.instance.StartStage();

        ControllerManager.instance.SetSwitch(true);
        ControllerManager.instance.SlotStartStage();

        InfoStageComponent.instance.SetBossRaid(true);

        ArrowNaviComponent.instance.SetTarget(TeamManager.instance.mainHero.transform);

        Signal.instance.DailyDungeonStatus.Emit(DailyDungeonStatusType.Start);
    }

    public async UniTask TimeoutAsync()
    {
        TeamManager.instance.SetState(CharacterStateType.None);
        StageManager.instance.SetState(CharacterStateType.None);

        TeamManager.instance.StopAllRespawn();
        TeamManager.instance.StopSkillCooltime();

        ControllerManager.instance.SetSwitch(false);

        Signal.instance.DailyDungeonStatus.Emit(DailyDungeonStatusType.Timeout);

        DailyDungeonRecordData resultData = new()
        {
            weekday = m_data.enterWeekday,
            gradeType = m_data.curGradeType,
            percent = m_data.percent,
        };

        m_data.count--;
        var popup = await PopupManager.instance
            .OpenPopupAndWait<PopupDailyDungeonResultComponent>(PopupType.DailyDungeonResult, resultData);

        await UniTask.WaitForEndOfFrame();

        // ÀçµµÀü
        if (popup.result == StatusType.Success)
            EnterAsync(m_data.enterWeekday, true).Forget();
        else
            ExitAsync().Forget();
    }

    public async UniTask ExitAsync()
    {
        if (m_data.enterWeekday == WeekdayType.MAX)
            return;

        m_data.enterWeekday = WeekdayType.MAX;
        Signal.instance.DailyDungeonStatus.Emit(DailyDungeonStatusType.Exit);

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();
        await UniTask.NextFrame();

        AddressableManager.instance.LoadScene("02_Lobby");
    }

    public DailyDungeonRecordData GetRecordGradeType(WeekdayType _weekday)
    {
        List<DailyDungeonRecordData> recordList = m_recordData.FindAll(x => x.weekday == _weekday);
        if (recordList.Count == 0)
            return default;

        return recordList[0];
    }

    public void SaveResultData(float _percent)
    {
        m_data.percent = _percent;

        int index = m_recordData.FindIndex(x => x.weekday == m_data.enterWeekday);
        if (index >= 0)
        {
            var data = m_recordData[index];
            if ((data.gradeType <= m_data.curGradeType && data.percent < _percent) ||
                data.gradeType < m_data.curGradeType)
            {
                data.gradeType = m_data.curGradeType;
                data.percent = _percent;
                PPWorker.Set(c_recordKey, m_recordData);
            }
        }
        else
        {
            m_recordData.Add(new()
            {
                weekday = m_data.enterWeekday,
                gradeType = m_data.curGradeType,
                percent = _percent
            });
            PPWorker.Set(c_recordKey, m_recordData);
        }
    }

    public enum DailyDungeonStatusType
    {
        None = -1,
        Start,
        Timeout,
        Exit,
    }

    public struct DailyDungeonData
    {
        public int count;
        public int adCount;
        public WeekdayType enterWeekday;
        public GradeType curGradeType;
        public float percent;

        public void Default()
        {
            adCount = 3;
            count = 2;
            enterWeekday = WeekdayType.None;
            curGradeType = GradeType.Normal;
        }

        //public int totalCount => count + adCount;
    }

    public struct DailyDungeonRecordData
    {
        public WeekdayType weekday;
        public GradeType gradeType;
        public float percent;

        //public float percent { get; set; }
        public bool isSweep { get; set; }
    }
}
