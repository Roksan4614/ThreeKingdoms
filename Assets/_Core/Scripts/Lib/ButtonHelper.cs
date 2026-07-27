using DG.Tweening;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHelper : MonoBehaviour, IValidatable, IPointerDownHandler, IPointerUpHandler
{
    protected virtual void Awake() { }

    public RectTransform rt => m_element.rt;

    public Button.ButtonClickedEvent onClick
        => m_element.button.onClick;

    public string text
    {
        get => m_element.txtName.text;
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

    public TextMeshProUGUI TMPText => m_element.txtName;

    public bool isTriggerSwitch { get; set; } = true;

    public UnityAction funcEnter { get; set; }
    public UnityAction funcExit { get; set; }
    public UnityAction funcDown { get; set; }
    public UnityAction funcUp { get; set; }


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

    public void SetColliderSize()
        => m_element.SetColliderSize();

    public void SetDrawSelect(bool _isSelect)
    {
        m_element.image.color = _isSelect ?
                Palette.instance.data.Get(PaletteColorType.button_select) :
                m_prevColorData.button == default ? Color.white : m_prevColorData.button;

        m_element.txtName.color = _isSelect ? Color.white :
            m_prevColorData.text == default ? Color.black : m_prevColorData.text;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    public void OnPointerDown(PointerEventData eventData)
        => funcDown?.Invoke();

    public void OnPointerUp(PointerEventData eventData)
        => funcUp?.Invoke();

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [SerializeField]
    PrevColorData m_prevColorData;

    [Serializable]
    struct PrevColorData
    {
        public Color button;
        public Color text;
    }

    [Serializable]
    struct ElementData
    {
        public RectTransform rt;
        public Vector3 localScale;
        public Button button;
        public Image image;
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
            image = _transform.GetComponent<Image>();
            txtName = _transform.GetComponent<TextMeshProUGUI>("Text");
            objCheck = _transform.Find("CheckBox/Check")?.gameObject;

            collider = _transform.GetComponent<BoxCollider2D>();
            if (collider != null)
                SetColliderSize();
        }

        public void SetColliderSize()
        {
            if (collider != null)
                collider.size = rt.sizeDelta;
        }
    }
    #endregion VALIDATA
}
