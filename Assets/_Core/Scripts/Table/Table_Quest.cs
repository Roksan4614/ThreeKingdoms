using System.Collections.Generic;
using System.Linq;
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

public enum QuestType
{
    NONE = -1,

    login,                        // 로그인
    enemy_kill,                   // 적 처치하기
    tournament_play,              // 토너먼트 참여하기
    raid_play,                    // 레이드 참여하기
    gacha_proceed,                // 연회 진행하기
    rice_claim,                   // 군량 수확하기
    gold_claim,                   // 금화 수확하기
    office_dispatch,              // 관아 파견하기
    daily_dungeon_play,           // 요일 던전 플레이하기
    stage_boss_kill,              // 스테이지 보스 처치하기
    ads_watch,                    // 광고 시청하기
    item_buy,                     // 상품 구매하기
    item_use,                     // 아이템 사용하기

    MAX
}

public class TableQuestData
{
    public QuestType key;
    public QuestCategoryType type;
    public int target_value;
    public string reward_item_key;
    public int reward_count;

    ItemData m_itemData;
    public ItemData itemData => m_itemData ??= TableManager.item.GetItemData(reward_item_key, reward_count);
}
