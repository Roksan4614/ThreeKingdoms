using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_CastleRise : BaseTable<string, TableCastleRiseData>
{
    Dictionary<CastleObjectType, List<TableCastleRiseData>> m_db = new();

    public IReadOnlyDictionary<CastleObjectType, List<TableCastleRiseData>> db => m_db;

    public Table_CastleRise(List<TableCastleRiseData> _data) : base(_data)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var d = list[i];
            d.Initialize();
            m_list[i] = d;
        }

        m_db = m_list.GroupBy(x => x.type)
            .ToDictionary(x => x.Key, x => x.OrderBy(x => x.level).ToList());
    }

    public IReadOnlyList<TableCastleRiseData> GetTable(CastleObjectType _obejctType)
    {
        if (m_db.ContainsKey(_obejctType))
            return m_db[_obejctType];

        return m_db[CastleObjectType.NONE];
    }

    public TableCastleRiseData GetRiseData(CastleObjectType _obejctType, int _nowLevel)
    {
        return GetTable(_obejctType).FirstOrDefault(x => x.level == _nowLevel);
    }
}

public struct TableCastleRiseData
{
    public string key;
    public int level;
    [JsonProperty] int req_stat_value_1;        // 요구치
    [JsonProperty] int req_stat_value_2;        // 요구치
    public int character_slot_max;              // 업그레이드 당 배치 수
    [JsonProperty] int upgrade_seconds;         // 업그레이드 소요 시간 (초)

    // CUSTOM
    public bool isActive => key.IsActive();
    public CastleObjectType type;
    public void Initialize()
    {
        type = System.Enum.Parse<CastleObjectType>(key);
    }

    public int[] maxCoreStat => new[] { value01, value02 };

    public int orinValue01 => req_stat_value_1;
    //public int value01 => Mathf.FloorToInt((1 - DataManager.castle.GetPalaceCharismaRate()) * req_stat_value_1) + req_stat_value_1;
    //public int value02 => Mathf.FloorToInt((1 - DataManager.castle.GetPalaceCharismaRate()) * req_stat_value_2) + req_stat_value_2;
    public int value01 => Mathf.RoundToInt(req_stat_value_1 * 2 * Mathf.Lerp(1, 0.5f, DataManager.castle.GetPalaceCharismaRate()));
    public int value02 => Mathf.RoundToInt(req_stat_value_2 * 2 * Mathf.Lerp(1, 0.5f, DataManager.castle.GetPalaceCharismaRate()));

    public int nowUpgradeSeconds => upgrade_seconds;
    public int upgradeSeconds => TableManager.castleRise.GetRiseData(type, level + 1).nowUpgradeSeconds;
}
