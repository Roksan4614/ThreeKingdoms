using UnityEngine;

public class LobbyScreen_Shop_Group_SlotPackage : LobbyScreen_Shop_Group_Slot
{
    public override void SetProductData(TableShopProductData _productData)
    {
        base.SetProductData(_productData);

        //transform.SetText("Panel/txt_limit", TableManager.stringTable.GetStringFormat("UI_BUY_REMAIN_COUNT", ));
    }
}
