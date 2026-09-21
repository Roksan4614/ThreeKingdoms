using Newtonsoft.Json;
using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class Table_Item : BaseTable<string, TableItemData>
{
    public Table_Item(List<TableItemData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public ItemData GetItemData(string _key, int _count = 0, string _value = null)
    {
        var data = Get(_key);

        if (data.IsActive() == false)
            return null;

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


[JsonObject(MemberSerialization.OptIn)]
public class TableItemData
{
    [JsonProperty] public string key;
    [JsonProperty] public ItemDetailType type;
    [JsonProperty] public string value;
    [JsonProperty] public ItemType category;

    public string name
        => TableManager.stringItem.GetItemName(this);
}

public class Table_String_Item : Table_String_Base
{
    public Table_String_Item(List<TableStringData> _table) : base(_table)
    {
        //SetDictionary(x => x.key);
    }

    public string GetItemName(TableItemData _itemData)
    {
        string key = "";
        if (_itemData.type == ItemDetailType.ClassSoulStone)
        {
            return TableManager.stringItem.GetStringFormat("NAME_SOUL_STONE", TableManager.stringHero.GetString(_itemData.value));
        }
        else
            key = $"NAME_{key.ToUpper()}";
        return TableManager.stringItem.GetString(key);
    }

    public string GetItemName(string _key)
        => GetString($"NAME_{_key.ToUpper()}");
}
