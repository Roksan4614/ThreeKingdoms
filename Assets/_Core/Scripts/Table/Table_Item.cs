using System.Collections.Generic;
using UnityEngine;

public class Table_Item : BaseTable<ItemType, TableItemData>
{
    public Table_Item(List<TableItemData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }
}

public enum ItemCategoryType
{
    Currency = 1,
    Soul_Stone,
    Bundle,
    Random_Box,
    Item,
    Piece
}

public enum ItemType
{
    NONE = 0,

    Gold,                               // 골드
    Rice,                               // 군량미
    Time_Stone,                         // 시간석
    Bundle_Normal,                      // 일반 보따리
    Bundle_Elite,                       // 고급 보따리
    Bundle_General,                     // 명장 보따리
    Bundle_Hero,                        // 명장 보따리
    Bundle_Legend,                      // 전설 보따리
    Public_Soul_Stone,                  // 공용 영혼석
    Class_Soul_Stone,                   // 클래스 영혼석
    Dedicated_Soul_Stone,               // 전용 영혼석
    Class_Soul_Stone_Random_Box,        // 클래스 영혼석 랜덤 상자
    Dedicated_Soul_Stone_Random_Box,    // 전용 영혼석 랜덤 상자
    Treasure,                           // 보물
    Treasure_Piece,                     // 보물 조각
    Treasure_Piece_Random_Box,          // 보물 조각 랜덤 상자
    Normal_Gatcha_Ticket,               // 일반 가챠 티켓
    Rare_Gatcha_Ticket,                 // 희귀 가챠 티켓

    MAX
}

public struct TableItemData
{
    public ItemType key;
    public string value;
    public ItemCategoryType category;

    //custom 
    public bool isActive => key > ItemType.NONE;
    public bool isNew;
    public long count;
}
