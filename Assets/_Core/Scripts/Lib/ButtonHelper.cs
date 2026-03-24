using DG.Tweening;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
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

    public bool isTriggerSwitch { get; set; } = true;
    public UnityAction funcEnter { get; set; }
    public UnityAction funcExit { get; set; }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (isTriggerSwitch && _collision.CompareTag("Pointer"))
        {
            transform.DOScale(m_element.localScale * 1.05f, .1f);
            funcEnter?.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D _collision)
    {
        if (isTriggerSwitch && _collision.CompareTag("Pointer"))
        {
            transform.DOScale(m_element.localScale, .1f);
            funcExit?.Invoke();
        }
    }

    public TextMeshProUGUI GetTMPText() => m_element.txtName;

    public void SetColliderSize()
        => m_element.SetColliderSize();

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform rt;
        public Vector3 localScale;
        public Button button;
        public TextMeshProUGUI txtName;

        public BoxCollider2D collider;
        public GameObject objCheck;

        public void Initialize(Transform _transform)
        {
            rt = (RectTransform)_transform;
            localScale = _transform.localScale;
            button = _transform.GetComponent<Button>();
            if (button == null)
            {
                button = _transform.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
            }
            txtName = _transform.GetComponent<TextMeshProUGUI>("Text");
            objCheck = _transform.Find("CheckBox/Check")?.gameObject;

            collider = _transform.GetComponent<BoxCollider2D>();
            if (collider != null)
                SetColliderSize();
        }

        public void SetColliderSize()
        {
            collider.size = rt.sizeDelta;
        }
    }
    #endregion VALIDATA
}
