using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Table_Hero_Position : BaseTable<PositionType, TableHeroPositionData>
{
    Dictionary<PositionCategory, List<TableHeroPositionData>> m_group;

    public Table_Hero_Position(List<TableHeroPositionData> _table) : base(_table)
    {
        m_group = _table.GroupBy(x => x.position_category).ToDictionary(x => x.Key, x => x.ToList());
    }

    public List<TableHeroPositionData> GetPositionds(PositionCategory _category)
        => m_group.ContainsKey(_category) ? m_group[_category] : new();

    public TableHeroPositionData GetData(PositionType _type)
        => m_list.Find(x => x.key == _type);
}

public class TableHeroPositionData
{
    public PositionType key;
    public PositionCategory position_category;
    [JsonProperty] string effect;
    public int unlock_condition_value;
    public int equip_condition_value;

    // CUSTOM
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

    public string name => TableManager.stringTable.GetPositionType(key);
    public string nameShort => TableManager.stringTable.GetPositionTypeShort(key);
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
