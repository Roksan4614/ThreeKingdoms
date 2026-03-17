using System.Collections.Generic;
using UnityEngine;

public class Table_Item : BaseTable<ItemType, TableItemData>
{
    public Table_Item(List<TableItemData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }
}


public enum ItemType
{
    NONE,

    Gold,           //골드
    Rice,           //군량미
    Scroll_Party,    //연회권
    Stone_Soul,     //영혼석
    Stone_Time,     //시간석

    MAX
}

public struct TableItemData
{
    public ItemType key;
    public string value;

    //custom 
    public long count;
}
