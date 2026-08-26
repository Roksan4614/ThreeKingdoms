using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rev9.ContentsMarket
{
    public class PopupContentsMarket_Slot : MonoBehaviour, IValidatable
    {
        public void SetProductData(ContentsMarketProductData _productData, UnityAction<ContentsMarketProductData> _onClick)
        {
            gameObject.SetActive(true);

            var btn = transform.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => _onClick(_productData));

            m_element.txtCost.text = _productData.cost.AmountKMBT(_isMBT: true);
            m_element.txtCost.transform.ForceRebuildLayout();

            for (int i = 0; i < m_element.iconCost.childCount; i++)
            {
                var icon = m_element.iconCost.GetChild(i).gameObject;
                icon.SetActive(icon.name == _productData.costType.ToString());
            }

            m_element.item.SetItemData(_productData.itemData);

            bool isClose = _productData.remainCount == 0;
            m_element.objClose.SetActive(isClose);

            bool isLimit = _productData.isLimit == true && isClose == false;
            m_element.txtCount.transform.parent.gameObject.SetActive(isLimit);

            if (isLimit == true)
            {
                string peroidType = TableManager.stringTable.GetString("PEROID_TYPE_" + _productData.peroidType.ToString().ToUpper());
                m_element.txtCount.text = $"{peroidType} {_productData.strRemainCount}";
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
            public TextMeshProUGUI txtCost;
            public TextMeshProUGUI txtCount;
            public ItemComponent item;

            public GameObject objClose;

            public Transform iconCost;

            public void Initialize(Transform _transform)
            {
                item = _transform.GetComponent<ItemComponent>("Panel/Item");
                txtCost = _transform.GetComponent<TextMeshProUGUI>("Panel/Cost/Text");
                txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/Count/Text");

                objClose = _transform.Find("Close").gameObject;

                iconCost = txtCost.transform.Find("Icon");
            }
        }
        #endregion VALIDATE

    }
}