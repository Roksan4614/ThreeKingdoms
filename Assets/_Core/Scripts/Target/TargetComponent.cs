using UnityEngine;
using UnityEngine.Rendering;

public abstract class TargetComponent : MonoBehaviour
{
    public virtual bool isLive => true;

    protected SortingGroup m_sortingGroup;
    protected Canvas m_canvas;

    protected virtual void Awake()
    {
        m_sortingGroup = transform.GetComponent<SortingGroup>();
        m_canvas = transform.GetComponent<Canvas>("Character/Canvas");
    }
    private void LateUpdate()
    {
        UpdateSortingOreder();
    }

    float m_prevPosY;
    public void UpdateSortingOreder()
    {
        if (m_prevPosY != transform.position.y)
        {
            m_prevPosY = transform.position.y;
            m_sortingGroup.sortingOrder = (int)(transform.position.y * -10f);
            m_canvas.sortingOrder = m_sortingGroup.sortingOrder + 1;
        }
    }
}
