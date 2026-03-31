using System;
using System.Collections.Generic;
using System.Reflection;
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

    public virtual void OnDrag(PointerEventData eventData)
    {
        var mousePosition = CameraManager.instance.GetMousePosition();

        var dist = (m_element.startPosition.position - mousePosition);

        if (dist.sqrMagnitude > 0.02f || m_pointer.gameObject.activeSelf == true)
        {
            button.interactable = false;
            m_pointer.gameObject.SetActive(true);

            var targetPos = CameraManager.instance.main.transform.position +
                ((mousePosition - m_element.startPosition.position).normalized * dist.sqrMagnitude * m_power);

            targetPos.z = m_pointer.position.z;

            m_pointer.position = targetPos;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        m_element.startPosition.position = CameraManager.instance.GetMousePosition();
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable == false)
        {
            Utils.AfterSecond(() => button.interactable = true);

            if (m_target != null && m_target.isLive && m_hero.target.target != m_target)
                m_hero.move.MoveTarget(m_target, true);

            m_element.pointer.gameObject.SetActive(false);
        }
    }

    #region VALIDATE
    public virtual void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [Serializable]
    protected struct ElementData
    {
        public Controll_Attack_Pointer pointer;
        public Transform startPosition;

        public void Initialize(Transform _transform)
        {
            pointer = _transform.GetComponent<Controll_Attack_Pointer>("MousePosition");
            startPosition = _transform.Find("StartPosition");
        }
    }
    #endregion VALIDATA
}
