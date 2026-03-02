using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControllerManager : Singleton<ControllerManager>, IPointerDownHandler, IPointerUpHandler, IDragHandler, IValidatable
{
    [SerializeField] bool m_isKeyboardMode = true;
    [SerializeField] float m_maxRadiusBar = 150;
    [SerializeField] float m_startRotZ = 20;

    CharacterComponent m_character;

    bool m_isKeyboardMoving = false;
    bool m_isControlAttack = false;

    bool m_isPush = false;

    public bool isActive => m_element.pad.gameObject.activeSelf || m_isKeyboardMoving || m_isControlAttack;

    private void Start()
    {
        m_element.pad.gameObject.SetActive(false);
        m_element.btnDash.transform.parent.gameObject.SetActive(false);

        Signal.instance.ConnectMainHero.connectLambda = new(this, _ => m_character = _);

    }

    private void Update()
    {
        if (m_character?.isLive == false)
            return;

        var lookAt = Vector2.zero;
        if (m_element.pad.gameObject.activeSelf == true)
            lookAt = m_element.padBar.position - m_element.pad.position;
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                lookAt.x = -1;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                lookAt.x = 1;

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                lookAt.y = 1;
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                lookAt.y = -1;
        }

        if (lookAt != Vector2.zero)
        {
            m_character.OnConrollerMove(lookAt);
            m_isKeyboardMoving = true;

        }
        else if (m_isKeyboardMoving == true)
        {
            m_isKeyboardMoving = false;
            StopControll();
        }

        // 공격
        if (m_isKeyboardMode && m_isPush == true)
        {
            if (isLeftClick_Push)
                OnButton_Attack();
            else if (isRightClick_Push && lookAt != Vector2.zero)
                OnButton_Dash(true);
        }
    }

    void OnButton_Attack()
    {
        m_isControlAttack = true;
        m_character.attack.ControlAttack();
        m_isControlAttack = false;
    }

    void OnButton_Dash(bool _isMouse)
    {
        var targetPos = Vector3.zero;
        if (_isMouse)
            targetPos = CameraManager.instance.GetMousePosition();

        m_character.move.Dash(targetPos);
    }

    public bool isLeftClick => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Z);
    public bool isRightClick => Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X);
    public bool isLeftClick_Push => Input.GetMouseButton(0) || Input.GetKey(KeyCode.Z);
    public bool isRightClick_Push => Input.GetMouseButton(1) || Input.GetKey(KeyCode.X);
    public bool isTouch => Input.touchCount > 0;
    public static bool isClick => instance.isLeftClick || instance.isTouch;

    public void OnPointerDown(PointerEventData _eventData)
    {
        m_isPush = true;

        if (m_isKeyboardMode)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 startPos);

        m_element.pad.anchoredPosition = startPos;
        m_element.pad.gameObject.SetActive(true);

        m_element.pad.rotation = Quaternion.Euler(0, 0, m_startRotZ);
        m_element.pad.DORotate(Vector3.zero, 0.1f).SetEase(Ease.OutBack);

        m_element.padBar.localPosition = Vector3.zero;

        m_element.btnDash.transform.parent.gameObject.SetActive(true);

        OnDrag(_eventData);

    }

    public void OnPointerUp(PointerEventData _eventData)
    {
        m_isPush = false;

        if (isActive == false)
            return;

        StopControll();
        m_element.pad.gameObject.SetActive(false);
        m_element.btnDash.transform.parent.gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData _eventData)
    {
        if (isActive == false)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 targetPos);

        m_element.padBar.anchoredPosition = Vector2.ClampMagnitude(targetPos - m_element.pad.anchoredPosition, m_maxRadiusBar);
    }

    void StopControll()
    {
        if (m_character?.isLive == true)
            m_character.SetState(TeamManager.instance.teamState);
    }

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
#endif

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public RectTransform pad;
        public RectTransform padBar;

        public Button btnAttack;
        public Button btnDash;

        public void Initialize(Transform _transform)
        {
            pad = (RectTransform)_transform.Find("Pad");
            padBar = (RectTransform)pad.Find("Bar");

            btnAttack = _transform.GetComponent<Button>("Action/btn_attack");
            btnDash = _transform.GetComponent<Button>("Action/btn_dash");
        }
    }
}
