using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_Treasure : BaseTable<string, TableTreasureData>
{
    public Table_String stringTable { get; private set; }

    public Table_Treasure(List<TableTreasureData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public void InitializeStringTable(Table_String _stringTable)
        => stringTable = _stringTable;
}

public struct TableTreasureData
{
    public string key;
    public BattleStatType stat;
    public string effect;
    public int pieces_required;

    Dictionary<BattleStatType, BattleStatData> m_dbEffect;

    //custom
    public bool isActive => key.IsActive() && effect.IsActive();
    public string name => TableManager.treasure.stringTable.GetString($"NAME_{key.ToUpper()}");

    public IReadOnlyDictionary<BattleStatType, BattleStatData> dbEffect
    {
        get
        {
            if (m_dbEffect == null)
            {
                m_dbEffect = new();
                var jobject = JObject.Parse(effect);

                for (var stat = BattleStatType.NONE + 1; stat < BattleStatType.MAX; stat++)
                {
                    string key = stat.ToString();
                    if (jobject.ContainsKey(key))
                    {
                        m_dbEffect.Add(stat, new()
                        {
                            statType = stat,
                            value = float.Parse(jobject[key].ToString())
                        });
                    }
                }
            }

            return m_dbEffect;
        }
    }
}

public struct BattleStatData
{
    public BattleStatType statType;
    public float value;

    //string m_statName;

    public float percent => value * 0.01f;
    public string stringPercent => $"+{value.AmountKMBT()}%";
    public string statName
        => TableManager.stringTable.GetBattleStat(statType);
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
                    return $"+{value.AmountKMBT()}%";
            }
        }
    }
}