using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Table_GuideQuest : BaseTable<string, Table_GuideQuest.TableGuideQuestData>
{
    public Table_GuideQuest(List<TableGuideQuestData> _table) : base(_table)
    {
        //SetDictionary(x => x.key);
    }

    public TableGuideQuestData GetGuideData(GuideQuestType _guideType)
        => m_list.Find(x => x.key.ToUpper() == _guideType.ToString().ToUpper());

    public TableGuideQuestData GetOpenStageData(int _chater, int _stage)
        => m_list.Find(x => x.startStage[0] == _chater && x.startStage[1] == _stage);

    public class TableGuideQuestData
    {
        public string key;
        [JsonProperty] int? target_value;

        string navigation;
        NavigationType[] m_navigation;

        string start_stage;
        int[] m_startStage;

        public ItemType reward_item;
        public int reward_count;

        public string open_guide_quest;

        //custom
        public int targetValue => target_value ?? 1;
        public int[] startStage
        {
            get
            {
                if (m_startStage == null)
                {
                    if (start_stage.IsActive() == true)
                    {
                        var parts = start_stage.Split('-');
                        m_startStage = new int[parts.Length];
                        for (int i = 0; i < parts.Length; i++)
                            m_startStage[i] = int.Parse(parts[i]);
                    }
                    else
                        m_startStage = new int[0];
                }
                return m_startStage;
            }
        }
        public NavigationType[] navi
        {
            get
            {
                if (m_navigation == null)
                {
                    if (navigation.IsActive() == true)
                    {
                        var parts = navigation.Replace(" ", "").Split(',');
                        m_navigation = new NavigationType[parts.Length];
                        for (int i = 0; i < parts.Length; i++)
                            Enum.TryParse(parts[i], out m_navigation[i]);
                    }
                    else
                        m_navigation = new NavigationType[0];
                }
                return m_navigation;
            }
        }
    }
}

public class Table_GuideQuest_Repeat : BaseTable<string, Table_GuideQuest.TableGuideQuestData>
{
    public Table_GuideQuest_Repeat(List<Table_GuideQuest.TableGuideQuestData> _table) : base(_table)
    {
        //SetDictionary(x => x.key);
    }

    public Table_GuideQuest.TableGuideQuestData GetRepeatData(GuideQuestRepeatType _guideType)
        => m_list.Find(x => x.key.ToUpper() == _guideType.ToString());
}