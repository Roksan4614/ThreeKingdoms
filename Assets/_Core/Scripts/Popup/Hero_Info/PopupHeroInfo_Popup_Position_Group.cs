using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupHeroInfo_Popup_Position_Group : MonoBehaviour, IValidatable
{

    public void Initialize(List<HeroPositionData> _data, UnityAction<HeroPositionType> _onClick)
    {
        int i = 0;
        for( ; i < _data.Count; i++)
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

            att.button.onClick.AddListener(() => _onClick(d.key));
            att.txtName.text = d.name;
            att.txtAttribute.text = d.stringAttribute;

            if (d.heroKey.IsActive() == false)
                att.txtHeroName.gameObject.SetActive(false);
            else
            {
                att.txtHeroName.gameObject.SetActive(true);
                att.txtHeroName.text = TableManager.hero.Get(d.heroKey).name;
            }
        }

        transform.ForceRebuildLayout();
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform basePosition;
        public void Initialize(Transform _transform)
        {
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

    }
    #endregion VALIDATA
}
