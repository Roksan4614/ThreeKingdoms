using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LobbyScreen_Shop_Group : MonoBehaviour
{
    protected List<LobbyScreen_Shop_Group_Slot> m_slots = new();
    public List<LobbyScreen_Shop_Group_Slot> slots => m_slots;
    public ShopCategoryType category => m_slots[0].category;
    public UnityAction<LobbyScreen_Shop_Group_Slot> actionProduct { get; set; }

    public float posY => ((RectTransform)transform).anchoredPosition.y * -1;

    Transform m_panel;

    public void Initialize(params TableShopProductData[] _products)
    {
        SetTitle(_products[0]);

        for (int i = 0; i < _products.Length; i++)
        {
            var slot = (i == m_panel.childCount ? Instantiate(m_panel.GetChild(0), m_panel) : m_panel.GetChild(i))
                .GetComponent<LobbyScreen_Shop_Group_Slot>();

            slot.SetProductData(_products[i]);
            slot.actionProduct = actionProduct;
            m_slots.Add(slot);
        }
    }

    void SetTitle(TableShopProductData _product)
    {
        var category = _product.shop_category_type;
        transform.SetText("txt_title", TableManager.stringShop.GetString($"CATEGORY_{category.ToString().ToUpper()}"));

        if (category == ShopCategoryType.Package || category == ShopCategoryType.Pass)
            m_panel = transform.Find($"Panel_{category.ToString()}");
        else
            m_panel = transform.Find("Panel");
    }
}
