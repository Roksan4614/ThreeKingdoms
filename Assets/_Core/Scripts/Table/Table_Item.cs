using Newtonsoft.Json;
using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class Table_Item : BaseTable<ItemKey, TableItemData>
{
    public Table_Item(List<TableItemData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public ItemData GetItemData(ItemKey _key, int _count = 0, string _value = null)
    {
        var data = Get(_key);

        if (data.IsActive() == false)
        {
            IngameLog.Add($"GetItemData: FAILED: {_key}/{_value}");
            return null;
        }

        ItemData result = new()
        {
            key = data.key,
            value = _value ?? data.value,
            category = data.category,
            type = data.type,
            count = _count
        };
        return result;
    }
}


public class TableItemData
{
    public ItemKey key;
    public ItemDetailType type;
    public string value;
    public ItemType category;

    public string name
        => TableManager.stringItem.GetItemName(this, true);

    public string desc
        => TableManager.stringItem.GetItemName(this, false);
}

public class Table_String_Item : Table_String_Base
{
    public Table_String_Item(List<TableStringData> _table) : base(_table)
    {
        //SetDictionary(x => x.key);
    }

    public string GetItemName(TableItemData _itemData, bool _isName = true)
    {
        string key = "";
        switch (_itemData.type)
        {
            case ItemDetailType.SoulStoneClass:
                {
                    string lower = _itemData.value.ToLower();
                    for (var i = HeroClassType.NONE + 1; i < HeroClassType.MAX; i++)
                    {
                        if (i.ToString().ToLower() == lower)
                            return TableManager.stringItem.GetStringFormat($"{(_isName ? "NAME" : "DESC")}_SOUL_STONE"
                                , TableManager.stringHero.GetClassType(i));
                    }
                }
                break;
            case ItemDetailType.SoulStoneDedicated:
                {
                    return TableManager.stringItem.GetStringFormat($"{(_isName ? "NAME" : "DESC")}_SOUL_STONE"
                        , TableManager.stringHero.GetName(_itemData.value));
                }
            case ItemDetailType.Gold:
                key = $"{(_isName ? "NAME" : "DESC")}_{_itemData.key.ToString().ToUpper()}";
                break;
            default:
                key = $"{(_isName ? "NAME" : "DESC")}_{_itemData.type.ToString().ToUpper()}";
                break;
        }

        return key.IsActive() ? TableManager.stringItem.GetString(key) : "";
    }
}
