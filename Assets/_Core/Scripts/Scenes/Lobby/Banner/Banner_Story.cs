using UnityEngine;

public class Banner_Story : MonoBehaviour, IValidatable
{
    private void Awake()
    {

    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper button;
        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<ButtonHelper>();
        }
    }
    #endregion VALIDATE

}
