using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;

public partial class PopupTournament_Ranking
{
    protected override void SetScrollRankerData(RankerData data, bool forceFindMe = false)
    {
        if (!GameServer.Enabled) { base.SetScrollRankerData(data, forceFindMe); return; }
        void ShowProfile(RankerUserData user)
            => PopupManager.instance.AlertShow($"{user.nickname}\nRank {user.rank} / {user.point} points");
        for (var index = 0; index < m_element.podiums.Length; index++)
        {
            var available = index < data.ranker.Count;
            m_element.podiums[index].gameObject.SetActive(available);
            if (available) m_element.podiums[index].SetRankerInfo(TabType.Tutorial_Point, data.ranker[index], ShowProfile);
        }
        m_element.scroll.Initialize<PopupLobbyBossRaid_PopupRanking_Item>(data.ranker.Count,
            (item, index) => item.SetRankerInfoAsync(TabType.Tutorial_Point, data.ranker[index], ShowProfile).Forget());
        var ownIndex = data.ranker.FindIndex(x => x.uid == GameServer.Uid);
        if (ownIndex >= 0) m_element.scroll.MoveToIndex(ownIndex, false);
        m_element.myRankInfo.SetRankerInfoAsync(TabType.Tutorial_Point, data.my,
            _ => { if (ownIndex >= 0) m_element.scroll.MoveToIndex(ownIndex, true); }).Forget();
    }
}
