using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Rev9.Tournament
{
    public class Table_TournamentReward : BaseTable<int, TableTournamentRewardData>
    {
        public Table_TournamentReward(List<TableTournamentRewardData> _table) : base(_table)
        {
            for (int i = 0; i < 10; i++)
            {
                m_list.Add(new()
                {
                    minRank = i < 3 ? i + 1 : i * 30,
                    minPoint = (10 - i) * 1000,
                    index = i + 1,
                    reward_key = "Gold,Rice,Time_Stone" + (i < 3 ? ",Public_Soul_Stone" : ""),
                    reward_count = $"{10 * (10 - i)},{(int)(10 * (10 - i) * .5)}, {(10 - i)}" + (i < 3 ? $",{3 - i}" : "")
                });
            }
        }
    }

    public class TableTournamentRewardData
    {
        public int index;

        public int minRank;
        public int minPoint;

        public string reward_key;
        public string reward_count;

        List<ItemData> m_rewards;
        public List<ItemData> rewards
        {
            get
            {
                if (m_rewards == null)
                {
                    var key = reward_key.Replace(" ", "").Split(",").Select(x => System.Enum.Parse<ItemType>(x)).ToArray();
                    var count = reward_count.Replace(" ", "").Split(",").Select(x => int.Parse(x)).ToArray();

                    m_rewards = new();
                    for (int i = 0; i < key.Length; i++)
                        m_rewards.Add(TableManager.item.GetItemData(key[i], count[i]));
                }

                return m_rewards;
            }
        }

        public string tierName => index <= 3 ? $"{index}등" : $"{index - 3}티어";
        public string desc => $"랭킹 {minRank}이내\n점수 {minPoint:#,0}이상";
    }
}