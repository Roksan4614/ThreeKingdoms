using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class Table_Field : BaseTable<int, TableFieldData>
{
    Dictionary<int, Dictionary<int, List<TableFieldData>>> m_group = new();
    public Table_Field(List<TableFieldData> _table) : base(_table)
    {
        m_group = m_list.GroupBy(x => x.chapter).ToDictionary(x => x.Key,
            x => x.GroupBy(x => x.stage).ToDictionary(x => x.Key, x => x.ToList()));
    }
}

public class TableFieldData
{
    public int chapter;
    public int stage;
    public int phase;

    public RegionType region_type;
}