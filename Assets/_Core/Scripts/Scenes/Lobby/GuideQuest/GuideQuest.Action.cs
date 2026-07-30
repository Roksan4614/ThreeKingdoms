using UnityEngine;

public partial class GuideQuestComponent
{
    public void Action_EnemyKill()
    {
        if (DataManager.instance.isLobby == false)
            return;

        if (TutorialManager.data.nowRepeatType == GuideQuestRepeatType.ENEMY_KILL)
            TutorialManager.instance.Update();
    }

    public void Action_StageBossKill()
    {
        if (DataManager.instance.isLobby == false)
            return;

        if (TutorialManager.data.nowRepeatType == GuideQuestRepeatType.STAGE_BOSS_KILL)
            TutorialManager.instance.Update();
    }
}
