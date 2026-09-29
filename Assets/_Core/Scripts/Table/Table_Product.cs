using Newtonsoft.Json;
using Rev9.ContentsMarket;
using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;

public class Table_ShopProduct : BaseTable<string, TableShopProductData>
{
    public Table_ShopProduct(List<TableShopProductData> _table) : base(_table)
    {
        m_list = m_list.FindAll(x => x.is_active > 0).SortBy(x => x.display_order);
    }

    public TableShopProductData GetProductData(string _key)
        => m_list.Find(x => x.key.Equals(_key));
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
    public string reward_item_key;
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
    [JsonProperty("key")]
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
                    m_rewards = TableManager.shopProductReward.GetProductRewards(reward_item_key).Select(x => x.itemData).ToArray();
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
}

[JsonObject(MemberSerialization.OptIn)]
public class TableProductData
{
    [JsonProperty] public int idx;
    //[JsonProperty] public string key;
    [JsonProperty("reward_item_key")]
    //[JsonProperty]
    public string reward_item_key;
    [JsonProperty] public int reward_count;
    [JsonProperty] public int price;
    [JsonProperty] public PayType pay_type;
    [JsonProperty] public LimitResetType limit_reset_type;

    [JsonProperty] public int buy_limit;
    [JsonProperty] public int display_order;
    [JsonProperty] public int is_active;

    [JsonProperty] public int countBuy;


    PeriodType? period_type;
    public PeriodType periodType
    {
        get => period_type ?? PeriodType.Daily;
        set => period_type = value;
    }

    public bool hasLimit => buy_limit > 0;
    public int remainCount => buy_limit - countBuy;
    public string strRemainCount => $"{remainCount}/{buy_limit}";

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

    public long currencyMyCount
    {
        get
        {
            switch (pay_type)
            {
                case PayType.Rice:
                case PayType.FreeGold:
                    return DataManager.userInfo.GetAssetAmount(pay_type);
                default:
                    return InventoryWorker.instance.GetItemCount(itemData);
            }
        }
    }
}
