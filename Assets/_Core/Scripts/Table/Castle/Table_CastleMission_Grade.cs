using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class Table_CastleMission_Grade : BaseTable<GradeType, TableCastleMissionGradeData>
{
    public Table_CastleMission_Grade(List<TableCastleMissionGradeData> _data) : base(_data)
    {
        SetDictionary(x => x.key);
    }
}

public class TableCastleMissionGradeData
{
    public GradeType key;

    [JsonProperty] int duration_seconds;
    [JsonProperty] int mission_xp;
    [JsonProperty] int req_stat_value;

    public int durationSeconds => duration_seconds;
    public int missionXp => mission_xp;
    public int reqStatValue => req_stat_value;
}