using UnityEngine;

public partial class TutorialManager
{
    public void Action_EnemyKill()
    {
        if (DataManager.instance.isLobby == false)
            return;

        if (m_data.nowRepeatType == GuideQuestRepeatType.ENEMY_KILL)
            Update();
    }

    public void Action_StageBossKill()
    {
        if (DataManager.instance.isLobby == false)
            return;

        if (m_data.nowRepeatType == GuideQuestRepeatType.STAGE_BOSS_KILL)
            Update();
    }

    public void Action_DailyDungeonPlay()
    {
        if (m_data.guideType == GuideQuestType.DAILY_DUNGEON_PLAY)
            Update();
    }
}
