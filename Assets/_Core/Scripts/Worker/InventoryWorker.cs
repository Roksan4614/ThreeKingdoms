using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;
using static UnityEditor.Progress;

public class InventoryWorker : BaseWorker<InventoryWorker>
{
    List<InventoryItemData> m_data;
    public static List<InventoryItemData> data => instance.m_data;
    const string c_key = "pp_inventory";

    List<ItemType> m_sortCategory = new()
        {
            ItemType.None,
            ItemType.Currency,
            ItemType.TicketGacha,
            ItemType.SoulStone,
            ItemType.Max,
        };
    public IReadOnlyList<ItemType> sortCategory => m_sortCategory;

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

    void SaveData()
    {
        m_data.Sort((x, y) => SortCompare(x, y));
        m_data = m_data.SortBy(x => m_sortCategory.FindIndex(s => s == x.category));

        PPWorker.Set(c_key, m_data);
    }

    public long GetItemCount(ItemKey _key, string _value = null)
        => m_data.Find(x =>
        {
            if (x.key != _key)
                return false;

            if (x.value.IsActive() == false && _value.IsActive() == false)
                return true;

            return x.value == _value;
        })?.count ?? 0;

    public long GetItemCount(ItemData _itemData)
        => m_data.Find(x => x.key == _itemData.key && x.value == _itemData.value)?.count ?? 0;

    public bool UseItem(ItemKey _itemKey, int _count, bool _isUpdate = true, bool _isTween = true)
    {
        var d = data.Find(x => x.key == _itemKey);

        if (d.count < _count)
            return false;

        d.count -= _count;
        if (d.count < 0)
            d.count = 0;

        SaveData();

        Signal.instance.Inventory_UpdateCount.Emit(d);

        return true;
    }

    public void AddItem(ItemKey _itemKey, int _count, bool _isUpdate = true, bool _isTween = true, bool _isRewardAction = true, Vector3 _actionPosition = default)
    {
        AddItem(_isUpdate, _isTween, _isRewardAction, _actionPosition, TableManager.item.GetItemData(_itemKey, _count));
    }
    public void AddItem(bool _isUpdate = true, bool _isTween = true, bool _isRewardAction = true, Vector3 _actionPosition = default, params ItemData[] _itemData)
    {
        if (_isRewardAction)
            RewardWorker.instance.RunAsync(_actionPosition, _itemData: _itemData).Forget();
        else
        {
            foreach (var item in _itemData)
            {
                switch (item.key)
                {
                    case ItemKey.Rice:
                    case ItemKey.GoldFree:
                        DataManager.userInfo.AddAsset(item.key, item.count, _isUpdate, _isTween);
                        break;
                    default:
                        var d = data.Find(x =>
                        {
                            if (x.key != item.key)
                                return false;

                            if (x.value.IsActive() == false && item.value.IsActive() == false)
                                return true;

                            return x.value == item.value;
                        });

                        if (d == null)
                        {
                            d = new()
                            {
                                key = item.key,
                                value = item.value,
                                category = item.category,
                                type = item.type,
                                count = item.count
                            };

                            d.idx = (int)Utils.GetUTC().Ticks;
                            data.Add(d);
                        }
                        else
                            d.count += item.count;

                        d.isNew = true;
                        SaveData();

                        Signal.instance.Inventory_UpdateCount.Emit(d);
                        break;
                }

            }

        }
    }

    public int SortCompare(ItemData x, ItemData y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int result = 0;

        // ¿µÈ¥¼®
        if (x.type == ItemDetailType.SoulStoneDedicated && y.type == ItemDetailType.SoulStoneDedicated)
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
        // Å¬·¡½º¿µÈ¥
        else if (x.type == ItemDetailType.SoulStoneClass && y.type == ItemDetailType.SoulStoneClass)
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

}


[Serializable]
public class ItemData : TableItemData
{
    //custom 
    public bool isNew;
    public long count;

    public bool EqaulsItemData(ItemData _itemData)
    {
        if (key == _itemData.key &&
            value.IsActive() == _itemData.value.IsActive())
            return true;
        return false;
    }
}

[Serializable]
public class InventoryItemData : ItemData
{
    public int idx;
}
