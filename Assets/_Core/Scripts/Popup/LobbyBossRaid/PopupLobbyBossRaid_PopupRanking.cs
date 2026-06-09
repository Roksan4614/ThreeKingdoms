using Cysharp.Threading.Tasks;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyBossRaid_PopupRanking : MonoBehaviour, IValidatable
{
    public enum TabType
    {
        NONE = -1,

        Point,
        PrevRaid,
    }

    TabType m_curTabType = TabType.NONE;

    private void Awake()
    {
        m_element.imgPanel.enabled = true;

        //m_element.baseRanker.transform.SetParent(m_element.scroll.viewport);
        //m_element.baseRanker.gameObject.SetActive(false);

        m_element.btnClose.onClick.AddListener(OnButton_Close);

        for (var i = 0; i < m_element.tabs.Length; i++)
        {
            var tabType = (TabType)i;
            m_element.tabs[i].onClick.AddListener(() => OnButton_Tab(tabType));
        }
    }

    public void OpenPopup()
    {
        Utils.SetActivePunch(transform, true);

        m_element.scroll.content.anchoredPosition = Vector2.zero;

        OnButton_Tab(TabType.Point);
    }

    void OnButton_Tab(TabType _tabType)
    {
        if (m_curTabType == _tabType)
            return;

        m_curTabType = _tabType;

        for (var i = 0; i < m_element.tabs.Length; i++)
            m_element.tabs[i].SetDrawSelect(i == (int)_tabType);

        SetPodium();
        //SetRanking();
    }

    void SetPodium()
    {
        Data_BossRaid.BossRaidRankerUserData[] ranker = (m_curTabType == TabType.Point ? DataManager.bossRaid.rankPoint : DataManager.bossRaid.rankPrevRaid).ranker.Take(3).ToArray();

        for (int i = 0; i < ranker.Length; i++)
            m_element.podiums[i].SetRankerInfoAsync(ranker[i], _rankerData => OnButtonAsync_UserInfo(_rankerData).Forget()).Forget();
    }

    void SetRanking()
    {
        var rankData = (m_curTabType == TabType.Point ? DataManager.bossRaid.rankPoint : DataManager.bossRaid.rankPrevRaid);

        // 포인트 랭킹일 경우 내 위아래로 20명씩임.
        if (m_curTabType == TabType.Point)
        {
            var index = rankData.ranker.FindIndex(x => x.uid == rankData.my.uid);
            int startIndex = Mathf.Max(0, index - 20);
            int endIndex = Mathf.Min(rankData.ranker.Count - 1, index + 20);
            rankData.ranker = rankData.ranker.GetRange(startIndex, endIndex - startIndex + 1);
        }

        int userIndex = -1;
        int i = 0;
        var content = m_element.scroll.content;
        for (; i < rankData.ranker.Count; i++)
        {
            var item = i == content.childCount ? Instantiate(m_element.baseRanker, content) : content.GetChild(i).GetComponent<PopupLobbyBossRaid_PopupRanking_Item>();
            item.SetRankerInfoAsync(m_curTabType, rankData.ranker[i], _rankerData => OnButtonAsync_UserInfo(_rankerData).Forget()).Forget();
            item.gameObject.SetActive(true);

            if (userIndex == -1 && rankData.ranker[i].uid == rankData.my.uid)
                userIndex = i;
        }

        for (; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);

        content.ForceRebuildLayout();

        // 콘텐츠 위치 설정
        if (m_curTabType == TabType.Point)
        {
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();

            var heightItem = content.rect.height - layout.padding.top - layout.padding.bottom - layout.spacing * (rankData.ranker.Count - 1);
            heightItem /= rankData.ranker.Count;

            var pos = content.anchoredPosition;
            pos.y = heightItem * Mathf.Max(0, userIndex - 2) + layout.padding.top + (layout.spacing * userIndex);
            content.anchoredPosition = pos;
        }
        else
        {
            content.anchoredPosition = Vector2.zero;
        }

        m_element.myRankInfo.SetRankerInfoAsync(m_curTabType, rankData.my, null).Forget();
    }

    async UniTask OnButtonAsync_UserInfo(Data_BossRaid.BossRaidRankerUserData _rankerData)
    {

    }

    void OnButton_Close()
    {
        Utils.SetActivePunch(transform, false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image imgPanel;

        public Button btnClose;
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtPoint;

        public ButtonHelper[] tabs;

        public ScrollRect scroll;

        public PopupLobbyBossRaid_PopupRanking_PodiumItem[] podiums;
        public PopupLobbyBossRaid_PopupRanking_Item baseRanker;
        public PopupLobbyBossRaid_PopupRanking_Item myRankInfo;

        public void Initialize(Transform _transform)
        {
            imgPanel = _transform.GetComponent<Image>();

            btnClose = _transform.GetComponent<Button>("Top/btn_back");
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Top/txt_title");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("txt_point");

            tabs = _transform.Find("Tab").GetComponentsInChildren<ButtonHelper>();

            scroll = _transform.GetComponent<ScrollRect>("Scroll");
            podiums = _transform.Find("Podium").GetComponentsInChildren<PopupLobbyBossRaid_PopupRanking_PodiumItem>();
            baseRanker = scroll.content.GetChild(0).GetComponent<PopupLobbyBossRaid_PopupRanking_Item>();
            myRankInfo = scroll.viewport.GetComponent<PopupLobbyBossRaid_PopupRanking_Item>("MyInfo");
        }
    }
    #endregion VALIDATE

}
