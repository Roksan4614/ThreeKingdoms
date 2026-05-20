using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class Table_CastleMission_Reward : BaseTable<string, TableCastleMissionRewardData>
{
    Dictionary<string, Dictionary<GradeType, List<TableCastleMissionRewardData>>> m_db = new();

    public Table_CastleMission_Reward(List<TableCastleMissionRewardData> _data) : base(_data)
    {
        m_db = _data.GroupBy(x => x.key).ToDictionary(
            x => x.Key,
            x => x.GroupBy(g => g.grade).ToDictionary(g => g.Key, g => g.ToList()));
    }
}

public struct TableCastleMissionRewardData
{
    public string key;
    public GradeType grade;
    public int unlock_pct;
    public string reward_key;
    public string reward_value;
    public int reward_min;
    public int reward_max;
    public float drop_rate;

    public bool isActive => key.IsActive();
    string m_keyProper;
    public string keyProper
    {
        get
        {
            if (m_keyProper.IsActive() == false)
                m_keyProper = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key);
            return m_keyProper;
        }
    }
}