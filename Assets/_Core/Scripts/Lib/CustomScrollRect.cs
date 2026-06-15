using UnityEngine;
using UnityEngine.EventSystems;

public class CustomScrollRect : UnityEngine.UI.ScrollRect
{
    private int m_pointerId = -1;
    private bool m_isDragging = false;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        // 이미 드래그 중이라면 추가되는 터치는 무시
        if (m_isDragging) return;

        m_pointerId = eventData.pointerId;
        m_isDragging = true;

        base.OnBeginDrag(eventData);
    }
    public override void OnDrag(PointerEventData eventData)
    {
        if (m_isDragging && eventData.pointerId == m_pointerId)
            base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (m_isDragging && eventData.pointerId == m_pointerId)
        {
            m_isDragging = false;
            m_pointerId = -1;

            base.OnEndDrag(eventData);
        }
    }

}
