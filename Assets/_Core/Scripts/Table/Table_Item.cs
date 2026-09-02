using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class Table_Item : BaseTable<ItemType, TableItemData>
{
    public Table_Item(List<TableItemData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public ItemData GetItemData(ItemType _itemType, int _count = 0)
    {
        var data = Get(_itemType);

        if (data.IsActive() == false)
            return null;

        ItemData result = new() {
            key = data.key,
            value = data.value,
            category = data.category,
            count = _count
        };
        return result;
    }
}

public enum ItemCategoryType
{
    Currency = 1,
    Soul_Stone,
    Bundle,
    Random_Box,
    Item,
    Piece,
    Point,
    Ticket,
}

public enum ItemType
{
    NONE = 0,

    gold,                               // 골드
    rice,                               // 군량미
    time_stone,                         // 시간석
    bundle_normal,                      // 일반 보따리
    bundle_elite,                       // 고급 보따리
    bundle_general,                     // 명장 보따리
    bundle_hero,                        // 명장 보따리
    bundle_legend,                      // 전설 보따리
    public_soul_stone,                  // 공용 영혼석
    class_soul_stone,                   // 클래스 영혼석
    dedicated_soul_stone,               // 전용 영혼석
    class_soul_stone_random_box,        // 클래스 영혼석 랜덤 상자
    dedicated_soul_stone_random_box,    // 전용 영혼석 랜덤 상자
    treasure,                           // 보물
    treasure_piece,                     // 보물 조각
    treasure_piece_random_box,          // 보물 조각 랜덤 상자
    normal_gatcha_ticket,               // 일반 가챠 티켓
    rare_gatcha_ticket,                 // 희귀 가챠 티켓
    tournament_point,
    tournament_ticket,
    raid_point,

    MAX
}

[JsonObject(MemberSerialization.OptIn)]
public class TableItemData
{
    [JsonProperty] public ItemType key;
    [JsonProperty] public string value;
    [JsonProperty] public ItemCategoryType category;

    public string stringKey => "NAME_" + key.ToString().ToUpper();
    public string name => TableManager.stringItem.GetString(stringKey);
    public string nameValue => TableManager.stringItem.GetString(stringKey + (value.IsActive() == false ? "" :$"_{value.ToUpper()}"));
    public string iconKey => $"{key}{(value == null ? "" : $"_{value}")}";
}

