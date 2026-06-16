using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Controller_Attack : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IValidatable
{
    protected float m_power = 2f;

    protected Button button;
    protected Transform m_pointer;

    protected CharacterComponent m_hero;
    CharacterComponent m_target;

    protected int m_pointerId;
    protected bool m_isPointerDown;

    protected virtual void Start()
    {
        button = transform.GetComponent<Button>();
        m_pointer = m_element.pointer.transform;

        Signal.instance.ConnectMainHero.connectLambda = new(this, _hero => m_hero = _hero);

        m_element.pointer.OnTriggerEnter = SlotTriggerEnter;
        m_element.pointer.OnTriggerEnter = SlotTriggerExit;
    }

    protected virtual void SlotTriggerEnter(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
            m_target = _collision.transform.parent.parent.parent.GetComponent<Character_Enemy>();
    }

    protected virtual void SlotTriggerExit(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
        {
            if (m_target == _collision.transform.parent.parent.parent.GetComponent<Character_Enemy>())
                m_target = null;
        }
    }

    public virtual void OnDrag(PointerEventData _eventData)
    {
        if (m_pointerId != _eventData.pointerId)
        {
            _eventData.pointerDrag = null;
            return;
        }

        var mousePosition = CameraManager.GetPosPointer(m_pointerId);

        var dist = (m_element.startPosition.position - mousePosition);

        if (dist.sqrMagnitude > 0.05f || m_pointer.gameObject.activeSelf == true)
        {
            button.interactable = false;
            m_pointer.gameObject.SetActive(true);

            var targetPos = CameraManager.instance.main.transform.position +
                ((mousePosition - m_element.startPosition.position).normalized * dist.sqrMagnitude * m_power);

            targetPos.z = m_pointer.position.z;

            m_pointer.position = targetPos;
        }
    }

    public virtual void OnPointerDown(PointerEventData _eventData)
    {
        if (m_isPointerDown == true)
        {
            _eventData.pointerDrag = null;
            return;
        }

        m_isPointerDown = true;

        m_pointerId = _eventData.pointerId;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 startPos);
        m_element.startPosition.anchoredPosition = startPos;
    }

    public virtual void OnPointerUp(PointerEventData _eventData)
    {
        if (m_pointerId != _eventData.pointerId)
            return;

        if (button.interactable == false)
        {
            Utils.AfterSecond(() => button.interactable = true);

            if (m_target != null && m_target.isLive && m_hero.target.target != m_target)
                m_hero.move.MoveTarget(m_target, true);

            m_element.pointer.gameObject.SetActive(false);
        }

        m_isPointerDown = false;
    }

    #region VALIDATE
    public virtual void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [Serializable]
    protected struct ElementData
    {
        public Controll_Attack_Pointer pointer;
        public RectTransform startPosition;
        public Transform updatePosition;

        public void Initialize(Transform _transform)
        {
            pointer = _transform.GetComponent<Controll_Attack_Pointer>("MousePosition");
            startPosition = (RectTransform)_transform.Find("StartPosition");
        }
    }
    #endregion VALIDATA
}
