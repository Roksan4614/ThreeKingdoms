using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System.Linq;
using UnityEngine;

public partial class PopupTournament_Ranking : PopupLobbyBossRaid_PopupRanking
{
    bool m_isClose;
    public async UniTask OpenPopupAsync()
    {
        m_isClose = false;
        if (ThreeKingdoms.Client.Server.GameServer.Enabled)
        {
            for (int i = 0; i < m_element.tabs.Length; i++) m_element.tabs[i].gameObject.SetActive(i == 0);
            m_curTabType = TabType.NONE;
        }
        Utils.SetActivePunch(transform, true);

        m_element.scroll.content.anchoredPosition = Vector2.zero;
        OnButton_Tab(TabType.Tutorial_Point);

        await UniTask.WaitUntil(() => m_isClose == true);
    }

    protected override void OnButton_Close()
    {
        m_isClose = true;
        Utils.SetActivePunch(transform, false, _callback:()=> m_isClose = false);
    }

    protected override async UniTask SetRankingAsync()
    {
        RankerData rankerData;
        try { rankerData = await TournamentWorker.instance.API_LoadRankerData(m_curTabType); }
        catch (System.Exception error) { PopupManager.instance.AlertShow(error.Message); return; }

        rankerData.ranker = GetRankerUserRange(rankerData);

        SetScrollRankerData(rankerData, true);
    }

    #region VALIDATE
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_elementTournament.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    ElementDataTournament m_elementTournament;

    [System.Serializable]
    struct ElementDataTournament
    {
        public void Initialize(Transform _transform)
        {
        }
    }
    #endregion VALIDATE

}
