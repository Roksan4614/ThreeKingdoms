using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class InfoStage_Boss
{
    void Awake_DailyDungeon()
    {
        StartTimerAsync_DailyDungeon().Forget();
        Signal.instance.DailyDungeonStatus.connectLambda = new(this, status =>
        {
            if (status == Data_DailyDungeon.DailyDungeonStatusType.Timeout || status == Data_DailyDungeon.DailyDungeonStatusType.Exit)
                m_ctsTimer = m_ctsTimer.ReleaseCTS();
        });

        Signal.instance.DailyDungeonNextStep.connect = SetBossInfo_DailyDungeon;
    }

    public void SetBossInfo_DailyDungeon(GradeType _gradeType)
    {
        m_element.txtName.text = $"[{TableManager.stringTable.GetGradeType(_gradeType)}] {DataManager.dailyDungeon.bossData.name}";
    }

    async UniTask StartTimerAsync_DailyDungeon()
    {
        var sceneToken = destroyCancellationToken;
        try
        {
        //#if UNITY_EDITOR
        //        await TimerAsync(5 / 60f, Utils.GetUTC().AddMinutes(5 / 60f));
        //#else
        //        await TimerAsync(DataManager.dailyDungeon.PlayTimeSeconds / 60f, Utils.GetUTC().AddSeconds(DataManager.dailyDungeon.PlayTimeSeconds));
        //#endif
        await TimerAsync(DataManager.dailyDungeon.PlayTimeSeconds / 60f, Utils.GetUTC().AddSeconds(DataManager.dailyDungeon.PlayTimeSeconds));

        }
        catch (System.OperationCanceledException) { return; }
        if (sceneToken.IsCancellationRequested) return;
        DataManager.dailyDungeon.TimeoutAsync().Forget();
    }
}
