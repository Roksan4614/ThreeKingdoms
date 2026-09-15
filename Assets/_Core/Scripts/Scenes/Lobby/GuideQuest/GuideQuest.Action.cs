using UnityEngine;

public partial class TutorialManager
{
    public void Action_EnemyKill()
    {
        if (ThreeKingdoms.Client.Server.GameServer.Enabled) { ServerRecordProgress("enemy_kill"); return; }
        if (DataManager.instance.isLobby == false)
            return;

        if (m_data.nowRepeatType == GuideQuestRepeatType.enemy_kill)
            Update();
    }

    public void Action_StageBossKill()
    {
        if (ThreeKingdoms.Client.Server.GameServer.Enabled) { ServerRecordProgress("stage_boss_kill"); return; }
        if (DataManager.instance.isLobby == false)
            return;

        if (m_data.nowRepeatType == GuideQuestRepeatType.stage_boss_kill)
            Update();
    }

    public void Action_DailyDungeonPlay()
    {
        // Server mode records the successful admission response, not a local UI invocation.
        if (ThreeKingdoms.Client.Server.GameServer.Enabled) return;
        if (m_data.guideType == GuideQuestType.daily_dungeon_play)
            Update();
    }
}
