using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class HeroInfoComponent : IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    float m_power = 2f;
    float m_magnitude;

    bool m_isReady = false;

    int m_pointerId = -100;

    public void OnDrag(PointerEventData _eventData)
    {
        if (m_hero.isLive == false || m_hero.element.skillRange == null || m_pointerId != _eventData.pointerId)
            return;

        var mousePosition = CameraManager.GetPosPointer(m_pointerId);
        var dist = (m_element.startPosition.position - mousePosition);

        if (dist.sqrMagnitude > 0.5f)
        {
            m_isReady = true;
            m_element.startPosition.gameObject.SetActive(true);
            m_element.button.interactable = false;
        }
        else if (m_element.button.interactable == false)
        {
            m_isReady = false;
            m_hero.attack.OnCancel_ControllSkill();
        }
    }

    public void OnPointerDown(PointerEventData _eventData)
    {
        if (m_statusSkill != StatusType.Valid)
        {
            _eventData.pointerDrag = null;
            return;
        }
        m_pointerId = _eventData.pointerId;
        m_element.startPosition.position = CameraManager.GetPosPointer(m_pointerId);
    }

    public void OnPointerUp(PointerEventData _eventData)
    {
        if (m_pointerId != _eventData.pointerId)
            return;

        if (m_element.button.interactable == false)
        {
            m_isReady = false;
            Utils.AfterSecond(() => m_element.button.interactable = true);

            m_element.startPosition.gameObject.SetActive(false);
            m_hero.attack.OnUp_ControllSkill();
        }

        m_pointerId = -100;
    }

    public void Update()
    {
        if (m_isReady == false)
            return;

        var startPosition = m_element.startPosition.position;
        var mousePosition = CameraManager.GetPosPointer(m_pointerId);
        var dist = (startPosition - mousePosition);

        if (Mathf.Approximately(m_magnitude, dist.sqrMagnitude) == false)
        {
            m_magnitude = dist.sqrMagnitude;
            var targetPos = m_hero.transform.position +
                ((mousePosition - startPosition).normalized * dist.sqrMagnitude * m_power);

            targetPos.z = 0;
            m_hero.attack.OnDrag_ControllSkill(targetPos);
        }
    }
}
