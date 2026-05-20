using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class Table_CastleMission : BaseTable<string, TableCastleMissionData>
{
    public Table_CastleMission(List<TableCastleMissionData> _data) : base(_data)
    {
        SetDictionary(x => x.key);
    }
}

public struct TableCastleMissionData
{
    public string key;
    [JsonProperty] CoreStatType req_stat_type;
    [JsonProperty] string relic_key;

    public bool isActive => key.IsActive();
    public CoreStatType statType => req_stat_type;
    public string keyRelic => relic_key;
}

public enum test
{
    none = -1,

    leadership,
    strength,
    intellect,
    politics,
    charisma,

    MAX
}