using UnityEngine;
using UnityEngine.Rendering;

public class TargetComponent : MonoBehaviour, IValidatable
{
    public virtual bool isLive => true;

    [SerializeField, HideInInspector]
    protected SortingGroup m_sortingGroup;
    [SerializeField, HideInInspector]
    protected Canvas m_canvas;

    protected bool isSwitchSorting { get; set; } = true;

    public virtual void OnManualValidate()
    {
        m_sortingGroup = transform.GetComponent<SortingGroup>("Character");

        m_canvas = transform.GetComponent<Canvas>("Character/Canvas");
        if (m_canvas != null)
            m_canvas.sortingOrder = m_sortingGroup.sortingOrder + 1;
    }

    protected virtual void LateUpdate()
    {
        UpdateSortingOreder();
    }

    float m_prevPosY;
    public void UpdateSortingOreder(bool _isForce = false)
    {
        if ((m_prevPosY != transform.position.y && isSwitchSorting == true) || _isForce == true)
        {
            m_prevPosY = transform.position.y;
            m_sortingGroup.sortingOrder = (int)(transform.position.y * -100f);
            if (m_canvas != null)
                m_canvas.sortingOrder = m_sortingGroup.sortingOrder + 1;
        }
    }

    public int sortingOrder => m_sortingGroup.sortingOrder;
}
