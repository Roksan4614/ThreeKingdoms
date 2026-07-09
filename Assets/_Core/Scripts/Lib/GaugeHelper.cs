using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GaugeHelper : MonoBehaviour, IValidatable
{
    public string textTitle { set => m_element.textTitle = value; }
    public string textAmount { set => m_element.textAmount = value; }
    public float fillAmount { get => m_element.bar.fillAmount; set => m_element.bar.fillAmount = value; }
    public float doFillAmount
    {
        set
        {
            m_element.bar.fillAmount = value;

            //m_element.bar.DOKill();
            //m_element.bar.DOFillAmount(value, 0.05f);
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Image bar;

        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtAmount;

        public TextMeshProUGUI txtTitle_Front;
        public TextMeshProUGUI txtAmount_Front;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("txt_title");
            txtAmount = _transform.GetComponent<TextMeshProUGUI>("txt_amount");

            bar = _transform.GetComponent<Image>("Bar/img_bar");

            txtTitle_Front = bar.transform.GetComponent<TextMeshProUGUI>("txt_title");
            txtAmount_Front = bar.transform.GetComponent<TextMeshProUGUI>("txt_amount");
        }

        public string textTitle { set => txtTitle.text = txtTitle_Front.text = value; }
        public string textAmount { set => txtAmount.text = txtAmount_Front.text = value; }
    }
    #endregion VALIDATE
}
