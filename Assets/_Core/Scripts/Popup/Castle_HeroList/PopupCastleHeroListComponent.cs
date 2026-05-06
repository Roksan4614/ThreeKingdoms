using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupCastleHeroListComponent : BasePopupComponent
{
    PopupCastleHeroListComponent() : base(PopupType.Castle_HeroList) { }

    PopupCastleHeroList_Item m_base;

    List<string> m_heroes = new();
    public List<string> heroes => m_heroes;

    protected override void Awake()
    {
        base.Awake();

        m_base = m_element.scroll.content.GetChild(0).GetComponent<PopupCastleHeroList_Item>();
        m_base.gameObject.SetActive(false);
        m_base.transform.SetParent(m_element.scroll.viewport);
    }

    public override void OpenPopup(params object[] _args)
    {
        m_heroes.Clear();
        m_heroes.AddRange((List<string>)_args[0]);

        int i = 0;
        var content = m_element.scroll.content;
        foreach (var hero in DataManager.userInfo.myHero)
        {
            var item = i == content.childCount ? Instantiate(m_base, content) :
                content.GetChild(i).GetComponent<PopupCastleHeroList_Item>();

            var data = hero;

            data.isBatch = m_heroes.Contains(hero.key);

            item.gameObject.SetActive(true);
            item.SetHeroInfoData(data, OnButton_Hero);
            i++;
        }
    }

    void OnButton_Hero(HeroInfoData _heroInfoData)
    {
        if (m_heroes.Remove(_heroInfoData.key) == false)
            m_heroes.Add(_heroInfoData.key);
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

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            scroll = panel.GetComponent<ScrollRect>("List/Scroll");

        }
    }
    #endregion VALIDATE
}
