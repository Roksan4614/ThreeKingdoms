using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCastleHeroListComponent_Mission : BasePopupComponent
{
    PopupCastleHeroListComponent_Mission() : base(PopupType.NONE) { }

    Data_Castle_Mission.CastleMissionData m_missionData;

    public StatusType statusType { get; private set; }
    public bool isUpdated { get; private set; }
    public string[] heroes => m_missionData.heroes.ToArray();

    private void Start()
    {
        m_element.baseItem.gameObject.SetActive(false);
        m_element.baseItem.transform.SetParent(m_element.scroll.viewport);

        m_element.btnConfirm.onClick.AddListener(() =>
        {
            isUpdated = true;
            Close();
        });
        m_element.btnCancel.onClick.AddListener(Close);

        m_element.gauges[1].gameObject.SetActive(false);
        m_element.gauges[1].transform.parent.ForceRebuildLayout();
    }

    public void Open(Data_Castle_Mission.CastleMissionData _missionData)
    {
        m_missionData = _missionData;
        m_missionData.heroes = new();
        m_missionData.heroes.AddRange(_missionData.heroes);

        isUpdated = false;
        statusType = StatusType.Wait;
        gameObject.SetActive(true);

        Utils.SetActivePunch(m_element.panel, true);

        RefreshHeroesData();
    }

    void RefreshHeroesData()
    {
        SetHeroInfoData(m_missionData.dbData.core_stat);
        SetCoreStatStatus();
    }

    bool SetHeroInfoData(CoreStatType _coreStat)
    {
        var myHero = DataManager.userInfo.myHero.Where(x => x.isMine == true && DataManager.castle.mission.GetMissionIdxBatchHero(x.key) == -1)
            .OrderByDescending(x => x.resultCoreStat[_coreStat]);

        int i = 0;
        var content = m_element.scroll.content;
        foreach (var hero in myHero)
        {
            bool isNew = i == content.childCount;
            var item = isNew ? Instantiate(m_element.baseItem, content) :
                content.GetChild(i).GetComponent<PopupCastleHeroList_Item_Mission>();

            var heroData = hero;

            heroData.isBatch = m_missionData.heroes.Contains(hero.key);

            item.gameObject.SetActive(true);
            item.SetHeroInfoData_Mission(heroData, OnButton_Hero, _coreStat);
            i++;

            // 유저아이콘 클릭했을 때 처리하자
            if (isNew)
                item.onClick_HeroIcon.AddListener(()
                    => OpenHeroInfoPopupAsync(item.heroInfoData).Forget());
        }

        for (; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);

        i = 0;
        for (var stat = CoreStatType.NONE + 1; stat < CoreStatType.MAX; stat++, i++)
        {
            var name = TableManager.stringTable.GetString($"CORESTAT_{stat.ToString().ToUpper()}");

            if (_coreStat == stat)
                name = $"<color=#{Palette.htmlString_Up}>{name}";
            else
                name = $"<color=#7E7E7E>{name}";

            m_element.btnCoreStat[i].text = name;
        }

        return true;
    }

    void OnButton_Hero(HeroInfoData _heroInfoData)
    {
        if (m_missionData.heroes.Remove(_heroInfoData.key) == false)
        {
            if (m_missionData.heroes.Count < 6)
                m_missionData.heroes.Add(_heroInfoData.key);
            else
            {
                PopupManager.instance.AlertShow("배치_인원이_이미_모두_찼습니다.");
                return;
            }
        }

        SetCoreStatStatus();
    }
    void SetCoreStatStatus()
    {
        var stat = m_missionData.dbData.core_stat;
        var now = DataManager.castle.mission.GetTotalCoreStat(m_missionData);
        var condition = m_missionData.coreStatMax;

        var percent = Mathf.Min(1f, now / (float)condition);
        var gauge = m_element.gauges[0];
        gauge.textTitle = $"필요_{TableManager.stringTable.GetString($"CORESTAT_{stat.ToString().ToUpper()}")}_수치 ({percent * 100:0.##}%)";
        gauge.textAmount = $"{now}/{condition}";
        gauge.fillAmount = percent;
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
            m_popupHeroInfo = await PopupManager.instance.OpenPopup<PopupHeroInfo>(PopupType.Hero_HeroInfo, _heroInfoData);
            m_popupHeroInfo.isDontDestroy = true;
        }
        else
            await m_popupHeroInfo.SetHeroInfoDataAsync(_heroInfoData);

        await UniTask.WaitUntil(() => m_popupHeroInfo.statusType != StatusType.Wait, cancellationToken: destroyCancellationToken);

        if (m_popupHeroInfo.isNeedUpdate == true)
            RefreshHeroesData();

        m_isOpenPopup_HeroInfo = false;
        Utils.SetActivePunch(m_element.panel, true);
    }

    public override void Close()
        => CloseAsync().Forget();

    async UniTask CloseAsync()
    {
        statusType = StatusType.Cancel;
        await Utils.SetActivePunchAsync(m_element.panel, false);
        gameObject.SetActive(false);
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform panel;
        public ScrollRect scroll;

        public ButtonHelper[] btnCoreStat;

        public ButtonHelper btnConfirm;
        public ButtonHelper btnCancel;

        public GaugeHelper[] gauges;

        public TextMeshProUGUI txtTitle;
        public PopupCastleHeroList_Item_Mission baseItem;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");

            txtTitle = panel.GetComponent<TextMeshProUGUI>("txt_title");
            scroll = panel.GetComponent<ScrollRect>("List/Scroll");
            baseItem = scroll.content.GetChild(0).GetComponent<PopupCastleHeroList_Item_Mission>();

            var top = scroll.transform.Find("Top");
            btnCoreStat = top.GetComponentsInChildren<ButtonHelper>();

            gauges = panel.Find("Condition").GetComponentsInChildren<GaugeHelper>();

            var buttons = panel.Find("Button_Box");
            btnConfirm = buttons.GetComponent<ButtonHelper>("btn_confirm");
            btnCancel = buttons.GetComponent<ButtonHelper>("btn_cancel");
        }
    }
    #endregion VALIDATE
}
