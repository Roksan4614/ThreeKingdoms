using UnityEngine;

public class PopupTournament_Ranking_Slot : PopupLobbyBossRaid_PopupRanking_Item
{
    protected override void SetRankerPoint(RankerUserData _rankerData, PopupLobbyBossRaid_PopupRanking.TabType _tabType)
    {
        string msg = $"{_rankerData.point:#,0}";
        switch (_tabType)
        {
            case PopupLobbyBossRaid_PopupRanking.TabType.Tutorial_Point:
                msg += "p";
                break;
            case PopupLobbyBossRaid_PopupRanking.TabType.Tutorial_Win:
                msg += "_½Â";
                break;
            case PopupLobbyBossRaid_PopupRanking.TabType.Tutorial_Winning:
                msg += "_¿¬½Â";
                break;
        }

        m_element.txtPoint.text = msg;
    }
}
