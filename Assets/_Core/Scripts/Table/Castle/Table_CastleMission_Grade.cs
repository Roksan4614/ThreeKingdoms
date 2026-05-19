using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class Table_CastleMission_Grade : BaseTable<string, TableCastleMissionGradeData>
{
    public Table_CastleMission_Grade(List<TableCastleMissionGradeData> _data) : base(_data)
    {
        m_list.Clear();
        m_list.Add(new("normal", 3600, 10, 100));
        m_list.Add(new("general", 21600, 60, 300));
        m_list.Add(new("legend", 86400, 240, 500));

        SetDictionary(x => x.key);
    }
}

public struct TableCastleMissionGradeData
{
    public string key;

    [JsonProperty] int duration_seconds;
    [JsonProperty] int mission_xp;
    [JsonProperty] int req_stat_value;

    public bool isActive => key.IsActive();
    public int durationSeconds => duration_seconds;
    public int missionXp => mission_xp;
    public int reqStatValue => req_stat_value;

    public TableCastleMissionGradeData(string _key, int _durationSeconds, int _missionXp, int _reqStatValue)
    {
        key = _key;
        duration_seconds = _durationSeconds;
        mission_xp = _missionXp;
        req_stat_value = _reqStatValue;
    }
}