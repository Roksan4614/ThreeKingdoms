using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;
using static Rev9.Tournament.Table_TournamentReward;


namespace Rev9.Tournament
{
    public class Table_TournamentTier : BaseTable<string, TableTournamentTierData>
    {
        public Table_TournamentTier(List<TableTournamentTierData> _table) : base(_table) { }
    }

    public class TableTournamentTierData
    {
        public int idx;
        public string id;
        public int score_min;
        public int rank_limit;
        public int soft_reset_score;

        ItemData[] m_rewards;
        public ItemData[] rewards => m_rewards ??= TableManager.tournamentReward.GetRewards(id);

        public string tierName => idx <= 3
            ? TableManager.stringTable.GetString($"UI_TIER_RANK_{idx}")
            : TableManager.stringTable.GetStringFormat("UI_TIER_RANK", (idx - 3).ToString());
        public string desc => TableManager.stringTable.GetStringFormat("UI_TOUR_REWARD_INFO", rank_limit.ToString(), $"{score_min:#,0}");
    }

    public class Table_TournamentReward : BaseTable<int, TableTournamentRewardData>
    {
        Dictionary<string, List<TableTournamentRewardData>> m_group = new();

        public Table_TournamentReward(List<TableTournamentRewardData> _table) : base(_table)
        {
            m_group = m_list.GroupBy(x => x.id).ToDictionary(x => x.Key, x => x.ToList());
        }

        public ItemData[] GetRewards(string _id)
            => m_group[_id].Select(x => x.itemData).ToArray();

    }

    public class TableTournamentRewardData
    {
        public string id;

        public ItemKey reward_item_key;
        public int reward_count;

        ItemData m_itemData;
        public ItemData itemData => m_itemData ??= TableManager.item.GetItemData(reward_item_key, reward_count);
    }
}