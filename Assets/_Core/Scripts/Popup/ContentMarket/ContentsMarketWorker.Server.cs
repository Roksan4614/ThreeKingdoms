using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Enums;
using ThreeKingdoms.Shared.Rest;
using ThreeKingdoms.Shared.Types;

namespace Rev9.ContentsMarket
{
    public partial class ContentsMarketWorker
    {
        private bool m_serverBuying;
        private string m_pendingPurchase;
        private RestRequestOptions m_pendingPurchaseOptions;
        private readonly HashSet<ContentsMarketTabType> m_serverEnabledShops = new();

        private async UniTask InitializeServerAsync()
        {
            var response = await GameServer.ContentShop.LobbyAsync(new ContentShopLobbyReq(), GameServer.Options());
            var catalog = response.Data ?? throw new InvalidOperationException("Content shop response is empty.");
            m_db = new();
            m_serverEnabledShops.Clear();
            foreach (ContentsMarketTabType tab in new[] { ContentsMarketTabType.Daily, ContentsMarketTabType.Raid, ContentsMarketTabType.Tournament })
                m_db[tab] = new();
            foreach (var shop in catalog.Shops)
            {
                var tab = ToTab(shop.ShopType);
                m_db[tab] = shop.Products.OrderBy(x => x.DisplayOrder).Select(ToProduct).ToList();
                if (shop.PurchaseEnabled) m_serverEnabledShops.Add(tab);
            }
        }

        private static ContentsMarketTabType ToTab(ContentShopType type) => type switch
        {
            ContentShopType.Daily => ContentsMarketTabType.Daily,
            ContentShopType.Raid => ContentsMarketTabType.Raid,
            ContentShopType.Tournament => ContentsMarketTabType.Tournament,
            _ => throw new InvalidOperationException("Unknown server shop: " + type)
        };

        private static ContentShopType ToShop(ContentsMarketTabType tab) => tab switch
        {
            ContentsMarketTabType.Daily => ContentShopType.Daily,
            ContentsMarketTabType.Raid => ContentShopType.Raid,
            ContentsMarketTabType.Tournament => ContentShopType.Tournament,
            _ => throw new InvalidOperationException("Unknown client shop: " + tab)
        };

        private static ContentsMarketProductData ToProduct(ContentShopProductDto product)
        {
            var item = ServerState.ToItem(product.RewardItemId, product.RewardCount);
            var costType = product.PayType switch
            {
                PayType.Rice => ItemType.rice,
                PayType.FreeGold or PayType.PaidGold => ItemType.gold,
                PayType.RaidPoint => ItemType.raid_point,
                PayType.TournamentPoint => ItemType.tournament_point,
                _ => throw new InvalidOperationException("Unknown shop currency: " + product.PayType)
            };
            return new ContentsMarketProductData
            {
                idx = checked((int)product.ProductId), key = item.key, value = item.value,
                count = checked((int)product.RewardCount), cost = checked((int)product.UnitPrice),
                costType = costType, countMax = checked((int)product.BuyLimit),
                countBuy = checked((int)product.BoughtCount)
            };
        }

        private async UniTask<bool> BuyServerAsync(ContentsMarketTabType tab, ContentsMarketProductData product, int quantity)
        {
            if (m_serverBuying) return false;
            m_serverBuying = true;
            try
            {
                if (!m_serverEnabledShops.Contains(tab)) throw new InvalidOperationException("This shop is not available.");
                if (quantity < 1) throw new InvalidOperationException("Select at least one product.");
                var purchase = $"{tab}:{product.idx}:{quantity}";
                if (m_pendingPurchase != purchase || m_pendingPurchaseOptions == null)
                {
                    m_pendingPurchase = purchase;
                    m_pendingPurchaseOptions = GameServer.Options();
                }
                var response = await GameServer.ContentShop.BuyAsync(new ContentShopBuyReq
                {
                    ShopType = ToShop(tab), ProductId = product.idx, Quantity = quantity
                }, m_pendingPurchaseOptions);
                var result = response.Data ?? throw new InvalidOperationException("Content shop purchase response is empty.");
                ServerState.ApplyAsset(result.Asset);
                ServerState.ApplyItems(result.ItemUpdates);
                if (result.CharacterSnapshot != null) await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                var updated = ToProduct(result.Product);
                var index = m_db[tab].FindIndex(x => x.idx == updated.idx);
                if (index >= 0) m_db[tab][index] = updated;
                m_pendingPurchase = null;
                m_pendingPurchaseOptions = null;
                return true;
            }
            catch (Exception error)
            {
                // Business failures have a stored receipt too. Once the server
                // definitively rejected a purchase, a later attempt needs a new ID.
                if (error is GameServerException server && server.HttpStatus >= 200 && server.HttpStatus < 500
                    && server.Code != "SERVER_RESPONSE_INVALID" && server.Code != "SERVER_HTTP_ERROR")
                {
                    m_pendingPurchase = null;
                    m_pendingPurchaseOptions = null;
                }
                PopupManager.instance.AlertShow(error.Message);
                return false;
            }
            finally { m_serverBuying = false; }
        }
    }
}
