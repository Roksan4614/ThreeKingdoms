using TMPro;
using UnityEngine;

public class PopupContentMarket_Popup_Buy : MonoBehaviour, IValidatable
{
    public bool CloseEscape()
    {
        if (gameObject.activeSelf)
        {
            Close();
            return true;
        }

        return false;
    }

    public void SetProductData(TableItemData _itemData)
    {
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);
    }

    void Close()
    {
        Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {

        public TextMeshProUGUI txtCount;
        public void Initialize(Transform _transform)
        {
            txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_count");
        }

        public Transform panel => txtCount.transform.parent;
    }
    #endregion VALIDATE

}
