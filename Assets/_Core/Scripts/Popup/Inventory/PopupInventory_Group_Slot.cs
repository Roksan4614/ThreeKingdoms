using Cysharp.Threading.Tasks;
using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Rev9.Inventory
{
    public class PopupInventory_Group_Slot : MonoBehaviour, IValidatable
    {
        public int idxTrigger => transform.GetSiblingIndex();
        public ItemData itemData { get; private set; }
        public Transform icon => m_element.icon;

        public UnityAction<ItemData> onClick { private get; set; }

        public void InitializeItem(ItemType _itemType, string _value = null)
        {
            name = _itemType.ToString();

            string key = _itemType.ToString();
            if (_value != null)
                key += "_" + _value;

            SetIconAsync(key).Forget();

            itemData = TableManager.item.GetItemData(_itemType);
            itemData.value = _value;

            m_element.button.onClick.AddListener(() => onClick(itemData));

            RefreshCount();

            Signal.instance.Inventory_UpdateCount.connectLambda = new(this, _itemData =>
            {
                if (itemData.key == _itemData.key && itemData.value == _itemData.value)
                    RefreshCount();
            });
        }

        void RefreshCount()
        {
            long count = InventoryWorker.instance.GetItemCount(itemData);
            m_element.txtAmount.text = count.AmountKMBT(_isMBT: true);

            // 0개면 안보여줄까 고민중
            //if (count == 0)
            //    gameObject.SetActive(false);
            //else
            //{
            //    m_element.txtAmount.text = count.AmountKMBT(_isMBT: true);

            //    if( gameObject.activeSelf == false)
            //    {
            //        gameObject.SetActive(true);


            //    }
            //}
        }

        async UniTask SetIconAsync(string _key)
        {
            var parent = m_element.icon;

            for (int i = 0; i < parent.childCount; i++)
                Destroy(parent.GetChild(i).gameObject);

            parent.gameObject.SetActive(false);
            var resource = await AddressableManager.instance.GetItemIconAsync(_key);

            if (resource != null)
            {
                parent.gameObject.SetActive(true);
                var icon = Instantiate(resource, parent);
                icon.transform.localPosition = Vector3.zero;
                icon.AutoResizeParent();
            }
        }

        public UnityAction<PopupInventory_Group_Slot, bool> actionTrigger { get; set; }

        private void OnTriggerEnter2D(Collider2D _collision)
        {
            if (_collision.tag == "Pointer" && ControllerManager.instance.isKeyboardMode)
                actionTrigger?.Invoke(this, true);
        }

        private void OnTriggerExit2D(Collider2D _collision)
        {
            if (_collision.tag == "Pointer" && ControllerManager.instance.isKeyboardMode)
                actionTrigger?.Invoke(this, false);
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public Button button;
            public TextMeshProUGUI txtAmount;
            public Transform icon;

            public void Initialize(Transform _transform)
            {
                button = _transform.GetComponent<Button>();
                txtAmount = _transform.GetComponent<TextMeshProUGUI>("txt_amount");
                icon = _transform.Find("Icon");
            }
        }
        #endregion VALIDATE

    }
}
