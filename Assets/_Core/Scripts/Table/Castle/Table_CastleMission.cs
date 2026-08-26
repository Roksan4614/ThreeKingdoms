using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class Table_CastleMission : BaseTable<string, TableCastleMissionData>
{
    public Table_CastleMission(List<TableCastleMissionData> _data) : base(_data)
    {
        SetDictionary(x => x.key);
    }

    public TableCastleMissionData GetNewMission(params string[] _keys)
    {
        List<string> lstKey = new();
        foreach (var k in _keys)
            lstKey.Add(k);

        var result = m_list.FindAll(x => lstKey.Contains(x.key) == false).RandomFirst();

        return result.DeepClone();
    }
}

public class TableCastleMissionData
{
    public string key;
    [JsonProperty] CoreStatType req_stat_type;
    [JsonProperty] string relic_key;

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