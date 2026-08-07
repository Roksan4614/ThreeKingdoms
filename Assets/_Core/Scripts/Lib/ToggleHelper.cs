using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleHelper : MonoBehaviour, IValidatable
{
    bool m_isOn;
    public string text { set => m_element.txtTitle.text = value; }

    public Button.ButtonClickedEvent onClick
        => m_element.button.onClick;

    public bool isOn
    {
        get => m_isOn;
        set
        {
            m_isOn = value;
            m_element.rtButton.SetAnchoredPositionX(m_isOn ? m_element.imgBG.rectTransform.rect.width - m_element.rtButton.rect.width : 0);
            m_element.txtToggle.text = m_isOn ? "ON" : "OFF";
            m_element.imgBG.color = m_isOn ? Color.gray8 : Color.white;
        }
    }

    public void OnButtonToggle() => isOn = !m_isOn;


    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtToggle;
        public Image imgBG;
        public RectTransform rtButton;
        public Button button;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Text");
            txtToggle = _transform.GetComponent<TextMeshProUGUI>("BG/Button/Text");
            imgBG = _transform.GetComponent<Image>("BG");
            rtButton = (RectTransform)_transform.Find("BG/Button");
            button = _transform.GetComponent<Button>("BG");
        }
    }
    #endregion VALIDATE

}
