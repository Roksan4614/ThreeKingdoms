using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class PopupCastleHeroListComponent : BasePopupComponent
{
    PopupCastleHeroListComponent() : base(PopupType.Castle_HeroList) { }

    PopupCastleHeroList_Item m_base;

    protected override void Awake()
    {
        base.Awake();

        m_base = m_element.scroll.content.GetChild(0).GetComponent<PopupCastleHeroList_Item>();
        m_base.gameObject.SetActive(false);
        m_base.transform.SetParent(m_element.scroll.viewport);
    }

    public override void OpenPopup(params object[] _args)
    {
        int i = 0;
        var content = m_element.scroll.content;
        foreach (var hero in DataManager.userInfo.myHero)
        {
            var item = i == content.childCount ? Instantiate(m_base, content) :
                content.GetChild(i).GetComponent<PopupCastleHeroList_Item>();

            item.gameObject.SetActive(true);
            item.SetHeroInfoData(hero, OnButton_Hero);
            i++;
        }
    }

    void OnButton_Hero(HeroInfoData _heroInfoData)
    {

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
