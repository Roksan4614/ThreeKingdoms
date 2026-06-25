using UnityEngine;

public class ArrowNaviComponent : Singleton<ArrowNaviComponent>, IValidatable
{
    [SerializeField] bool m_isOnlyRot;

    Transform m_target;

    private void Start()
    {
        m_element.arrow.gameObject.SetActive(false);
    }

    public void SetParent(Transform _parent)
    {
        transform.SetParent(_parent);
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;

    }
    public void SetTarget(Transform _target) => m_target = _target;

    private void FixedUpdate()
    {
        if (m_target == null)
            return;

        var lookAt = m_target.position - transform.position;
        float angle = Mathf.Atan2(lookAt.y, lookAt.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if (m_isOnlyRot == false)
        {
            var targetPos = m_target.position;

            var lt = PopupManager.instance.lt;
            var rb = PopupManager.instance.rb;

            bool isActive = true;
            if (targetPos.x > rb.x + 1)
                targetPos.x = rb.x;
            else if (targetPos.x < lt.x - 1)
                targetPos.x = lt.x;
            else
                isActive = false;

            if (targetPos.y < rb.y)
                targetPos.y = rb.y;
            else if (targetPos.y > lt.y)
                targetPos.y = lt.y;
            else if (isActive == false)
            {
                m_element.arrow.gameObject.SetActive(false);
                return;
            }

            var scale = m_element.arrow.localScale;
            scale.y = lookAt.x > 0 ? 1 : -1;
            m_element.arrow.localScale = scale;

            m_element.arrow.gameObject.SetActive(true);
            m_element.arrow.transform.position = targetPos;
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Transform arrow;
        public void Initialize(Transform _transform)
        {
            arrow = _transform.Find("imgArrow");
        }
    }
    #endregion VALIDATE

}
