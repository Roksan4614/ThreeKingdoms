using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Rev9.Inventory
{
    public class PopupInventory_Group : MonoBehaviour
    {
        public UnityAction<ItemData> onClick { private get; set; }
        public UnityAction<PopupInventory_Group_Slot, bool> actionTrigger { get; set; }

        public void InitializeGroup(ItemCategoryType _category, List<ItemType> _items)
        {
            name = _category.ToString();
            transform.SetText("txt_title", TableManager.stringItem.GetString("CATEGORY_" + name.ToUpper()));

            for (int i = 0; i < _items.Count; i++)
            {
                var itemType = _items[i];

                //클래스 영혼석을 위함
                if (itemType == ItemType.class_soul_stone)
                {
                    for (var ct = HeroClassType.NONE + 1; ct < HeroClassType.MAX; ct++)
                    {
                        var slot = Instantiate(transform.GetChild(1), transform).GetComponent<PopupInventory_Group_Slot>();
                        slot.InitializeItem(_items[i], ct.ToString());
                        slot.onClick = _itemData => onClick(_itemData);
                        slot.actionTrigger = (_slot, _isEnter) => actionTrigger(_slot, _isEnter);
                    }
                }
                else
                {
                    var slot = (i == 0 ? transform.GetChild(1) : Instantiate(transform.GetChild(1), transform)).GetComponent<PopupInventory_Group_Slot>();
                    slot.InitializeItem(_items[i]);
                    slot.onClick = _itemData => onClick(_itemData);
                    slot.actionTrigger = (_slot, _isEnter) => actionTrigger(_slot, _isEnter);
                }
            }

            transform.ForceRebuildLayout();
        }
    }
}