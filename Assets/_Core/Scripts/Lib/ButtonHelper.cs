using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHelper : MonoBehaviour, IValidatable
{
    public Button.ButtonClickedEvent onClick
        => m_element.button.onClick;

    public string text
    {
        set => m_element.txtName.text = value;
    }

    public bool isCheck
    {
        get => m_element.objCheck != null && m_element.objCheck.activeSelf;
        set => m_element.objCheck?.gameObject.SetActive(value);
    }

    public bool interactable
    {
        get => m_element.button.interactable;
        set => m_element.button.interactable = value;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Button button;
        public TextMeshProUGUI txtName;

        public GameObject objCheck;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<Button>();
            txtName = _transform.GetComponent<TextMeshProUGUI>("Text");
            objCheck = _transform.Find("CheckBox/Check")?.gameObject;
        }
    }
    #endregion VALIDATA
}
