using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Controller_Attack : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IValidatable
{
    [SerializeField] float m_power = 7f;

    Vector3 m_posStart;

    public Button button;
    Transform m_pointer;
    CharacterComponent m_hero;

    private void Start()
    {
        button = transform.GetComponent<Button>();
        m_pointer = m_element.pointer.transform;

        Signal.instance.ConnectMainHero.connectLambda = new(this, _hero => m_hero = _hero);
    }

    public void OnDrag(PointerEventData eventData)
    {
        var pos = CameraManager.instance.GetMousePosition();

        var dist = (m_posStart - pos);

        if (dist.sqrMagnitude > 0.1f || m_pointer.gameObject.activeSelf == true)
        {
            button.interactable = false;
            m_pointer.gameObject.SetActive(true);

            m_pointer.position =
                m_hero.transform.position + ((pos - m_posStart).normalized * dist.magnitude * m_power);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        m_posStart = CameraManager.instance.GetMousePosition();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable == false)
        {
            Utils.AfterSecond(() =>
            {
                button.interactable = true;
            });

            var target = m_element.pointer.enemy;
            if (target != null && m_hero.target.target != target)
                m_hero.move.MoveTarget(target, true);

            m_element.pointer.gameObject.SetActive(false);
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Controll_Attack_Pointer pointer;
        public void Initialize(Transform _transform)
        {
            pointer = _transform.GetComponent<Controll_Attack_Pointer>("MousePosition");
        }
    }
    #endregion VALIDATA
}
