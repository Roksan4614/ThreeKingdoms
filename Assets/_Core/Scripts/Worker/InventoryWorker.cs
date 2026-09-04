using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class InventoryWorker
{
    static InventoryWorker m_instance;
    public static InventoryWorker instance
    {
        get
        {
            if (m_instance == null)
                m_instance = new();
            return m_instance;
        }
    }
    public void Release()
    {
        m_instance = null;
    }

    List<InventoryItemData> m_data;
    public static List<InventoryItemData> data => instance.m_data;
    const string c_key = "pp_inventory";

    List<ItemCategoryType> m_sortCategory = new()
        {
            ItemCategoryType.NONE,
            ItemCategoryType.Currency,
            ItemCategoryType.Point,
            ItemCategoryType.Ticket,
            ItemCategoryType.Soul_Stone,
            ItemCategoryType.MAX,
        };
    public IReadOnlyList<ItemCategoryType> sortCategory => m_sortCategory;

    void SaveData()
    {
        m_data.Sort((x, y) => SortCompare(x, y));
        m_data = m_data.SortBy(x => m_sortCategory.FindIndex(s => s == x.category));

        PPWorker.Set(c_key, m_data);
    }

    public int SortCompare(ItemData x, ItemData y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int result = 0;

        // 영혼석
        if (x.key == ItemType.dedicated_soul_stone && y.key == ItemType.dedicated_soul_stone)
        {
            var heroX = DataManager.userInfo.GetHeroInfoData(x.value);
            var heroY = DataManager.userInfo.GetHeroInfoData(y.value);

            if (heroX.isMine != heroY.isMine)
                return heroX.isMine ? -1 : 1;

            result = CompareRegion(heroX, heroY);
            if (result != 0) return result;
            result = CompareClass(heroX, heroY);

            if (result != 0) return result;
        }
        // 클래스영혼
        else if (x.key == ItemType.class_soul_stone && y.key == ItemType.class_soul_stone)
        {
            HeroClassType classTypeX = System.Enum.Parse<HeroClassType>(x.value);
            HeroClassType classTypeY = System.Enum.Parse<HeroClassType>(y.value);

            result = classTypeX.CompareTo(classTypeY);
            if (result != 0) return result;
        }

        result = string.Compare(x.name, y.name, System.StringComparison.Ordinal);
        if (result != 0) return result;

        return 0;
    }
    private int CompareRegion(HeroInfoData x, HeroInfoData y)
    {
        bool isX = x.regionType == DataManager.userInfo.region;
        bool isY = y.regionType == DataManager.userInfo.region;

        if (isX == isY)
            return x.regionType.CompareTo(y.regionType);

        return isX ? -1 : 1;
    }
    private int CompareClass(HeroInfoData x, HeroInfoData y) => x.classType.CompareTo(y.classType);

    public async UniTask InitializeAsync()
    {
        m_data = PPWorker.Get<List<InventoryItemData>>(c_key);

        if (m_data == null)
        {
            m_data = new List<InventoryItemData>();
            SaveData();
        }

        await UniTask.NextFrame();
    }

    public long GetItemCount(ItemData _itemData)
        => m_data.Find(x => x.key == _itemData.key && x.value == _itemData.value)?.count ?? 0;


    public static void AddItem(ItemType _itemType, int _count, string _value = null, bool _isUpdate = true, bool _isTween = true, bool _isRewardAction = true, Vector3 _actionPosition = default)
    {
        AddItem(_isUpdate, _isTween, _isRewardAction, _actionPosition, TableManager.item.GetItemData(_itemType, _count, _value));
    }
    public static void AddItem(bool _isUpdate = true, bool _isTween = true, bool _isRewardAction = true, Vector3 _actionPosition = default, params ItemData[] _itemData)
    {
        if (_isRewardAction)
            RewardWorker.instance.RunAsync(_actionPosition, _itemData: _itemData).Forget();
        else
        {
            foreach (var item in _itemData)
            {
                switch (item.key)
                {
                    case ItemType.rice:
                    case ItemType.gold:
                        DataManager.userInfo.AddAsset(item.key, item.count, _isUpdate, _isTween);
                        break;
                    default:
                        var d = data.Find(x => x.key == item.key && x.value == item.value);

                        if (d == null)
                        {
                            d = JsonConvert.DeserializeObject<InventoryItemData>(Newtonsoft.Json.JsonConvert.SerializeObject(item));
                            data.Add(d);
                        }
                        else
                            d.count += item.count;

                        //// 장수 영혼석인데 보유하지 않았다면
                        //if (item.key == ItemType.dedicated_soul_stone && DataManager.userInfo.HasHero(d.value) == false)
                        //{
                        //    var grade = TableManager.hero.GetGradeFromSoulCount(d.count);
                        //    if (grade > GradeType.NONE)
                        //    {
                        //        DataManager.userInfo.AddHero(d.value, grade);
                        //        d.count -= TableManager.hero.GetNeedSoul(grade);
                        //    }
                        //}

                        d.isNew = true;
                        instance.SaveData();
                        break;
                }
            }
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class ItemData : TableItemData
{
    //custom 
    [JsonProperty] public bool isNew;
    [JsonProperty] public long count;

    public bool EqaulsItemData(ItemData _itemData)
    {
        if (key == _itemData.key &&
            value.IsActive() == _itemData.value.IsActive())
            return true;
        return false;
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class InventoryItemData : ItemData
{
}
