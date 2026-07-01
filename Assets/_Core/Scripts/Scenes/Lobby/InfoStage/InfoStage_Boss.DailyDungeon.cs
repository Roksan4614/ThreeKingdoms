using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class InfoStage_Boss
{
    void Awake_DailyDungeon()
    {
        StartTimerAsync_DailyDungeon().Forget();

        Signal.instance.DailyDungeonNextStep.connect = SetBossInfo_DailyDungeon;
    }

    public void SetBossInfo_DailyDungeon(GradeType _gradeType)
    {
        m_element.txtName.text = $"[{TableManager.stringTable.GetGradeType(_gradeType)}] {DataManager.dailyDungeon.bossData.name}";
    }

    async UniTask StartTimerAsync_DailyDungeon()
    {
#if UNITY_EDITOR
        await TimerAsync(10 / 60f, Utils.GetUTC().AddMinutes(10 / 60f));
#else
        await TimerAsync(1, Utils.GetUTC().AddMinutes(1));
#endif

        DataManager.dailyDungeon.TimeoutAsync().Forget();
    }
}
