using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Popup_Position : MonoBehaviour, IValidatable
{
    string m_heroKey;

    Dictionary<CategoryType_HeroPositon, PopupHeroInfo_Popup_Position_Group> m_group = new();

    private void Start()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);

        for (var i = CategoryType_HeroPositon.NONE + 1; i < CategoryType_HeroPositon.MAX; i++)
        {
            CategoryType_HeroPositon type = i;
            int idx = (int)type;

            var data = TableManager.heroPosition.GetPositionds(type);
            if (data.Count == 0)
                continue;

            var group = Instantiate(m_element.baseGroup, m_element.scroll.content);
            group.Initialize(data, _type => OnButtonAsync(_type).Forget());

            m_group.Add(type, group);
        }

        DestroyImmediate(m_element.baseGroup.gameObject);
        m_element.scroll.transform.ForceRebuildLayout();
    }

    public void SetActive(string _heroKey)
    {
        gameObject.SetActive(true);
        m_heroKey = _heroKey;

        m_element.scroll.content.anchoredPosition = Vector2.zero;
        RefreshData();
    }

    void RefreshData()
    {
        foreach (var g in m_group)
            g.Value.RefreshData();
    }

    bool m_isDoing = false;
    async UniTask OnButtonAsync(HeroPositionType _heroPositionType)
    {
        if (m_isDoing == true)
            return;

        IngameLog.Add("OnButton: " + _heroPositionType);
        m_isDoing = true;

        bool result = await DataManager.heroPosition.API_BindPosition(m_heroKey, _heroPositionType);

        if (result == true)
            RefreshData();

        m_isDoing = false;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public ScrollRect scroll;

        public PopupHeroInfo_Popup_Position_Group baseGroup;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");

            scroll = panel.GetComponent<ScrollRect>();
            baseGroup = scroll.content.GetChild(0).GetComponent<PopupHeroInfo_Popup_Position_Group>();
        }
    }
    #endregion VALIDATA
}

