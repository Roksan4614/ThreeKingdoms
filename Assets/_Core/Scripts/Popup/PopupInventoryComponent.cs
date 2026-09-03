using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Inventory
{
    public class PopupInventoryComponent : BasePopupComponent
    {
        ItemCategoryType m_curCategory;

        PopupInventoryComponent() : base(PopupType.Inventory) { }


        Dictionary<ItemCategoryType, ButtonHelper> m_tabs = new();
        public RectTransform panel => m_element.panel;

        bool m_isStarted = false;
        private void Start()
        {
            var sortCategory = InventoryWorker.instance.sortCategory;
            var content = m_element.scrollTab.content;

            // 일단 생성
            for (int i = 1; i < sortCategory.Count; i++)
                Instantiate(content.GetChild(0), content);

            for (int i = 0; i < sortCategory.Count; i++)
            {
                var type = sortCategory[i];
                var button = content.GetChild(i).GetComponent<ButtonHelper>();
                button.onClick.AddListener(() => SetTab(type));
                button.text = TableManager.stringItem.GetString("CATEGORY_" + type.ToString().ToUpper());
                button.SetDrawSelect(i == 0);

                m_tabs.Add(type, button);
            }

            m_curCategory = ItemCategoryType.NONE - 1;
            SetTab(ItemCategoryType.NONE);

            m_isStarted = true;
        }

        private void OnEnable()
        {
            if (m_isStarted == true)
            {
                m_element.scrollList.content.anchoredPosition =
                m_element.scrollTab.content.anchoredPosition = Vector2.zero;

                SetTab(ItemCategoryType.NONE);
            }
        }

        void SetTab(ItemCategoryType _category)
        {
            if (m_curCategory == _category)
                return;

            if (m_tabs.ContainsKey(m_curCategory))
                m_tabs[m_curCategory].SetDrawSelect(false);

            m_curCategory = _category;
            m_tabs[m_curCategory].SetDrawSelect(true);

            SetItemList();
        }

        void SetItemList()
        {
            var dbInventory = InventoryWorker.data;

            if (m_curCategory > ItemCategoryType.NONE)
                dbInventory = dbInventory.FindAll(x => x.category == m_curCategory);

            var content = m_element.scrollList.content;

            int i = 0;
            for (; i < dbInventory.Count; i++)
            {
                if (i > 0)
                    Instantiate(content.GetChild(0), content);
            }

            for (; i < content.childCount; i++)
                content.GetChild(i).gameObject.SetActive(false);

            content.ForceRebuildLayout();

            i = 0;
            for (; i < dbInventory.Count; i++)
            {
                var slot = content.GetChild(i).GetComponent<ItemComponent>();
                slot.gameObject.SetActive(true);
                slot.SetItemData(dbInventory[i]);

                var btn = slot.transform.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnButton_Item(slot.data));
            }

            m_element.txtEmpty.gameObject.SetActive(dbInventory.Count == 0);
        }
        void OnButton_Item(ItemData _itemData)
        {
            IngameLog.Add($"OnButton: {_itemData.nameValue}");
        }

        public override void Close()
        {
            Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
        }

        #region VALIDATE
        public override void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public ScrollRect scrollTab;
            public ScrollRect scrollList;

            public TextMeshProUGUI txtEmpty;

            public void Initialize(Transform _transform)
            {
                scrollTab = _transform.GetComponent<ScrollRect>("Panel/Tab");
                scrollList = _transform.GetComponent<ScrollRect>("Panel/List/Scroll");
                txtEmpty = scrollList.viewport.GetComponent<TextMeshProUGUI>("txt_empty");
            }
            public RectTransform panel => (RectTransform)scrollTab.transform.parent;

        }
        #endregion VALIDATE

    }
}