using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_DailyDungeon_Grade : BaseTable<GradeType, TableDailyDungeonGradeData>
{
    public Table_DailyDungeon_Grade(List<TableDailyDungeonGradeData> _table) : base(_table)
    {
        SetDictionary(x => x.dungeon_boss_grade);
    }

    public List<ItemData> GetReward(HeroClassType _heroCalssType, GradeType _gradeType, float _percent)
    {
        List<ItemData> result = null;

        var grade = GradeType.NONE + 1;
        for (; grade <= _gradeType; grade++)
        {
            var data = Get(grade);

            if (result == null)
                result = data.GetReward(_heroCalssType, true);
            else
            {
                var rewards = data.GetReward(_heroCalssType, true);
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i].count > 0)
                    {
                        int idx = result.FindIndex(x => x.key == rewards[i].key);
                        var d = result[idx];
                        d.count += rewards[i].count;
                        result[idx] = d;
                    }
                }
            }
        }

        if (_percent > 0 && grade < GradeType.MAX)
        {
            var rewards = Get(grade).GetReward(_heroCalssType, true);

            for (int i = 0; i < rewards.Count; i++)
            {
                int idx = result.FindIndex(x => x.key == rewards[i].key);
                var d = result[idx];
                d.count += Mathf.FloorToInt(result[i].count * _percent);
                result[idx] = d;
            }
        }

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].count == 0)
            {
                result.RemoveAt(i--);
                continue;
            }
        }

        return result;
    }
}

public class TableDailyDungeonGradeData
{
    public GradeType dungeon_boss_grade;
    public float hp_mul;
    public float atk_mul;
    public float def_mul;
    public int soul_stone_count;
    public int rice;
    public int gold;
    public int time_stone_count;


    List<ItemData> m_rewards;
    public List<ItemData> GetReward(HeroClassType _classType, bool _isWithCount)
    {
        if (m_rewards == null)
        {
            m_rewards = new() {
                new()
                {
                    category = ItemCategoryType.Soul_Stone,
                    key = ItemType.class_soul_stone,
                    value = _classType.ToString(),
                    count = _isWithCount ? soul_stone_count : 0
                },
                new()
                {
                    key = ItemType.time_stone,
                    count = _isWithCount ? time_stone_count : 0
                },
                new()
                {
                    key = ItemType.gold,
                    count = _isWithCount ? gold : 0
                },
                new()
                {
                    key = ItemType.rice,
                    count = _isWithCount ? rice : 0
                }
            };
        }
        return m_rewards;
    }
}


public class Table_DailyDungeon_Boss : BaseTable<WeekdayType, TableDailyDungeonBossData>
{
    public Table_DailyDungeon_Boss(List<TableDailyDungeonBossData> _table) : base(_table)
    {
        SetDictionary(x => x.weekday);
    }
}

public class TableDailyDungeonBossData
{
    public WeekdayType weekday;
    public HeroClassType dungeon_boss_class;
    public string monster_key;

    public string name => TableManager.stringHero.GetString($"HISTORICAL_NAME_{monster_key.ToUpper()}");
    public string desc => TableManager.stringHero.GetString($"HISTORICAL_NAME_{monster_key.ToUpper()}_DESC");
    public string className => TableManager.stringHero.GetString($"CLASSTYPE_{dungeon_boss_class.ToString().ToUpper()}");
}
