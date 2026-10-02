using Cysharp.Threading.Tasks;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class Data_Shop
{
    public async UniTask InitializeAsync()
    {

    }

    public void OpenURL_GoldPaid(int _price)
    {
        //Utils.OpenUrl("https://naver.com");

#if SERVICE_DEV
        PopupManager.instance.AlertShow($"EDIT: 원보 {_price}개 추가");
        InventoryWorker.instance.AddItem(_itemData: TableManager.item.GetItemData(ItemKey.GoldPaid, _price));
#endif
    }

    public async UniTask<bool> API_BuyItemAsync(TableShopProductData _productData)
    {
        await RewardWorker.OpenRewardPopupAsync(_productData.rewards);

        InventoryWorker.instance.UseItem(_productData.currencyKey, _productData.price);
        return true;
    }
}

public class ShopHistoryData
{

}