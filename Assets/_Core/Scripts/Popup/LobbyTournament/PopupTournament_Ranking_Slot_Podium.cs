using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class PopupTournament_Ranking_Slot_Podium : PopupLobbyBossRaid_PopupRanking_PodiumItem
{
    protected override void SetRankerPoint(RankerUserData _rankerData, PopupLobbyBossRaid_PopupRanking.TabType _tabType)
    {
        string msg = $"{_rankerData.point:#,0}";
        var tabType = (PopupTournament_Ranking.TabTournamentType)_tabType;
        switch (tabType)
        {
            case PopupTournament_Ranking.TabTournamentType.Tournament_Point:
                msg += TableManager.stringTable.GetString("UI_RANK_DIGIT_POINT");
                break;
            case PopupTournament_Ranking.TabTournamentType.Tournament_Win:
                msg += TableManager.stringTable.GetString("UI_RANK_DIGIT_WIN");
                break;
            case PopupTournament_Ranking.TabTournamentType.Tournament_Winning:
                msg += TableManager.stringTable.GetString("UI_RANK_DIGIT_WINNING");
                break;
        }

        m_element.txtPoint.text = msg;
    }
}
