using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CastleData = Data_Castle.CastleData;

public class PopupCastleHeroListComponent : BasePopupComponent
{
    PopupCastleHeroListComponent() : base(PopupType.Castle_HeroList) { }

    PopupCastleHeroList_Item m_base;

    CastleData m_castleData;
    int m_maxBatchSlot;

    public StatusType statusType { get; private set; }
    public StatusType resultType { get; private set; }

    public List<string> heroes => m_castleData.heroes;

    protected override void Awake()
    {
        base.Awake();

        m_base = m_element.scroll.content.GetChild(0).GetComponent<PopupCastleHeroList_Item>();
        m_base.gameObject.SetActive(false);
        m_base.transform.SetParent(m_element.scroll.viewport);

        for (int i = 0; i < m_element.btnCoreStat.Length; i++)
        {
            CoreStatType statType = CoreStatType.NONE + 1 + i;
            m_element.btnCoreStat[i].onClick.AddListener(() => { SetHeroInfoData(statType); });
        }

        m_element.btnConfirm.onClick.AddListener(() =>
        {
            DataManager.castle.SetBatchHeroAsync(m_castleData, _result =>
            {
                resultType = _result;
                Close();

            }).Forget();
        });
        m_element.btnCancel.onClick.AddListener(Close);
    }

    public override void OpenPopup(params object[] _args)
    {
        Utils.SetActivePunch(m_element.panel, true);

        statusType = resultType = StatusType.Wait;
        m_castleData = (CastleData)_args[0];

        var prev = m_castleData.heroes;
        m_castleData.heroes = new();
        m_castleData.heroes.AddRange(prev);

        m_element.txtTitle.text = $"장수_목록: {m_castleData.name}";
        //m_element.txtTitle.text = $"장수_목록: Lv.{m_castleData.level} {DataManager.castle.GetObjectName(m_castleData.type)}";

        for (int i = 0; i < m_element.gauges.Length; i++)
            m_element.gauges[i].fillAmount = 0;

        RefreshHeroesData(true);

        m_maxBatchSlot = m_castleData.dbRise.character_slot_max;
        m_element.txtBatchCount.text = $"({m_castleData.heroes.Count}/{m_maxBatchSlot})";
    }

    void RefreshHeroesData(bool _isInit)
    {
        for (var i = CoreStatType.NONE + 1; i < CoreStatType.MAX; i++)
        {
            if (SetHeroInfoData(i))
                break;
        }
        SetCoreStatStatus(_isInit);
    }

    bool SetHeroInfoData(CoreStatType _coreStats)
    {
        var coreStat = TableManager.castle.GetCastleData(m_castleData.type).coreStat;

        if (coreStat.Contains(_coreStats) == false)
            return false;

        var myHero = DataManager.userInfo.myHero.SortByDescending(x => x.resultCoreStat[_coreStats]);

        int i = 0;
        var content = m_element.scroll.content;
        foreach (var hero in myHero)
        {
            bool isNew = i == content.childCount;
            var item = isNew ? Instantiate(m_base, content) :
                content.GetChild(i).GetComponent<PopupCastleHeroList_Item>();

            var heroData = hero;

            heroData.isBatch = m_castleData.heroes.Contains(hero.key);

            item.gameObject.SetActive(true);
            item.SetHeroInfoData(m_castleData, heroData, OnButton_Hero, coreStat);
            i++;

            // 유저아이콘 클릭했을 때 처리하자
            if (isNew)
                item.Initialize(this, () => OpenHeroInfoPopupAsync(item.heroInfoData).Forget());
        }

        for (; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);

        i = 0;
        for (var stat = CoreStatType.NONE + 1; stat < CoreStatType.MAX; stat++, i++)
        {
            var name = TableManager.stringTable.GetString($"CORESTAT_{stat.ToString().ToUpper()}");

            if (_coreStats == stat)
                name = $"<color=#{Palette.htmlString_Up}>{name}";
            else if (coreStat.Contains(stat) == false)
                name = $"<color=#7E7E7E>{name}";

            m_element.btnCoreStat[i].text = name;

        }

        return true;
    }

    void SetCoreStatStatus(bool _isInit = false)
    {
        var dbRise = m_castleData.dbRise;
        var coreStat = Array.FindAll( TableManager.castle.GetCastleData(m_castleData.type).coreStat, x => x != CoreStatType.NONE);

        int i = 0;
        for (; i < coreStat.Length; i++)
        {
            var stat = coreStat[i];

            var now = DataManager.castle.GetTotalCoreStat(m_castleData, stat);
            var condition = m_castleData.type == CastleObjectType.Palace ? dbRise.orinValue01 : dbRise.maxCoreStat[i];

            var percent = Mathf.Min(1f, now / (float)condition);
            m_element.gauges[i].textTitle = $"필요_{TableManager.stringTable.GetString($"CORESTAT_{stat.ToString().ToUpper()}")}_수치 ({percent * 100:0.##}%)";
            m_element.gauges[i].textAmount = $"{now}/{condition}";
            m_element.gauges[i].doFillAmount = percent;
        }

        if (_isInit)
        {
            for (; i < m_element.gauges.Length; i++)
                m_element.gauges[i].gameObject.SetActive(false);

            m_element.gauges[0].transform.parent.ForceRebuildLayout(1);
        }
    }

    void OnButton_Hero(PopupCastleHeroList_Item _item)
    {
        if (m_castleData.heroes.Remove(_item.heroInfoData.key) == false)
            m_castleData.heroes.Add(_item.heroInfoData.key);

        m_element.txtBatchCount.text = $"({m_castleData.heroes.Count}/{m_maxBatchSlot})";
        SetCoreStatStatus();
    }

    bool m_isOpenPopup_HeroInfo;
    PopupHeroInfo m_popupHeroInfo;
    async UniTask OpenHeroInfoPopupAsync(HeroInfoData _heroInfoData)
    {
        if (m_isOpenPopup_HeroInfo == true)
            return;

        Signal.instance.Event_ActivePunch_Start.Emit();
        Utils.SetActivePunch(m_element.panel, false);

        m_isOpenPopup_HeroInfo = true;

        if (m_popupHeroInfo == null)
        {
            m_popupHeroInfo = await PopupManager.instance.OpenPopupAsync<PopupHeroInfo>(PopupType.Hero_HeroInfo, _heroInfoData);
            m_popupHeroInfo.isDontDestroy = true;
        }
        else
            await m_popupHeroInfo.SetHeroInfoDataAsync(_heroInfoData);

        await UniTask.WaitUntil(() => m_popupHeroInfo.statusType != StatusType.Wait, cancellationToken: destroyCancellationToken);

        if (m_popupHeroInfo.isNeedUpdate == true)
        {
            resultType = StatusType.Success;
            RefreshHeroesData(false);
        }

        m_isOpenPopup_HeroInfo = false;
        Utils.SetActivePunch(m_element.panel, true);
    }

    public bool CloseEscape()
    {
        if (m_popupHeroInfo?.gameObject.activeSelf == true)
            return false;
        if (statusType == StatusType.Wait)
        {
            CloseAsync().Forget();
            return false;
        }
        return true;
    }

    public override void Close()
        => CloseAsync().Forget();

    async UniTask CloseAsync()
    {
        if (resultType != StatusType.Success)
            resultType = StatusType.Cancel;

        statusType = StatusType.Cancel;
        await Utils.SetActivePunchAsync(m_element.panel, false, false);
        gameObject.SetActive(false);
    }

    public bool isFullBatch => m_castleData.heroes.Count >= m_maxBatchSlot;

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform panel;
        public ScrollRect scroll;

        public TextMeshProUGUI txtTitle;
        public ButtonHelper[] btnCoreStat;
        public GaugeHelper[] gauges;
        public TextMeshProUGUI txtBatchCount;

        public ButtonHelper btnConfirm;
        public ButtonHelper btnCancel;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            scroll = panel.GetComponent<ScrollRect>("List/Scroll");
            txtTitle = panel.GetComponent<TextMeshProUGUI>("txt_title");

            var top = scroll.transform.Find("Top");
            btnCoreStat = top.GetComponentsInChildren<ButtonHelper>();

            gauges = panel.Find("Condition").GetComponentsInChildren<GaugeHelper>();
            txtBatchCount = panel.GetComponent<TextMeshProUGUI>("List/txt_batchCount");

            var buttons = panel.Find("Button_Box");
            btnConfirm = buttons.GetComponent<ButtonHelper>("btn_confirm");
            btnCancel = buttons.GetComponent<ButtonHelper>("btn_cancel");
        }
    }
    #endregion VALIDATE
}
