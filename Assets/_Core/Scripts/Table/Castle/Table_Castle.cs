using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class Table_Castle : BaseTable<string, TableCastleData>
{
    Dictionary<CastleObjectType, TableCastleData> m_db = new();
    public IReadOnlyDictionary<CastleObjectType, TableCastleData> db => m_db;

    public Table_Castle(List<TableCastleData> _table) : base(_table)
    {
        for (var i = 0; i < m_list.Count; i++)
        {
            var d = m_list[i];
            d.Initialize();
            m_list[i] = d;
        }

        m_db = m_list.ToDictionary(x => x.objectType, x => x);
    }

    public TableCastleData GetCastleData(CastleObjectType _objectType)
        => m_db.ContainsKey(_objectType) ? m_db[_objectType] : default;
}

public class TableCastleData
{
    public string key;

    [JsonProperty] string stat_type_1;
    [JsonProperty] string stat_type_2;

    // CUSTOM
    public void Initialize()
    {
        m_coreStat = new[] {
                    stat_type_1.IsActive() ? Enum.Parse<StatType>(stat_type_1) : StatType.None,
                    stat_type_2.IsActive() ? Enum.Parse<StatType>(stat_type_2) : StatType.None
                };

        m_objectType = Enum.Parse<CastleObjectType>(key);
    }

    CastleObjectType m_objectType;
    public CastleObjectType objectType => m_objectType;

    StatType[] m_coreStat;
    public StatType[] coreStat => m_coreStat;
}

public enum CastleObjectType
{
    NONE = -1,

    Palace,
    Market,
    Farm,
    Office,
    Merchant,
    Gate,
    //Wall,
    MAX
}