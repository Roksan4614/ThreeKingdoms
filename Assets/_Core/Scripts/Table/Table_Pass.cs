using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

namespace Rev9.Pass
{

    public class Table_PassReward : BaseTable<int, TablePassRewardData>
    {
        public Table_PassReward(List<TablePassRewardData> _table) : base(_table) { }

        public TablePassRewardData GetRewardData(int _level)
            => m_list.Find(x => x.level == _level);

        public ItemData GetRewardItem(int _level, bool _isPaid)
        {
            var data = GetRewardData(_level);
            return _isPaid ? data.itemDataPaid : data.itemData;
        }
    }

    public class TablePassRewardData
    {
        public int level;
        public string reward_key;
        public int reward_count;
        public string paid_reward_key;
        public int paid_reward_count;
        public int exp;

        ItemData m_itemData;
        public ItemData itemData
            => m_itemData ??= TableManager.item.GetItemData(reward_key, reward_count);

        ItemData m_itemDataPaid;
        public ItemData itemDataPaid
            => m_itemDataPaid ??= TableManager.item.GetItemData(paid_reward_key, paid_reward_count);
    }

    public class Table_PassQuest : BaseTable<int, TablePassQuestData>
    {
        public Table_PassQuest(List<TablePassQuestData> _table) : base(_table) { }

        public int GetCount(TablePassQuestData _data)
            => m_list.Find(x => x.key == _data.key && x.type == _data.type)?.count ?? 0;
        public TablePassQuestData GetRandomQuest(bool _isSeason)
        {
            TablePassQuestData result = new();

            var type = _isSeason ? QuestDateType.Season : QuestDateType.Daily;
            var questData = m_list.FindAll(x => x.type == type).RandomFirst();

            result.key = questData.key;
            result.type = questData.type;
            result.exp = questData.exp;
            result.is_character = 1;
            result.value = "GuanYu";

            return result;
        }
    }

    public class TablePassQuestData
    {
        public QuestType key;
        public QuestDateType type;
        public int count;
        public int exp;
        public int is_character;
        public int is_country;
        public int is_class;

        //custom
        public string value { get; set; }

        public string name => TableManager.questString.GetString($"{Utils.ToSnakeCase(key.ToString()).ToUpper()}_NAME");

        public string GetDesc(string _value)
            => TableManager.questString.GetStringFormat($"{Utils.ToSnakeCase(key.ToString()).ToUpper()}_DESC", _value);
    }
}