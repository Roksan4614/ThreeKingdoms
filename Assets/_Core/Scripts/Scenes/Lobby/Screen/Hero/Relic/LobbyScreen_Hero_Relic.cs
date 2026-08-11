using Cysharp.Threading.Tasks;
using DG.Tweening;
using Rev9.Tournament;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Hero_Relic : LobbyScreen_Hero_TabBase, IValidatable
{
    protected bool m_isScreenHeroMode = true;

    protected enum TabType
    {
        NONE = -1,
        Relic, Treasure,
        MAX
    }

    enum HeroCountType
    {
        type_1,
        type_10,
        type_100,
        Max
    }

    TabType m_curTab = TabType.Relic;
    HeroCountType m_curHeroCountType = HeroCountType.type_1;

    const string c_keyHeroCountType = "pp_HeroCountType";

    List<TotalRelicData> m_totalRelic = new();

    protected override void Awake()
    {
        TotalRelicData baseData = new();
        baseData.Create(m_element.pTotalTreasure.GetChild(0));
        m_totalRelic.Add(baseData);

        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
        {
            var tab = i;
            m_element.btnTabs[(int)i].onClick.AddListener(() => SetActiveTab(tab));
        }

        var countType = HeroCountType.type_1 + PPWorker.Get<int>(c_keyHeroCountType);
        m_curHeroCountType = countType - 1;
        SetHeroCountType(countType);

        m_element.heroCountData.btnOpen.onClick.AddListener(() => SetActiveCountPanel(true));

        for (int i = 0; i < m_element.heroCountData.btnCount.Length; i++)
        {
            var b = m_element.heroCountData.btnCount[i];

            var type = (HeroCountType)i;
            b.text = GetStringCountType(type);
            b.onClick.AddListener(() =>
            {
                SetHeroCountType(type);
                SetActiveCountPanel(false);

                PPWorker.Set(c_keyHeroCountType, (int)type);
            });
        }
    }

    bool m_isStarted = false;
    protected virtual void Start()
    {
        m_curTab = TabType.NONE;
        SetActiveTab(m_isScreenHeroMode ? TabType.Relic : TabType.Treasure);
        m_isStarted = true;
    }

    protected virtual void OnEnable()
    {
        if (m_isStarted == true)
            OnEnableAsync().Forget();
    }

    async UniTask OnEnableAsync()
    {
        m_curTab = TabType.NONE;
        SetActiveTab(m_isScreenHeroMode ? TabType.Relic : TabType.Treasure);
        await UniTask.NextFrame();
        RebuildLayout();
    }

    protected virtual void SetActiveTab(TabType _tabType)
    {
        if (m_curTab == _tabType)
            return;

        m_curTab = _tabType;

        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
            m_element.btnTabs[(int)i].SetDrawSelect(i == _tabType);

        m_element.heroCountData.rtPanel.parent.parent.gameObject.SetActive(_tabType == TabType.Relic);
        m_element.heroCountData.rtPanel.anchoredPosition = Vector2.zero;

        if (_tabType == TabType.Relic)
            UpdateRelic_TotalClass();
        else
            UpdateTreasure_TotalStat();
    }

    void SetActiveCountPanel(bool _isActive)
    {
        var tab = m_element.btnTabs[0].transform.parent.gameObject;

        if (_isActive == false)
            tab.SetActive(true);
        else if (tab.activeSelf == false)
        {
            SetActiveCountPanel(false);
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

    void UpdateRelic_TotalClass()
    {
        var myHero = DataManager.userInfo.GetHeroSortData(_isWithNotMine: false).ToArray();

        m_element.scroll.Initialize<LobbyScreen_Hero_Relic_Item>(myHero.Length,
            (_item, _idxData) =>
            {
                _item.SetRelicData(myHero[_idxData], false
                    , _heroInfo => OnButton_Item(_heroInfo.key.IsActive() ? TabType.Relic : TabType.Treasure, _heroInfo));
#if UNITY_EDITOR
                _item.name = myHero[_idxData].key;
#endif
            });

        m_element.pTotalClass.gameObject.SetActive(true);
        m_element.pTotalTreasure.gameObject.SetActive(false);
        m_element.txtTreasureCount.gameObject.SetActive(false);

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

    protected virtual void UpdateTreasure_TotalStat()
    {
        var scroll = m_element.scroll;

        var dbTreasure = TableManager.treasure.list.Where(x => x.isActive)
            .OrderByDescending(x => DataManager.stat.relic.GetTreasureData(x.key).isBatch)
            .ThenBy(x => DataManager.stat.relic.GetTreasureData(x.key).tickBatch)
            .ToArray();

        m_element.scroll.Initialize<LobbyScreen_Hero_Relic_Item>(dbTreasure.Length,
            (_item, _idxData) =>
            {
                _item.SetTreasureDataAsync(DataManager.stat.relic.dataTreasure.ToList(), dbTreasure[_idxData]
                    , _heroInfo => OnButton_Item(_heroInfo.key.IsActive() ? TabType.Relic : TabType.Treasure, _heroInfo)).Forget();
#if UNITY_EDITOR
                _item.name = dbTreasure[_idxData].key;
#endif
            });

        m_element.pTotalClass.gameObject.SetActive(false);
        m_element.pTotalTreasure.gameObject.SetActive(true);
        m_element.txtTreasureCount.gameObject.SetActive(true);

        SetTextTotalTreasure();
    }

    protected void SetTextTotalTreasure()
    {
        var dbBonusTreasure = m_isScreenHeroMode ? DataManager.stat.relic.bonusTreasureBonus : TournamentWorker.data.GetTeam().bonusTreasureBonus;
        var pTotalTreasure = m_element.pTotalTreasure;

        int i = 0;
        foreach (var d in dbBonusTreasure)
        {
            if (i == m_totalRelic.Count)
            {
                TotalRelicData newData = new();
                newData.Create(Instantiate(m_totalRelic[0].txtTitle, pTotalTreasure).transform);
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

        var countBatchTreasure = m_isScreenHeroMode ?
            DataManager.stat.relic.dataTreasure.Count(x => x.isBatch == true) :
            TournamentWorker.data.GetTeam().treasure.Count();

        m_element.txtTreasureCount.text = $"선택한_보물: ({countBatchTreasure}/3)";
        if (countBatchTreasure > 0)
        {
            m_element.pTotalTreasure.gameObject.SetActive(true);
            m_element.pTotalTreasure.ForceRebuildLayout();
        }
        else
            m_element.pTotalTreasure.gameObject.SetActive(false);

        RebuildLayout();
    }

    protected void RebuildLayout()
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

    protected void OnButton_Item(TabType _tapType, HeroInfoData _heroInfoData)
    {
        if (_tapType == TabType.Relic)
            SetTextTotalClass(_heroInfoData.classType);
        else
        {
            m_element.scroll.content.anchoredPosition = Vector2.zero;
            UpdateTreasure_TotalStat();
            SetTextTotalTreasure();
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    protected ElementData m_element;

    [Serializable]
    protected struct ElementData
    {
        public TextMeshProUGUI txtTreasureCount;
        public TextMeshProUGUI[] txtTotalClass;
        public Transform pTotalTreasure;

        public LoopScrollHelper scroll;

        public ButtonHelper[] btnTabs;

        public HeroCountData heroCountData;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            txtTreasureCount = panel.GetComponent<TextMeshProUGUI>("txt_treasure_count");
            txtTotalClass = panel.Find("Total_Class").GetComponentsInChildren<TextMeshProUGUI>(true);

            scroll = panel.Find("List/Scroll").GetComponent<LoopScrollHelper>();

            pTotalTreasure = panel.Find("Total_Treasure");

            var tab = _transform.Find("Tab");
            btnTabs = tab.GetComponentsInChildren<ButtonHelper>();

            var heroCount = _transform.Find("Hero_Count");
            heroCountData.btnOpen = heroCount.GetComponent<ButtonHelper>("btn_open");
            heroCountData.rtPanel = heroCount.GetComponent<RectTransform>("Viewport/Panel");
            heroCountData.btnCount = heroCountData.rtPanel.GetComponentsInChildren<ButtonHelper>();
        }

        public RectTransform rtPanel => (RectTransform)txtTreasureCount.transform.parent;
        public RectTransform rtLayout => (RectTransform)scroll.transform.parent;
        public Transform pTotalClass => txtTotalClass[0].transform.parent;
    }

    [Serializable]
    protected struct HeroCountData
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
