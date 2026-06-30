using System.Collections.Generic;
using UnityEngine;

public class Table_DailyDungeon_Grade : BaseTable<GradeType, TableDailyDungeonGradeData>
{
    public Table_DailyDungeon_Grade(List<TableDailyDungeonGradeData> _table) : base(_table)
    {
        SetDictionary(x => x.dungeon_boss_grade);
    }
}

public struct TableDailyDungeonGradeData
{
    public GradeType dungeon_boss_grade;
    public float hp_mul;
    public float atk_mul;
    public float def_mul;
    public int soul_stone_count;
    public int rice;
    public int gold;
    public int time_stone_count;
}


public class Table_DailyDungeon_Boss : BaseTable<WeekdayType, TableDailyDungeonBossData>
{
    public Table_DailyDungeon_Boss(List<TableDailyDungeonBossData> _table) : base(_table)
    {
        SetDictionary(x => x.weekday);
    }
}

public struct TableDailyDungeonBossData
{
    public WeekdayType weekday;
    public HeroClassType dungeon_boss_class;
    public string monster_key;

    public string name => TableManager.stringHero.GetString($"NAME_HISTORICAL_{monster_key.ToUpper()}");
    public string desc => TableManager.stringHero.GetString($"NAME_HISTORICAL_{monster_key.ToUpper()}_DESC");
    public string className => TableManager.stringHero.GetString($"CLASSTYPE_{dungeon_boss_class.ToString().ToUpper()}");
}
