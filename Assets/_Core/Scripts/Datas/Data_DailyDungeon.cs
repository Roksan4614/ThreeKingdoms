using Cysharp.Threading.Tasks;
using UnityEngine;

public class Data_DailyDungeon
{
    DailyDungeonData m_data;
    public DailyDungeonData data => m_data;

    public WeekdayType enterWeekday => m_data.enterWeekday;
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

    public async UniTask<bool> EnterAsync(WeekdayType _weekType)
    {
        if (m_data.enterWeekday > WeekdayType.None)
            return false;

        if (m_data.count > 0)
            m_data.count--;
        else
            m_data.adCount--;

        m_data.enterWeekday = _weekType;
        m_data.curGradeType = GradeType.Normal;

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();

        TeamManager.instance.SetState(CharacterStateType.None);
        StageManager.instance.SetState(CharacterStateType.None);

        await UniTask.NextFrame();

        AddressableManager.instance.LoadScene("04_DailyDungeon");
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
        IngameLog.Add("TIMEOUT");

        TeamManager.instance.SetState(CharacterStateType.None);
        StageManager.instance.SetState(CharacterStateType.None);

        TeamManager.instance.StopAllRespawn();
        ControllerManager.instance.SetSwitch(false);

        Signal.instance.DailyDungeonStatus.Emit(DailyDungeonStatusType.Timeout);
        var popup = await PopupManager.instance.OpenPopupAndWait<PopupDailyDungeonResultComponent>(PopupType.DailyDungeonResult);

        m_data.count--;

        if (popup.result == StatusType.Success)
            ExitAsync().Forget();
        else
        {
            m_data.enterWeekday++;
            EnterAsync(m_data.enterWeekday).Forget();
        }
    }

    public async UniTask ExitAsync()
    {
        if (m_data.enterWeekday == WeekdayType.None)
            return;

        m_data.enterWeekday = WeekdayType.None;
        Signal.instance.DailyDungeonStatus.Emit(DailyDungeonStatusType.Exit);

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();
        await UniTask.NextFrame();
        AddressableManager.instance.LoadScene("02_Lobby");
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

        public void Default()
        {
            adCount = 3;
            count = 2;
            enterWeekday = WeekdayType.None;
            curGradeType = GradeType.Normal;
        }

        //public int totalCount => count + adCount;
    }
}
