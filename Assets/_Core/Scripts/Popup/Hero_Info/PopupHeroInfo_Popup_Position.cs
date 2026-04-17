using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Popup_Position : MonoBehaviour, IValidatable
{
    string m_heroKey;

    private void Start()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(OnClose);

        foreach (var category in DataManager.heroPosition.data)
        {
            var group = Instantiate(m_element.baseGroup, m_element.scroll.content);
            group.Initialize(category.Value, OnButton);
        }

        DestroyImmediate(m_element.baseGroup.gameObject);
        m_element.scroll.transform.ForceRebuildLayout();
    }

    public void SetActive(string _heroKey)
    {
        gameObject.SetActive(true);
        m_heroKey = _heroKey;
    }

    void OnButton(HeroPositionType _heroPositionType)
    {
        IngameLog.Add("OnButton: " + _heroPositionType);
    }

    void OnClose()
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

