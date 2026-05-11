using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Data_Castle;

public class PopupCastleHeroListComponent : BasePopupComponent
{
    PopupCastleHeroListComponent() : base(PopupType.Castle_HeroList) { }

    PopupCastleHeroList_Item m_base;

    CastleData m_castleData;
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
            resultType = StatusType.Success;
            Close();
        });
        m_element.btnCancel.onClick.AddListener(Close);
    }

    public override void OpenPopup(params object[] _args)
    {
        resultType = StatusType.Wait;
        m_castleData = (CastleData)_args[0];

        var prev = m_castleData.heroes;
        m_castleData.heroes = new();
        m_castleData.heroes.AddRange(prev);

        for (var i = CoreStatType.NONE + 1; i < CoreStatType.MAX; i++)
        {
            if (SetHeroInfoData(i))
                break;
        }

        m_element.txtTitle.text = $"장수_목록: {DataManager.castle.GetObjectName(m_castleData.type)}";
        //m_element.txtTitle.text = $"장수_목록: Lv.{m_castleData.level} {DataManager.castle.GetObjectName(m_castleData.type)}";

        SetCoreStatStatus(true);
    }

    bool SetHeroInfoData(CoreStatType _coreStats)
    {
        var coreStat = TableManager.castle.GetCastleData(m_castleData.type).coreStat;

        if (coreStat.Contains(_coreStats) == false)
            return false;

        var myHero = DataManager.userInfo.myHero.Where(x => x.isMine == true).OrderByDescending(x => x.resultCoreStat[_coreStats]);

        int i = 0;
        var content = m_element.scroll.content;
        foreach (var hero in myHero)
        {
            var item = i == content.childCount ? Instantiate(m_base, content) :
                content.GetChild(i).GetComponent<PopupCastleHeroList_Item>();

            var heroData = hero;

            heroData.isBatch = m_castleData.heroes.Contains(hero.key);

            item.gameObject.SetActive(true);
            item.SetHeroInfoData(m_castleData, heroData, OnButton_Hero, coreStat);
            i++;
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
        var dbRise = TableManager.castleRise.GetRiseData(m_castleData.type, m_castleData.level);
        var coreStat = TableManager.castle.GetCastleData(m_castleData.type).coreStat.Where(x => x != CoreStatType.NONE).ToArray();

        int i = 0;
        for (; i < coreStat.Length; i++)
        {
            var stat = coreStat[i];

            var now = DataManager.castle.GetTotalCoreStat(m_castleData, stat);
            var condition = dbRise.maxCoreStat[i];

            var percent = Mathf.Min(1f, now / (float)condition);
            m_element.amountBar[i].textTitle = $"필요_{TableManager.stringTable.GetString($"CORESTAT_{stat.ToString().ToUpper()}")}_수치 ({percent * 100:0.##}%)";
            m_element.amountBar[i].textAmount = $"{now}/{condition}";
            m_element.amountBar[i].fill = percent;
        }

        if (_isInit)
            for (; i < m_element.amountBar.Length; i++)
                m_element.amountBar[i].gameObject.SetActive(false);

        if (_isInit == true)
            m_element.amountBar[0].transform.parent.ForceRebuildLayout(1);
    }

    void OnButton_Hero(HeroInfoData _heroInfoData)
    {
        if (m_castleData.heroes.Remove(_heroInfoData.key) == false)
        {
            if (m_castleData.heroes.Count < 6)
                m_castleData.heroes.Add(_heroInfoData.key);
            else
            {
                PopupManager.instance.AlertShow("배치_인원이_이미_모두_찼습니다.");
                return;
            }
        }

        SetCoreStatStatus();
    }

    public override void Close()
    {
        CloseAsync().Forget();
    }

    async UniTask CloseAsync()
    {
        await Utils.SetActivePunchAsync(m_element.panel, false, false);

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

        public TextMeshProUGUI txtTitle;
        public ButtonHelper[] btnCoreStat;
        public AmountBarHelper[] amountBar;

        public ButtonHelper btnConfirm;
        public ButtonHelper btnCancel;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            scroll = panel.GetComponent<ScrollRect>("List/Scroll");
            txtTitle = panel.GetComponent<TextMeshProUGUI>("txt_title");

            var top = scroll.transform.Find("Top");
            btnCoreStat = top.GetComponentsInChildren<ButtonHelper>();

            amountBar = panel.Find("Condition").GetComponentsInChildren<AmountBarHelper>();

            var buttons = panel.Find("Button_Box");
            btnConfirm = buttons.GetComponent<ButtonHelper>("btn_confirm");
            btnCancel = buttons.GetComponent<ButtonHelper>("btn_cancel");
        }
    }
    #endregion VALIDATE
}
