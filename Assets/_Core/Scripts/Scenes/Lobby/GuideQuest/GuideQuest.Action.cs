using UnityEngine;

public partial class TutorialManager
{
    public void Action_EnemyKill()
    {
        if (DataManager.instance.isLobby == false)
            return;

        if (m_data.nowRepeatType == GuideQuestRepeatType.enemy_kill)
            Update();
    }

    public void Action_StageBossKill()
    {
        if (DataManager.instance.isLobby == false)
            return;

        if (m_data.nowRepeatType == GuideQuestRepeatType.stage_boss_kill)
            Update();
    }

    public void Action_DailyDungeonPlay()
    {
        if (m_data.guideType == GuideQuestType.daily_dungeon_play)
            Update();
    }
}
