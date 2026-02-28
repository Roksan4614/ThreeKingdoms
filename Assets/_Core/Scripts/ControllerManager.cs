using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ControllerManager : Singleton<ControllerManager>, IPointerDownHandler, IPointerUpHandler, IDragHandler, IValidatable
{
    [SerializeField] float m_maxRadiusBar = 150;
    [SerializeField] float m_startRotZ = 20;

    CharacterComponent m_character;

    public bool isActive => m_element.pad.gameObject.activeSelf || m_isKeyboardDoing;

    private void Start()
    {
        m_element.pad.gameObject.SetActive(false);

        Signal.instance.ConnectMainHero.connectLambda = new(this, _ => m_character = _);

    }

    bool m_isKeyboardDoing = false;
    private void Update()
    {
        if (m_character?.isLive == false)
            return;

        var lookAt = Vector2.zero;
        if (m_element.pad.gameObject.activeSelf == true)
            lookAt = m_element.padBar.position - m_element.pad.position;
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                lookAt = Vector2.left;
            else if (Input.GetKey(KeyCode.RightArrow))
                lookAt = Vector2.right;

            if (Input.GetKey(KeyCode.UpArrow))
                lookAt += Vector2.up;
            else if (Input.GetKey(KeyCode.DownArrow))
                lookAt += Vector2.down;
        }

        if (lookAt != Vector2.zero)
        {
            // 대쉬
            if (Input.GetKeyDown(KeyCode.D))
                m_character.move.Dash();

            m_character.OnConrollerMove(lookAt);
            m_isKeyboardDoing = true;
        }
        else if (m_isKeyboardDoing == true)
        {
            m_isKeyboardDoing = false;
            StopControll();
        }

        // 공격
        if (Input.GetKeyDown(KeyCode.A))
            m_character.attack.ControlAttack();
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

        m_element.pad.rotation = Quaternion.Euler(0, 0, m_startRotZ);
        m_element.pad.DORotate(Vector3.zero, 0.1f).SetEase(Ease.OutBack);

        m_element.padBar.localPosition = Vector3.zero;

        OnDrag(_eventData);
    }

    public void OnPointerUp(PointerEventData _eventData)
    {
        if (isActive == false)
            return;
        StopControll();

        m_element.pad.gameObject.SetActive(false);
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

        public void Initialize(Transform _transform)
        {
            pad = (RectTransform)_transform.Find("Pad");
            padBar = (RectTransform)pad.Find("Bar");
        }
    }
}
