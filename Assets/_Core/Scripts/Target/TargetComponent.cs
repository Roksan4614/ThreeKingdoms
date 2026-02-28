using UnityEngine;
using UnityEngine.Rendering;

public abstract class TargetComponent : MonoBehaviour, IValidatable
{
    public virtual bool isLive => true;

    [SerializeField]
    protected SortingGroup m_sortingGroup;
    [SerializeField]
    protected Canvas m_canvas;

#if UNITY_EDITOR
    public virtual void OnManualValidate()
    {
        m_canvas = transform.GetComponent<Canvas>("Character/Canvas");
        m_sortingGroup = transform.GetComponent<SortingGroup>();
    }
#endif

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

    public int sortingOrder => m_sortingGroup.sortingOrder;
}
