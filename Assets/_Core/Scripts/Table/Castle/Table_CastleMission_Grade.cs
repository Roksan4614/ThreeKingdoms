using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;

public class Table_CastleMission_Grade : BaseTable<GradeType, TableCastleMissionGradeData>
{
    public Table_CastleMission_Grade(List<TableCastleMissionGradeData> _data) : base(_data)
    {
        SetDictionary(x => x.mission_grade);
    }
}

public class TableCastleMissionGradeData
{
    public GradeType mission_grade;

    int duration_seconds;
    int mission_xp;
    int req_stat_value;

    public int durationSeconds => duration_seconds;
    public int missionXp => mission_xp;
    public int reqStatValue => req_stat_value;
}