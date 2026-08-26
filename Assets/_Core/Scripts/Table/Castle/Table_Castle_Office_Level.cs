using System.Collections.Generic;
using UnityEngine;

public class Table_Castle_Office_Level : BaseTable<int, TableCastleOfficeLevelData>
{
    public Table_Castle_Office_Level(List<TableCastleOfficeLevelData> _data) : base(_data)
    {
        SetDictionary(x => x.level);
    }

    public TableCastleOfficeLevelData GetLevelInfo(int _level)
        => Get(_level);
}

public class TableCastleOfficeLevelData
{
    public int level;
    public int req_xp;
    public int accum_xp;
    public int upgrade_seconds;

}