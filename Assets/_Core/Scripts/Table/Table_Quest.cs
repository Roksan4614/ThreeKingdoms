using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class Table_Quest : BaseTable<int, TableQuestData>
{
    Dictionary<QuestCategoryType, List<TableQuestData>> m_group;
    public Table_Quest(List<TableQuestData> _table) : base(_table)
    {
        m_group = _table.GroupBy(x => x.type).ToDictionary(x => x.Key, x => x.ToList());
    }

    public List<TableQuestData> GetQuestList(QuestCategoryType _category)
        => m_group[_category];

    public TableQuestData GetQuestData(QuestCategoryType _category, QuestType _type)
        => m_group[_category].Find(x => x.key == _type);
}

public class Table_QuestReward : BaseTable<int, TableQuestData>
{
    Dictionary<QuestCategoryType, List<TableQuestData>> m_group;
    public Table_QuestReward(List<TableQuestData> _table) : base(_table)
    {
        m_group = _table.GroupBy(x => x.type).ToDictionary(x => x.Key, x => x.ToList());
    }

    public List<TableQuestData> GetQuestRewardData(QuestCategoryType _category)
        => m_group[_category];

    public ItemData[] GetRewards(QuestCategoryType _category)
    {
        var db = m_group[_category];
        ItemData[] rewards = new ItemData[db.Count];

        for (int i = 0; i < db.Count; i++)
        {
            var data = db[i];
            rewards[i] = TableManager.item.GetItemData(data.reward_item_key, data.reward_count);
        }

        return rewards;
    }

    public TableQuestData GetQuestRewardData(QuestCategoryType _category, int _target)
        => m_group.ContainsKey(_category) ? m_group[_category].Find(x => x.target_value == _target) : null;

    public TableQuestData GetQuestRewardData_Index(QuestCategoryType _category, int _index)
        => m_group.ContainsKey(_category) == false ? null : m_group[_category].Count <= _index ? null : m_group[_category][_index];
}

public enum QuestCategoryType
{
    NONE = -1,

    daily,
    weekly,

    MAX
}

public class TableQuestData
{
    public QuestType key;
    public QuestCategoryType type;
    public int target_value;
    public ItemKey reward_item_key;
    public int reward_count;

    ItemData m_itemData;
    public ItemData itemData => m_itemData ??= TableManager.item.GetItemData(reward_item_key, reward_count);
}
