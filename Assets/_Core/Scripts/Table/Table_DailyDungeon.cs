using System;
using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class Table_DailyDungeon_Grade : BaseTable<GradeType, TableDailyDungeonGradeData>
{
	public Table_DailyDungeon_Grade(List<TableDailyDungeonGradeData> _table) : base(_table)
	{
		SetDictionary(x => x.key);
	}

	public List<ItemData> GetReward(string _key, GradeType _gradeType, float _percent)
	{
		List<ItemData> result = new();

		//보스 보상
		var grade = GradeType.None + 1;
		bool isGrade = false;
		while (grade <= _gradeType)
		{
			while (true)
			{
				var rewards = isGrade
					? TableManager.dailyDungeonRewardGrade.GetRewards(null, grade).DeepClone()
					: TableManager.dailyDungeonRewardClass.GetRewards(_key, grade).DeepClone();

				foreach (var r in rewards)
				{
					var idx = result.FindIndex(x => x.key == r.key);

					if (grade == _gradeType)
						r.count = Mathf.FloorToInt(r.count * _percent);

					if (r.count == 0)
						continue;

					if (idx == -1)
						result.Add(r);
					else
						result[idx].count += r.count;
				}

				if (isGrade == true)
					break;
				isGrade = true;
			}

			grade++;
		}


		//var rewards = TableManager.dailyDungeonRewardClass.GetRewards(_key, _gradeType).DeepClone();
		//foreach (var r in rewards)
		//	r.count += Mathf.FloorToInt(r.count * _percent);
		//result.AddRange(rewards);

		////등급 보상
		//rewards = TableManager.dailyDungeonRewardGrade.GetRewards(null, _gradeType).DeepClone();
		//foreach (var r in rewards)
		//	r.count += Mathf.FloorToInt(r.count * _percent);
		//result.AddRange(rewards);

		//var grade = GradeType.None + 1;
		//for (; grade <= _gradeType; grade++)
		//{
		//	var data = Get(grade);

		//	if (result == null)
		//		result = data.GetReward(_heroCalssType, true);
		//	else
		//	{
		//		var rewards = data.GetReward(_heroCalssType, true);
		//		for (int i = 0; i < rewards.Count; i++)
		//		{
		//			if (rewards[i].count > 0)
		//			{
		//				int idx = result.FindIndex(x => x.key == rewards[i].key);
		//				var d = result[idx];
		//				d.count += rewards[i].count;
		//				result[idx] = d;
		//			}
		//		}
		//	}
		//}

		//if (_percent > 0 && grade < GradeType.Max)
		//{
		//	var rewards = Get(grade).GetReward(_heroCalssType, true);

		//	for (int i = 0; i < rewards.Count; i++)
		//	{
		//		int idx = result.FindIndex(x => x.key == rewards[i].key);
		//		var d = result[idx];
		//		d.count += Mathf.FloorToInt(result[i].count * _percent);
		//		result[idx] = d;
		//	}
		//}

		//for (int i = 0; i < result.Count; i++)
		//{
		//	if (result[i].count == 0)
		//	{
		//		result.RemoveAt(i--);
		//		continue;
		//	}
		//}

		return result;
	}
}

public class TableDailyDungeonGradeData
{
	public GradeType key;
	public float hp_mul;
	public float atk_mul;
	public float def_mul;
	//public int soul_stone_count;
	//public int rice;
	//public int gold;
	//public int time_stone_count;


	//List<ItemData> m_rewards;
	//public List<ItemData> GetReward(HeroClassType _classType, bool _isWithCount)
	//{
	//    if (m_rewards == null)
	//    {
	//        m_rewards = new() {
	//            TableManager.item.GetItemData(Enum.Parse<ItemKey>($"SoulStoneClass{_classType}"), _isWithCount ? soul_stone_count : 0),
	//            TableManager.item.GetItemData(ItemKey.TimeStone, _isWithCount ? time_stone_count: 0),
	//            TableManager.item.GetItemData(ItemKey.GoldFree, _isWithCount ? gold: 0),
	//            TableManager.item.GetItemData(ItemKey.Rice, _isWithCount ? rice: 0),
	//        };
	//    }
	//    return m_rewards;
	//}
}

public class Table_DailyDungeon_Reward : BaseTable<string, TableDailyDungeonRewardData>
{
	public Table_DailyDungeon_Reward(List<TableDailyDungeonRewardData> _table) : base(_table)
	{
	}

	public List<ItemData> GetRewards(string _key, GradeType _grade)
	{
		List<ItemData> result = new();
		foreach (var d in m_list)
		{
			if (d.daily_dungeon_boss_key == _key && d.dungeon_boss_grade == _grade)
				result.Add(d.itemData);
		}
		return result;
	}
}
public class TableDailyDungeonRewardData
{
	public string daily_dungeon_boss_key;
	public GradeType dungeon_boss_grade;
	public ItemKey reward_item_key;
	public int reward_item_amount;

	ItemData m_itemData;
	public ItemData itemData => m_itemData ??= TableManager.item.GetItemData(reward_item_key, reward_item_amount);
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
	public string key;
	public WeekdayType weekday;
	public string boss_key;

	HeroClassType? m_classType = null;
	public HeroClassType classType => m_classType ??= TableManager.hero.Get(boss_key).classType;

	public string name => TableManager.stringHero.GetString($"HISTORICAL_NAME_{boss_key.ToUpper()}");
	public string desc => TableManager.stringHero.GetString($"HISTORICAL_NAME_{boss_key.ToUpper()}_DESC");
	public string className => TableManager.stringHero.GetClassType(classType);
}
