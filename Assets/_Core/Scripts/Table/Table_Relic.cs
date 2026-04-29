using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_Relic : BaseTable<string, TableRelicOriginData>
{
    List<TableRelicData> m_dbList = new();

    Dictionary<string, TableRelicData> m_dbRelic = new();
    public IReadOnlyList<TableRelicData> dbList => m_dbList;

    public Table_Relic(List<TableRelicOriginData> _table) : base(_table)
    {
        List<TableRelicOriginData> list = new()
            {
                new() {
                    key = "막야검",
                    statType = BattleStatType.attack_speed,
                    value = 10,
                },
                new() {
                    key = "막야검",
                    statType = BattleStatType.life_steal,
                    value = 5,
                },
                new() {
                    key = "적토마",
                    statType = BattleStatType.move_speed,
                    value = 10,
                },
                new() {
                    key = "손자병법서",
                    statType = BattleStatType.cooldown_rate,
                    value = 10,
                },
                new() {
                    key = "손자병법서",
                    statType = BattleStatType.life_steal,
                    value = 5,
                },
                new() {
                    key = "청낭서",
                    statType = BattleStatType.life_steal,
                    value = 10,
                }
            };

        m_dbRelic = list.GroupBy(x => x.key).ToDictionary(x => x.Key, x =>
        {
            TableRelicData data = new();
            data.key = x.Key;
            data.statData = x.Select(s => new BattleStatData()
            {
                statType = s.statType,
                value = s.value
            }).ToList();

            return data;
        });

        m_dbList = m_dbRelic.Values.ToList();
    }

    public TableRelicData GetGroupData(string _key)
        => m_dbRelic[_key];
}

public struct TableRelicOriginData
{
    public string key;
    public BattleStatType statType;
    public float value;
}

public struct TableRelicData
{
    public string key;
    public List<BattleStatData> statData;
}

public struct BattleStatData
{
    public BattleStatType statType;
    public float value;

    //string m_statName;

    public float percent => value * 0.01f;
    public string stringPercent => $"+{value.AmountKMBT()}%";
    public string statName
        => TableManager.stringTable.GetString("BATTLESTAT_" + statType.ToString().ToUpper());
    //{
    //    get
    //    {
    //        if (m_statName.IsActive() == false)
    //            m_statName = TableManager.stringTable.GetString("BATTLESTAT_" + statType.ToString().ToUpper());
    //        return m_statName;
    //    }
    //}
    public string stringPoint
    {
        get
        {
            switch (statType)
            {
                case BattleStatType.attack_power:
                case BattleStatType.defence:
                case BattleStatType.health_max:
                    return Mathf.RoundToInt(value).AmountKMBT();

                case BattleStatType.attack_speed:
                    return $"{value:0.0}/s";

                case BattleStatType.move_speed:
                    return $"+{value}";
                default:
                    return $"+{value}%";
            }
        }
    }
}