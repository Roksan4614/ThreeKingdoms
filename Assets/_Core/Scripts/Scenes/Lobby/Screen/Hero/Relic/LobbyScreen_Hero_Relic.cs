using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct RelicInfoData
{
    public string key;
    public int level;
}

public class LobbyScreen_Hero_Relic : LobbyScreen_Hero_TabBase, IValidatable
{
    enum TabType
    {
        NONE = -1,
        Hero, Relic,
        MAX
    }

    TabType m_curTab = TabType.Hero;

    public override void Awake()
    {
        m_element.baseScrollItem.transform.SetParent(m_element.scroll.viewport);
        m_element.baseScrollItem.gameObject.SetActive(false);

        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
        {
            var tab = i;
            m_element.btnTabs[(int)i].onClick.AddListener(() => SetActiveTab(tab));
        }

    }

    public void Start()
    {
        UpdateTotalClass();
    }

    void SetActiveTab(TabType _tabType)
    {
        if (m_curTab == _tabType)
            return;

        m_curTab = _tabType;

        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
        {
            m_element.imgTabs[(int)i].color = i == _tabType ?
                Palette.instance.data.Get(PaletteColorType.button_select) :
                Color.white;
            m_element.btnTabs[(int)i].TMPText.color = i == _tabType ? Color.white : Color.black;
        }

        if (_tabType == TabType.Hero)
            UpdateTotalClass();
        else
            UpdateTotalStat();
    }

    void UpdateTotalClass()
    {
        var dbMyHero = DataManager.userInfo.GetHeroSortData().Where(
            x => DataManager.userInfo.GetHeroInfoData(x.key).isMine == true).ToArray();

        int i = 0;
        var scroll = m_element.scroll;
        for (; i < dbMyHero.Length; i++)
        {
            var heroInfo = dbMyHero[i];
            LobbyScreen_Hero_Relic_Item item = null;
            if (i == scroll.content.childCount)
            {
                item = Instantiate(m_element.baseScrollItem, scroll.content);
                item.Bind(_data => { OnButton_Item(TabType.Hero, _data); });
            }
            else
                item = scroll.content.GetChild(i).GetComponent<LobbyScreen_Hero_Relic_Item>();

            RelicInfoData relicData = new()
            {
                key = heroInfo.key,
                level = DataManager.stat.relic.dataHero[heroInfo.key]
            };

            item.gameObject.SetActive(true);
            item.SetHeroData(relicData);
        }

        for (; i < scroll.content.childCount; i++)
            scroll.content.GetChild(i).gameObject.SetActive(false);

        m_element.pTotalClass.gameObject.SetActive(true);
        m_element.pTotalRelic.gameObject.SetActive(false);
        m_element.txtRelicCount.gameObject.SetActive(false);

        RebuildLayout();
    }

    void UpdateTotalStat()
    {
        int i = 0;
        var scroll = m_element.scroll;

        for (; i < scroll.content.childCount; i++)
            scroll.content.GetChild(i).gameObject.SetActive(false);

        m_element.pTotalClass.gameObject.SetActive(false);
        m_element.pTotalRelic.gameObject.SetActive(true);
        m_element.txtRelicCount.gameObject.SetActive(true);

        RebuildLayout();
    }

    void RebuildLayout()
    {
        m_element.scroll.content.anchoredPosition = Vector2.zero;

        var rtPanel = m_element.rtPanel;
        var rtLayout = m_element.rtLayout;

        rtPanel.ForceRebuildLayout();

        var heightPanel = rtPanel.rect.height;
        var posY_Layout = rtLayout.anchoredPosition.y;

        var sizeLayout = rtLayout.sizeDelta;
        sizeLayout.y = heightPanel + posY_Layout;
        rtLayout.sizeDelta = sizeLayout;
    }

    void OnButton_Item(TabType _tapType, RelicInfoData _relicData)
    {

    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtRelicCount;
        public TextMeshProUGUI[] txtTotalClass;
        public TotalRelicStatData baseTotalRelic;

        public ScrollRect scroll;
        public LobbyScreen_Hero_Relic_Item baseScrollItem;

        public ButtonHelper[] btnTabs;
        public Image[] imgTabs;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            txtRelicCount = panel.GetComponent<TextMeshProUGUI>("txt_relic_count");
            txtTotalClass = panel.Find("Total_Class").GetComponentsInChildren<TextMeshProUGUI>(true);

            scroll = panel.Find("List/Scroll").GetComponent<ScrollRect>();
            baseScrollItem = scroll.content.GetChild(0).GetComponent<LobbyScreen_Hero_Relic_Item>();

            baseTotalRelic = new();
            baseTotalRelic.parent = panel.Find("Total_Relic");
            baseTotalRelic.txtName = panel.Find("Total_Relic/Text").GetComponent<TextMeshProUGUI>();
            baseTotalRelic.txtValue = panel.Find("Total_Relic/Text/Text").GetComponent<TextMeshProUGUI>();

            var menu = _transform.Find("Menu");
            btnTabs = menu.GetComponentsInChildren<ButtonHelper>();
            imgTabs = btnTabs.Select(x => x.transform.GetComponent<Image>()).ToArray();
        }

        public RectTransform rtPanel => (RectTransform)txtRelicCount.transform.parent;
        public RectTransform rtLayout => (RectTransform)scroll.transform.parent;
        public Transform pTotalClass => txtTotalClass[0].transform.parent;
        public Transform pTotalRelic => baseTotalRelic.parent;
    }

    [Serializable]
    struct TotalRelicStatData
    {
        public Transform parent;
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtValue;
    }
    #endregion VALIDATA
}
