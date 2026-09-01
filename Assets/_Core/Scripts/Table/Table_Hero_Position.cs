using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Table_Hero_Position : BaseTable<HeroPositionType, TableHeroPositionData>
{
    Dictionary<CategoryType_HeroPositon, List<TableHeroPositionData>> m_group;

    public Table_Hero_Position(List<TableHeroPositionData> _table) : base(_table)
    {
        m_group = _table.GroupBy(x => x.category).ToDictionary(x => x.Key, x => x.ToList());
    }

    public List<TableHeroPositionData> GetPositionds(CategoryType_HeroPositon _category)
        => m_group.ContainsKey(_category) ? m_group[_category] : new();

    public TableHeroPositionData GetData(HeroPositionType _type)
        => m_list.Find(x => x.type == _type);
}

public struct TableHeroPositionData
{
    public string key;
    public CategoryType_HeroPositon category;
    [JsonProperty] string effect;


    // CUSTOM
    public bool isActive => key.IsActive();
    public HeroPositionType type => System.Enum.Parse<HeroPositionType>(key);

    List<BattleStatData> m_statData;
    public List<BattleStatData> statData
    {
        get
        {
            if (m_statData == null)
            {
                m_statData = new();
                var db = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(effect);
                foreach (var d in db)
                {
                    BattleStatData statData = new();
                    statData.statType = System.Enum.Parse<BattleStatType>(d.Key);
                    statData.value = d.Value;

                    m_statData.Add(statData);
                }
            }
            return m_statData;
        }
    }

    public string name => TableManager.stringTable.GetHeroPositionType(key);
    public string stringAttribute
    {
        get
        {
            string result = "";

            int idx = 0;
            foreach (var s in statData)
            {
                if (idx > 0)
                    result += "\n";

                string stringPoint = "";
                switch (s.statType)
                {
                    case BattleStatType.attack_power:
                    case BattleStatType.defence:
                    case BattleStatType.health_max:
                        stringPoint = $"+{Mathf.RoundToInt(s.value).AmountKMBT()}";
                        break;
                    default:
                        stringPoint = $"+{s.value.AmountKMBT()}%";
                        break;
                }

                result += $"{s.statName} {stringPoint}";
                idx++;
            }
            return result;
        }
    }
}
