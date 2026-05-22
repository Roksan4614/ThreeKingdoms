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

    public IReadOnlyList<TableCastleMissionRewardData> GetReward(Data_Castle_Mission.CastleMissionData _missionData)
    {
        // 특수한 미션이 세팅되어 있다면
        if (m_db.ContainsKey(_missionData.key) == true)
        {
            var db = m_db[_missionData.key];

            // 해당 난이도가 있다면
            if (db.ContainsKey(_missionData.grade))
                return db[_missionData.grade];
        }
        else
        {
            var key = _missionData.dbData.statType.ToString().ToLower();
            if (m_db.ContainsKey(key))
            {
                var db = m_db[key];

                if (db.ContainsKey(_missionData.grade))
                    return db[_missionData.grade];
            }
        }

        return null;
    }

}

public struct TableCastleMissionRewardData
{
    public string key;
    public GradeType grade;
    public int unlock_pct;
    public ItemType reward_key;
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

    TableItemData m_itemData;
    public TableItemData itemData
    {
        get
        {
            if (m_itemData.isActive == false)
                m_itemData = new()
                {
                    key = reward_key,
                    count = reward_max,
                    value = reward_value
                };
            return m_itemData;
        }
    }
}