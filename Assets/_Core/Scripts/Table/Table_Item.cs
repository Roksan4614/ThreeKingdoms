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


    public string stringKey => "NAME_" + key.ToString().ToUpper();
    public string name => TableManager.stringItem.GetString(stringKey);
    public string nameValue => TableManager.stringItem.GetString(stringKey + (value.IsActive() == false ? "" : $"_{value.ToUpper()}"));
    public string iconKey => $"{key}{(value == null ? "" : $"_{value}")}";
}

