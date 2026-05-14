using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GaugeHelper : MonoBehaviour, IValidatable
{
    public string textTitle { set => m_element.textTitle = value; }
    public string textAmount { set => m_element.textAmount = value; }
    public float fillAmount { set => m_element.fillAmount = value; }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Image m_bar;

        public TextMeshProUGUI m_txtTitle;
        public TextMeshProUGUI m_txtAmount;

        public TextMeshProUGUI m_txtTitle_Front;
        public TextMeshProUGUI m_txtAmount_Front;

        public void Initialize(Transform _transform)
        {
            m_txtTitle = _transform.GetComponent<TextMeshProUGUI>("txt_title");
            m_txtAmount = _transform.GetComponent<TextMeshProUGUI>("txt_amount");

            m_bar = _transform.GetComponent<Image>("Bar/img_bar");

            m_txtTitle_Front = m_bar.transform.GetComponent<TextMeshProUGUI>("txt_title");
            m_txtAmount_Front = m_bar.transform.GetComponent<TextMeshProUGUI>("txt_amount");
        }

        public string textTitle { set => m_txtTitle.text = m_txtTitle_Front.text = value; }
        public string textAmount { set => m_txtAmount.text = m_txtAmount_Front.text = value; }
        public float fillAmount { set => m_bar.fillAmount = value; }
    }
    #endregion VALIDATE
}
