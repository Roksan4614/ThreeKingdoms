using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Hero_Relic : LobbyScreen_Hero_TabBase, IValidatable
{
    enum TabType
    {
        NONE = -1,
        Hero, Relic,
        MAX
    }

    enum HeroCountType
    {
        type_1,
        type_10,
        type_100,
        Max
    }

    TabType m_curTab = TabType.Hero;
    HeroCountType m_curHeroCountType = HeroCountType.type_1;

    const string c_keyHeroCountType = "pp_HeroCountType";

    List<TotalRelicData> m_totalRelic = new();

    protected override void Awake()
    {
        m_element.baseScrollItem.transform.SetParent(m_element.scroll.viewport);
        m_element.baseScrollItem.gameObject.SetActive(false);

        TotalRelicData baseData = new();
        baseData.Create(m_element.pTotalRelic.GetChild(0));
        m_totalRelic.Add(baseData);

        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
        {
            var tab = i;
            m_element.btnTabs[(int)i].onClick.AddListener(() => SetActiveTab(tab));
        }

        var countType = HeroCountType.type_1 + PPWorker.Get<int>(c_keyHeroCountType);
        m_curHeroCountType = countType - 1;
        SetHeroCountType(countType);

        m_element.heroCountData.btnOpen.onClick.AddListener(() => SetActiveHeroCount(true));

        for (int i = 0; i < m_element.heroCountData.btnCount.Length; i++)
        {
            var b = m_element.heroCountData.btnCount[i];

            var type = (HeroCountType)i;
            b.text = GetStringCountType(type);
            b.onClick.AddListener(() =>
            {
                SetHeroCountType(type);
                SetActiveHeroCount(false);

                PPWorker.Set(c_keyHeroCountType, (int)type);
            });
        }
    }

    private void Start()
    {
        m_curTab = TabType.Hero - 1;
        SetActiveTab(TabType.Hero);
    }

    private void OnEnable()
    {
        SetActiveTab(TabType.Hero);
    }

    void SetActiveTab(TabType _tabType)
    {
        if (m_curTab == _tabType)
            return;

        m_curTab = _tabType;

        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
            m_element.btnTabs[(int)i].SetDrawSelect(i == _tabType);

        m_element.heroCountData.rtPanel.parent.parent.gameObject.SetActive(_tabType == TabType.Hero);
        m_element.heroCountData.rtPanel.anchoredPosition = Vector2.zero;

        if (_tabType == TabType.Hero)
            UpdateTotalClass();
        else
            UpdateTotalStat();
    }

    void SetActiveHeroCount(bool _isActive)
    {
        var tab = m_element.btnTabs[0].transform.parent.gameObject;

        if (_isActive == false)
            tab.SetActive(true);
        else if (tab.activeSelf == false)
        {
            SetActiveHeroCount(false);
            return;
        }

        m_element.heroCountData.btnOpen.interactable = false;

        var rtPanel = m_element.heroCountData.rtPanel;
        rtPanel.DOAnchorPosX(_isActive ? -rtPanel.rect.width : 0, 0.1f)
            .OnComplete(() =>
            {
                m_element.heroCountData.btnOpen.interactable = true;

                if (_isActive == true)
                    tab.SetActive(false);
            });
    }

    void SetHeroCountType(HeroCountType _type)
    {
        if (m_curHeroCountType == _type)
            return;

        m_curHeroCountType = _type;
        int curIdx = (int)m_curHeroCountType;
        for (int i = 0; i < m_element.heroCountData.btnCount.Length; i++)
            m_element.heroCountData.btnCount[i].SetDrawSelect(i == curIdx);

        m_element.heroCountData.btnOpen.text = GetStringCountType(_type);
    }

    string GetStringCountType(HeroCountType _type)
        => _type switch
        {
            HeroCountType.type_1 => "+1",
            HeroCountType.type_10 => "+10",
            HeroCountType.type_100 => "+100",
            _ => "_최대_"
        };

    void UpdateTotalClass()
    {
        var myHero = DataManager.userInfo.GetHeroSortData().ToArray();

        int i = 0;
        var scroll = m_element.scroll;
        for (; i < myHero.Length; i++)
        {
            var heroInfo = myHero[i];
            LobbyScreen_Hero_Relic_Item item = null;
            if (i == scroll.content.childCount)
            {
                item = Instantiate(m_element.baseScrollItem, scroll.content);
                item.Bind(_data => { OnButton_Item(_data.key.IsActive() ? TabType.Hero : TabType.Relic, _data); });
            }
            else
                item = scroll.content.GetChild(i).GetComponent<LobbyScreen_Hero_Relic_Item>();

            heroInfo.enchantLevel = DataManager.stat.relic.dataHero[heroInfo.key];

            item.gameObject.SetActive(true);
            item.SetHeroDataAsync(heroInfo).Forget();
        }

        // 나머지 숨기기
        for (; i < scroll.content.childCount; i++)
            scroll.content.GetChild(i).gameObject.SetActive(false);

        m_element.pTotalClass.gameObject.SetActive(true);
        m_element.pTotalRelic.gameObject.SetActive(false);
        m_element.txtRelicCount.gameObject.SetActive(false);

        RebuildLayout();

        // 보너스 스탯 넣어주자
        // 지휘관 <color=#BA0700>+000.00K%
        for (var t = HeroClassType.NONE + 1; t < HeroClassType.MAX; t++)
            SetTextTotalClass(t);
    }

    void SetTextTotalClass(HeroClassType _classType)
    {
        var db = DataManager.stat.relic.bonusClassBonus;
        var txt = m_element.txtTotalClass[(int)_classType];
        var amount = db.ContainsKey(_classType) ? db[_classType] : 0;

        txt.text = $"{TableManager.stringHero.GetString("CLASSTYPE_" + _classType.ToString().ToUpper())}_<color=#BA0700>+{amount.AmountKMBT()}%";
    }

    void UpdateTotalStat()
    {
        int i = 0;
        var scroll = m_element.scroll;

        var dbRelic = TableManager.relic.dbList;
        var myRelic = DataManager.stat.relic.dataRelic;

        for (; i < dbRelic.Count; i++)
        {
            LobbyScreen_Hero_Relic_Item item = null;
            if (i == scroll.content.childCount)
            {
                item = Instantiate(m_element.baseScrollItem, scroll.content);
                item.Bind(_data => { OnButton_Item(_data.key.IsActive() ? TabType.Hero : TabType.Relic, _data); });
            }
            else
                item = scroll.content.GetChild(i).GetComponent<LobbyScreen_Hero_Relic_Item>();

            item.gameObject.SetActive(true);
            item.SetRelicDataAsync(dbRelic[i]).Forget();
        }

        for (; i < scroll.content.childCount; i++)
            scroll.content.GetChild(i).gameObject.SetActive(false);

        m_element.pTotalClass.gameObject.SetActive(false);
        m_element.pTotalRelic.gameObject.SetActive(true);
        m_element.txtRelicCount.gameObject.SetActive(true);

        SetTextTotalRelic();
    }

    void SetTextTotalRelic()
    {
        var dbBonusRelic = DataManager.stat.relic.bonusRelicBonus;
        var pTotalRelic = m_element.pTotalRelic;

        //baseTotalRelic.txtName = panel.Find("Total_Relic/Text").GetComponent<TextMeshProUGUI>();
        int i = 0;
        foreach (var d in dbBonusRelic)
        {
            if (i == m_totalRelic.Count)
            {
                TotalRelicData newData = new();
                newData.Create(Instantiate(m_totalRelic[0].txtTitle, pTotalRelic).transform);
                m_totalRelic.Add(newData);
            }

            TotalRelicData data = m_totalRelic[i];
            data.txtTitle.text = d.Value.statName;
            data.txtValue.text = $"{d.Value.stringPercent}";
            m_totalRelic[i].SetActive(true);
            i++;
        }

        for (; i < m_totalRelic.Count; i++)
            m_totalRelic[i].SetActive(false);

        var countBatchRelic = DataManager.stat.relic.dataRelic.Count(x => x.isBatch == true);
        m_element.txtRelicCount.text = $"선택한_보물: ({countBatchRelic}/3)";
        if (countBatchRelic > 0)
        {
            m_element.pTotalRelic.gameObject.SetActive(true);
            m_element.pTotalRelic.ForceRebuildLayout();
        }
        else
            m_element.pTotalRelic.gameObject.SetActive(false);

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

    void OnButton_Item(TabType _tapType, HeroInfoData _heroInfoData)
    {
        if (_tapType == TabType.Hero)
            SetTextTotalClass(_heroInfoData.classType);
        else
            SetTextTotalRelic();
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
        public Transform pTotalRelic;

        public ScrollRect scroll;
        public LobbyScreen_Hero_Relic_Item baseScrollItem;

        public ButtonHelper[] btnTabs;

        public HeroCountData heroCountData;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            txtRelicCount = panel.GetComponent<TextMeshProUGUI>("txt_relic_count");
            txtTotalClass = panel.Find("Total_Class").GetComponentsInChildren<TextMeshProUGUI>(true);

            scroll = panel.Find("List/Scroll").GetComponent<ScrollRect>();
            baseScrollItem = scroll.content.GetChild(0).GetComponent<LobbyScreen_Hero_Relic_Item>();

            pTotalRelic = panel.Find("Total_Relic");

            var tab = _transform.Find("Tab");
            btnTabs = tab.GetComponentsInChildren<ButtonHelper>();

            var heroCount = _transform.Find("Hero_Count");
            heroCountData.btnOpen = heroCount.GetComponent<ButtonHelper>("btn_open");
            heroCountData.rtPanel = heroCount.GetComponent<RectTransform>("Viewport/Panel");
            heroCountData.btnCount = heroCountData.rtPanel.GetComponentsInChildren<ButtonHelper>();
        }

        public RectTransform rtPanel => (RectTransform)txtRelicCount.transform.parent;
        public RectTransform rtLayout => (RectTransform)scroll.transform.parent;
        public Transform pTotalClass => txtTotalClass[0].transform.parent;
    }

    [Serializable]
    struct HeroCountData
    {
        public ButtonHelper btnOpen;

        public RectTransform rtPanel;
        public ButtonHelper[] btnCount;

        public void Release()
        {
            btnOpen = null; btnCount = null;
        }
    }

    struct TotalRelicData
    {
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtValue;

        public void Create(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("");
            txtValue = _transform.GetComponent<TextMeshProUGUI>("Text");
        }

        public void SetActive(bool _isActive) => txtTitle.gameObject.SetActive(_isActive);
    }

    #endregion VALIDATA
}
