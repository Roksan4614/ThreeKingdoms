using UnityEngine;
using UnityEngine.Rendering;

public abstract class TargetComponent : MonoBehaviour, IValidatable
{
    public virtual bool isLive => true;

    [SerializeField]
    protected SortingGroup m_sortingGroup;
    [SerializeField]
    protected Canvas m_canvas;

    protected bool isSwitchSorting { get; set; } = true;

    public virtual void OnManualValidate()
    {
        m_sortingGroup = transform.GetComponent<SortingGroup>("Character");

        m_canvas = transform.GetComponent<Canvas>("Character/Canvas");
        if (m_canvas != null)
            m_canvas.sortingOrder = m_sortingGroup.sortingOrder + 1;
    }

    private void LateUpdate()
    {
        UpdateSortingOreder();
    }

    float m_prevPosY;
    public void UpdateSortingOreder()
    {
        if (m_prevPosY != transform.position.y && isSwitchSorting == true)
        {
            m_prevPosY = transform.position.y;
            m_sortingGroup.sortingOrder = (int)(transform.position.y * -10f);
            if (m_canvas != null)
                m_canvas.sortingOrder = m_sortingGroup.sortingOrder + 1;
        }
    }

    public int sortingOrder => m_sortingGroup.sortingOrder;
}
