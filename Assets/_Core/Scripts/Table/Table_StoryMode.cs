using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class Table_StoryMode_Node : BaseTable<string, Table_StoryMode_Node.TableStoryModeNodeData>
{
    public List<List<List<TableStoryModeNodeData>>> group { get; private set; }

    public Table_StoryMode_Node(List<TableStoryModeNodeData> _table) : base(_table)
    {
        //group = m_list.Where(x => x.chapter_key > 0 && x.isActive == true)
        group = m_list.Where(x => x.chapter_key > 0)
            .GroupBy(x => x.year)
            .Select(x =>
                x.GroupBy(y => Regex.Replace(y.node_key, @"(?<=\d)[a-zA-Z]+$", ""))
                .Select(y => y.ToList())
                .ToList())
            .ToList();
    }

    public struct TableStoryModeNodeData
    {
        public string node_key;
        public RegionType region_type;
        public int year;
        public string character_key;
        public int chapter_key;
        public int stage_key;
        public int order_num;
        public string reward_character;
        public string next_node_key;

        [JsonProperty] bool? is_conditonal;
        [JsonProperty] bool? has_if_story;
        [JsonProperty] int? required_choice_seq;
        [JsonProperty] bool? is_active;

        public bool isActive => node_key.IsActive() && (is_active ?? false);
        public bool isConditional => is_conditonal ?? false;
        public bool hasIfStory => has_if_story ?? false;
        public int requiredChoiceSeq => required_choice_seq ?? -1;
    }
}

public class Table_StoryMode_Unlock : BaseTable<string, Table_StoryMode_Unlock.TableStoryModeUnlockData>
{
    public Table_StoryMode_Unlock(List<TableStoryModeUnlockData> _table) : base(_table) { }

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
