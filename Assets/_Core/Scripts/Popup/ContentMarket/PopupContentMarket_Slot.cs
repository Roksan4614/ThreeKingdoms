using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupContentMarket_Slot : MonoBehaviour, IValidatable
{
    TableItemData m_itemData;
    public void SetProductData(TableItemData _itemData, UnityAction<TableItemData> _onClick)
    {
        m_itemData = _itemData;

        var btn = transform.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => _onClick(m_itemData));

        if (_itemData.count == 0)
            _itemData.count = 1;

        m_element.item.SetItemData(_itemData);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtCost;
        public ItemComponent item;

        public void Initialize(Transform _transform)
        {
            item = _transform.GetComponent<ItemComponent>("Panel/Item");
            txtCost = _transform.GetComponent<TextMeshProUGUI>("Panel/Cost/Text");
        }
    }
    #endregion VALIDATE

}
