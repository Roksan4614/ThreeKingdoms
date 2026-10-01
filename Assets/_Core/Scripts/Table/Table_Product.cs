using Newtonsoft.Json;
using Rev9.ContentsMarket;
using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;

public class Table_ShopProduct : BaseTable<string, TableShopProductData>
{
    public Table_ShopProduct(List<TableShopProductData> _table) : base(_table)
    {
        m_list = m_list.FindAll(x => x.isActive).SortBy(x => x.display_order);
    }

    public TableShopProductData GetProductData(string _key)
        => m_list.Find(x => x.key.Equals(_key));

    public TableShopProductData GetBattlePass()
    {
        foreach (var p in m_list)
        {
            if (p.product_type == ShopProductType.Pass)
                return p;
        }
        return null;
    }

    public List<TableShopProductData> GetProducts(ShopCategoryType _categoryType)
        => m_list.FindAll(x => x.shop_category_type == _categoryType);
}

public class Table_ShopProductReward : BaseTable<string, TableShopProductRewardData>
{
    Dictionary<string, TableShopProductRewardData[]> m_grop = new();

    public Table_ShopProductReward(List<TableShopProductRewardData> _table) : base(_table)
    {
        m_grop = m_list.GroupBy(x => x.shop_product_key).ToDictionary(x => x.Key, x => x.ToArray());
    }

    public TableShopProductRewardData[] GetProductRewards(string _key)
        => m_grop.ContainsKey(_key) ? m_grop[_key] : new TableShopProductRewardData[0];
}

public class TableShopProductRewardData
{
    public string shop_product_key;
    public ItemKey reward_item_key;
    public int reward_count;

    ItemData m_itemData;
    public ItemData itemData
    {
        get
        {
            if (m_itemData == null)
                m_itemData = TableManager.item.GetItemData(reward_item_key, reward_count);
            return m_itemData;
        }
    }
}

public class TableShopProductData : TableProductData
{
    public string key;
    public ShopCategoryType shop_category_type;
    public ShopProductType product_type;

    public string store_product_key;

    ItemData[] m_rewards;
    public ItemData[] rewards
    {
        get
        {
            if (m_rewards == null)
            {
                if (product_type == ShopProductType.Package)
                {
                    m_rewards = TableManager.shopProductReward.GetProductRewards(key).Select(x => x.itemData).ToArray();
                }
                else
                {
                    m_rewards = new ItemData[1];
                    m_rewards[0] = itemData;
                }

                if (m_rewards == null)
                    m_rewards = new ItemData[0];
            }
            return m_rewards;
        }
    }

    public string name
        => TableManager.stringShop.GetString($"NAME_{key.ToUpper()}");
    public string desc
        => TableManager.stringShop.GetString($"DESC_{key.ToUpper()}");
}

public class TableProductData
{
    public int idx;
    public ItemKey? reward_item_key;
    public int reward_count;
    public int price;
    public PayType pay_type;
    public LimitResetType limit_reset_type;

    public int buy_limit;
    public int display_order;
    public int is_active;

    public int countBuy;

    public bool isActive => is_active > 0;
    public bool hasLimit => buy_limit > 0;
    public int remainCount => buy_limit - countBuy;
    public string strRemainCount => $"{remainCount}/{buy_limit}";

    ItemData m_itemData;
    public ItemData itemData
    {
        get
        {
            if (m_itemData == null && reward_item_key != null)
                m_itemData = TableManager.item.GetItemData(reward_item_key.Value, reward_count);
            return m_itemData;
        }
    }

    public bool isEnoughCurrency => currencyMyCount >= price || pay_type == PayType.Cash;

    public ItemKey currencyKey
    {
        get
        {
            switch (pay_type)
            {
                case PayType.Rice: return ItemKey.Rice;
                case PayType.GoldFree: return ItemKey.GoldFree;
                case PayType.GoldPaid: return ItemKey.GoldPaid;
                case PayType.PointRaid: return ItemKey.PointRaid;
                case PayType.PointTournament: return ItemKey.PointTournament;
                default:
                    return 0;
            }
        }
    }

    public long currencyMyCount
    {
        get
        {
            switch (pay_type)
            {
                case PayType.Rice:
                case PayType.GoldFree:
                    return DataManager.userInfo.GetAssetAmount(pay_type);
                case PayType.GoldPaid:
                case PayType.PointRaid:
                case PayType.PointTournament:
                    return InventoryWorker.instance.GetItemCount(currencyKey);
                default:
                    return 0;
            }
        }
    }
}
