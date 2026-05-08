using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Data_Castle;

public class PopupCastleHeroListComponent : BasePopupComponent
{
    PopupCastleHeroListComponent() : base(PopupType.Castle_HeroList) { }

    PopupCastleHeroList_Item m_base;

    CastleData m_castleData;

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
    }

    public override void OpenPopup(params object[] _args)
    {
        m_castleData = (CastleData)_args[0];

        for (var i = CoreStatType.NONE + 1; i < CoreStatType.MAX; i++)
        {
            if (SetHeroInfoData(i))
                break;
        }
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
            else if(coreStat.Contains(stat) == false)
                name = $"<color=#7E7E7E>{name}";

            m_element.btnCoreStat[i].text = name;

        }

        return true;
    }

    void OnButton_Hero(HeroInfoData _heroInfoData)
    {
        if (m_castleData.heroes.Remove(_heroInfoData.key) == false)
            m_castleData.heroes.Add(_heroInfoData.key);
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

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            scroll = panel.GetComponent<ScrollRect>("List/Scroll");

            var top = scroll.transform.Find("Top");
            btnCoreStat = top.GetComponentsInChildren<ButtonHelper>();
        }
    }
    #endregion VALIDATE
}
