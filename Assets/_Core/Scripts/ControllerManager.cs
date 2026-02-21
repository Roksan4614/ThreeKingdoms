using System;
using System.Collections;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ControllerManager : Singleton<ControllerManager>, IPointerDownHandler, IPointerUpHandler, IDragHandler, IValidatable
{
    [SerializeField] float m_maxRadiusBar = 150;
    //[SerializeField]    float m_speed = 10;

    CharacterComponent m_character;

    public bool isActive => m_element.pad.gameObject.activeSelf;

    private void Start()
    {
        m_element.pad.gameObject.SetActive(false);

        Signal.instance.ConnectMainHero.connectLambda = new(this, _ => m_character = _);

    }

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
#endif

    private void Update()
    {
        if (isActive == true && m_character?.isLive == true)
            m_character.OnConrollerMove(m_element.padBar.position - m_element.pad.position);
    }

    public bool isLeftClick => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Z);
    public bool isRightClick => Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X);
    public bool isLeftClick_Push => Input.GetMouseButton(0) || Input.GetKey(KeyCode.Z);
    public bool isRightClick_Push => Input.GetMouseButton(1) || Input.GetKey(KeyCode.X);
    public bool isTouch => Input.touchCount > 0;
    public static bool isClick => instance.isLeftClick || instance.isTouch;

    public void OnPointerDown(PointerEventData _eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 startPos);

        m_element.pad.anchoredPosition = startPos;
        m_element.pad.gameObject.SetActive(true);
        m_element.padBar.localPosition = Vector3.zero;

        OnDrag(_eventData);
    }

    public void OnPointerUp(PointerEventData _eventData)
    {
        if (isActive == false)
            return;

        if (m_character?.isLive == true)
            m_character.SetState(TeamManager.instance.teamState);

        m_element.pad.gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData _eventData)
    {
        if (isActive == false)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 targetPos);

        m_element.padBar.anchoredPosition = Vector2.ClampMagnitude(targetPos - m_element.pad.anchoredPosition, m_maxRadiusBar);
    }

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public RectTransform pad;
        public RectTransform padBar;

        public void Initialize(Transform _transform)
        {
            pad = (RectTransform)_transform.Find("Pad");
            padBar = (RectTransform)pad.Find("Bar");
        }
    }
}
