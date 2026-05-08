using System;
using System.Collections.Generic;
using System.Linq;
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

public struct TableCastleData
{
    public string key;

    public string stat_01;
    public string stat_02;

    // CUSTOM
    public bool isActive => key.IsActive();

    public void Initialize()
    {
        m_coreStat = new[] {
                    stat_01.IsActive() ? Enum.Parse<CoreStatType>(stat_01) : CoreStatType.NONE,
                    stat_02.IsActive() ? Enum.Parse<CoreStatType>(stat_02) : CoreStatType.NONE
                };

        m_objectType = Enum.Parse<CastleObjectType>(key);
    }

    CastleObjectType m_objectType;
    public CastleObjectType objectType => m_objectType;

    CoreStatType[] m_coreStat;
    public CoreStatType[] coreStat => m_coreStat;
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