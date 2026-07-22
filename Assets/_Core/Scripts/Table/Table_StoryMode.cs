using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class Table_StoryMode_Node : BaseTable<string, Table_StoryMode_Node.TableStoryModeNodeData>
{
    public List<List<List<TableStoryModeNodeData>>> group { get; private set; }

    public Table_StoryMode_Node(List<TableStoryModeNodeData> _table) : base(_table)
    {
        m_list = m_list.Where(x => x.isActive == true).ToList();
    }

    public void SortInitialize()
    {
        var regionType = DataManager.userInfo.region;

        for (int i = 0; i < m_list.Count; i++)
        {
            var data = m_list[i];

            if (data.region_type == regionType)
            {
                if (i > 0)
                {
                    data.order_num = 1;
                    m_list.RemoveAt(i);
                    m_list.Insert(0, data);
                }
                break;
            }
            else
            {
                data.order_num++;
                m_list[i] = data;
            }
        }

        group = m_list.Where(x => x.chapter_key > 0)
            //group = m_list.Where(x => x.chapter_key > 0)
            .GroupBy(x => x.year)
            .Select(x =>
                x.GroupBy(y => y.order_num) //Regex.Replace(y.node_key, @"(?<=\d)[a-zA-Z]+$", ""))
                .Select(y => y.ToList())
                .ToList())
            .ToList();
    }

    public TableStoryModeNodeData GetNode(string _nodeKey)
        => m_list.Find(x => x.node_key == _nodeKey);
    public List<TableStoryModeNodeData> GetNode_OrderNum(int _orderNum)
        => m_list.FindAll(x => x.order_num == _orderNum);

    public List<TableStoryModeNodeData> GetNode_Next(TableStoryModeNodeData _prevData)
    {
        var idx = m_list.FindIndex(x => x.order_num == _prevData.order_num) + 1;

        for (; idx < m_list.Count; idx++)
        {
            if (m_list[idx].order_num > _prevData.order_num && m_list[idx].chapter_key > 0)
                return GetNode_OrderNum(m_list[idx].order_num);
        }

        return default;
    }

    public struct TableStoryModeNodeData
    {
        public string node_key;
        public RegionType region_type;
        public int year;
        public int chapter_key;
        public int stage_key;
        public int order_num;
        public string reward_character;
        public string next_node_key;
        public string reward_currency_type;

        [JsonProperty] int? reward_currency_amount;
        [JsonProperty] bool? is_conditonal;
        [JsonProperty] int? has_if_story;
        [JsonProperty] int? required_choice_seq;
        [JsonProperty] bool? is_active;

        public bool isActive => node_key.IsActive() && (is_active ?? false);
        public bool isConditional => is_conditonal ?? false;
        public int hasIfStory => has_if_story ?? -100;
        public int requiredChoiceSeq => required_choice_seq ?? -1;
        public int rewardCurrencyAmount => reward_currency_amount ?? 0;

        public string name => TableManager.storyString.GetString($"{node_key.ToUpper()}_TITLE");
        public string desc => TableManager.storyString.GetString($"{node_key.ToUpper()}_DESC");
    }
}

public class Table_StoryMode_Unlock : BaseTable<string, Table_StoryMode_Unlock.TableStoryModeUnlockData>
{
    Dictionary<string, List<TableStoryModeUnlockData>> m_group;

    public Table_StoryMode_Unlock(List<TableStoryModeUnlockData> _table) : base(_table)
    {
        m_group = _table.GroupBy(x => x.node_key).ToDictionary(x => x.Key, x => x.ToList());
    }

    public TableStoryModeUnlockData[] GetSourceNodeKey(string _nodeKey)
        => m_group.ContainsKey(_nodeKey) ? m_group[_nodeKey].ToArray() : Array.Empty<TableStoryModeUnlockData>();

    public struct TableStoryModeUnlockData
    {
        public string node_key;
        public string source_node_key;
        public int required_choice_seq;

        public bool isActive => node_key.IsActive();
    }
}

public class Table_StoryMode_Choice : BaseTable<string, Table_StoryMode_Choice.TableStoryModeChoiceData>
{
    public Table_StoryMode_Choice(List<TableStoryModeChoiceData> _table) : base(_table) { }

    public struct TableStoryModeChoiceData
    {
        public string node_key;
        public int choice_seq;

        public bool isActive => node_key.IsActive();
    }
}
