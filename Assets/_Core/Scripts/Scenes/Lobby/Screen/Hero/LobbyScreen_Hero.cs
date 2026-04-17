using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyScreen_Hero : LobbyScreen_Base
{
    public enum HeroTabType
    {
        NONE = -1,
        Hero, Relic, Collection,
        MAX
    }

    HeroTabType m_tabType = HeroTabType.Hero;

    Dictionary<HeroTabType, LobbyScreen_Hero_TabBase> m_tabs;

    protected override void Awake()
    {
        base.Awake();

        m_tabs = new();
        for (var i = HeroTabType.NONE + 1; i < HeroTabType.MAX; i++)
        {
            m_tabs.Add(i, m_element.tabs[(int)i]);

            var tab = i;
            m_element.btnTabs[(int)i].onClick.AddListener(() => SetActiveTab(tab));
        }

        SetActiveTab(m_tabType);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetActiveTab(HeroTabType.Hero);
    }

    public void SetActiveTab(HeroTabType _tabType)
    {
        m_tabType = _tabType;

        if (m_tabs == null)
            return;

        for (var i = HeroTabType.NONE + 1; i < HeroTabType.MAX; i++)
        {
            m_tabs[i].gameObject.SetActive(i == _tabType);

            m_element.btnTabs[(int)i].SetDrawSelect(i == _tabType);
        }

        m_txtTitle.text = _tabType.ToString().ToUpper();
    }

    protected override bool IsCloseScreen()
    {
        for (var i = HeroTabType.NONE + 1; i < HeroTabType.MAX; i++)
        {
            if (m_tabs[i].IsCloseScreen())
                return true;
        }

        return false;
    }

    protected override async UniTask CloseAsync()
    {
        for (var i = HeroTabType.NONE + 1; i < HeroTabType.MAX; i++)
            await m_tabs[i].CloseAsync();

        await base.CloseAsync();
    }

    public override void Close(bool _isTween = true)
    {
        // SaveData
        (m_tabs[HeroTabType.Hero] as LobbyScreen_Hero_Hero).SaveDataAsync().Forget();
        base.Close(_isTween);
    }
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_element.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;
    [Serializable]
    struct ElementData
    {
        public List<LobbyScreen_Hero_TabBase> tabs;

        public ButtonHelper[] btnTabs;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");

            tabs = new();
            for (var i = HeroTabType.NONE + 1; i < HeroTabType.MAX; i++)
                tabs.Add(panel.GetComponent<LobbyScreen_Hero_TabBase>(i.ToString()));

            var tab = panel.Find("Tab");
            btnTabs = tab.GetComponentsInChildren<ButtonHelper>();
        }
    }
}