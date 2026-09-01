using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupHeroInfo_Popup_Position_Group : MonoBehaviour, IValidatable
{
    Dictionary<HeroPositionType, ButtonPositionData> m_data = new();
    public void Initialize(CategoryType_HeroPositon _category, List<TableHeroPositionData> _data, UnityAction<CategoryType_HeroPositon, HeroPositionType> _onClick)
    {
        m_element.txtTitle.text = _category switch
        {
            CategoryType_HeroPositon.HEAD => "수장",
            CategoryType_HeroPositon.GENERAL => "관직",
            _ => "칭호"
        };
        m_element.txtTitle.transform.parent.ForceRebuildLayout();

        int i = 0;
        for (; i < _data.Count; i++)
        {
            var d = _data[i];
            ButtonPositionData att = new();

            if (i == 0)
                att.Initialize(m_element.basePosition);
            else
            {
                att.Initialize(Instantiate(m_element.basePosition, transform));
                att.button.onClick.RemoveAllListeners();
            }

            att.button.onClick.AddListener(() => _onClick(_category, d.type));
            att.txtName.text = d.name;
            att.txtAttribute.text = d.stringAttribute;

            att.transform.ForceRebuildLayout();

            m_data.Add(d.type, att);
        }

        transform.ForceRebuildLayout();
    }

    public void RefreshData(HeroPositionType _heroPositionType = HeroPositionType.NONE)
    {
        if (_heroPositionType > HeroPositionType.NONE)
            RefreshData(_heroPositionType, m_data[_heroPositionType]);
        else
        {
            foreach (var att in m_data)
                RefreshData(att.Key, att.Value);
        }
    }

    void RefreshData(HeroPositionType _type, ButtonPositionData _data)
    {
        var hpData = DataManager.heroPosition.GetHeroPositionData(_type);

        bool isActive_Hero = hpData != null;
        _data.check.SetActive(isActive_Hero);
        _data.txtHeroName.gameObject.SetActive(isActive_Hero);

        if (isActive_Hero)
            _data.txtHeroName.text = DataManager.userInfo.GetHeroInfoData(hpData.heroKey).name;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform basePosition;
        public TextMeshProUGUI txtTitle;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Title/Panel/Text");
            basePosition = _transform.Find("btn_position");
        }
    }

    [Serializable]
    struct ButtonPositionData
    {
        public Button button;
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtHeroName;
        public TextMeshProUGUI txtAttribute;
        public GameObject check;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<Button>();
            txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/Text");
            txtHeroName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_hero");
            txtAttribute = _transform.GetComponent<TextMeshProUGUI>("txt_attribute");
            check = _transform.Find("Panel/Box/Check").gameObject;
        }

        public Transform transform => button.transform;
    }
    #endregion VALIDATA
}
