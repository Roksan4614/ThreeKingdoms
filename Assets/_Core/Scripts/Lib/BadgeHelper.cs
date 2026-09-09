using TMPro;
using UnityEngine;

public class BadgeHelper : MonoBehaviour, IValidatable
{
    public string text
    {
        get => m_element.txtContent.text;
        set
        {
            m_element.txtContent.text = value;
            m_element.txtContent.transform.ForceRebuildLayout(1);
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtContent;
        public void Initialize(Transform _transform)
        {
            txtContent = _transform.GetComponent<TextMeshProUGUI>("Text");
        }
    }
    #endregion VALIDATE

}
