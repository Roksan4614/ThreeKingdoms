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
        List<TableCastleData> list = new();

        list.Add(new()
        {
            key = CastleObjectType.Palace.ToString(),
            stat_00 = CoreStatType.Charisma.ToString()
        });

        list.Add(new()
        {
            key = CastleObjectType.Market.ToString(),
            stat_00 = CoreStatType.Intellect.ToString(),
            stat_01 = CoreStatType.Politics.ToString()
        });

        list.Add(new()
        {
            key = CastleObjectType.Farm.ToString(),
            stat_00 = CoreStatType.Leadership.ToString(),
            stat_01 = CoreStatType.Politics.ToString()
        });

        list.Add(new()
        {
            key = CastleObjectType.Office.ToString(),
        });

        list.Add(new()
        {
            key = CastleObjectType.Merchant.ToString(),
            stat_00 = CoreStatType.Leadership.ToString(),
            stat_01 = CoreStatType.Intellect.ToString()
        });

        list.Add(new()
        {
            key = CastleObjectType.Gate.ToString(),
            stat_00 = CoreStatType.Leadership.ToString(),
            stat_01 = CoreStatType.Strength.ToString()
        });

        for (var i = 0; i < list.Count; i++)
        {
            var d = list[i];
            d.Initialize();
            list[i] = d;
        }

        m_db = list.ToDictionary(x => x.objectType, x => x);
    }
}

public struct TableCastleData
{
    public string key;

    public string stat_00;
    public string stat_01;

    public void Initialize()
    {
        m_coreStat = new[] {
                    stat_00.IsActive() ? Enum.Parse<CoreStatType>(stat_00) : CoreStatType.NONE,
                    stat_01.IsActive() ? Enum.Parse<CoreStatType>(stat_01) : CoreStatType.NONE
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