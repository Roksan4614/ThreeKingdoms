using TMPro;
using UnityEngine;

public class UIPowerHelper : MonoBehaviour, IValidatable
{
    protected virtual void Start()
    {
        textBadge = "POW";
    }

    public string text { set => m_element.txtValue.text = value; }
    public string textBadge { set => m_element.txtBadge.text = value; }


    #region VALIDATE
    public virtual void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtValue;
        public TextMeshProUGUI txtBadge;

        public void Initialize(Transform _transform)
        {
            txtValue = _transform.GetComponent<TextMeshProUGUI>("Text");
            txtBadge = _transform.GetComponent<TextMeshProUGUI>("Badge/Text");
        }
    }
    #endregion
}
