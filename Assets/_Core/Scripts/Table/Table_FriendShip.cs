using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_FriendShip : BaseTable<string, TableFriendShipOriginData>
{
    List<TableFriendShipData> m_dbList = new();

    Dictionary<string, TableFriendShipData> m_dbFriendShip = new();
    public IReadOnlyList<TableFriendShipData> dbList => m_dbList;

    public Table_FriendShip(List<TableFriendShipOriginData> _table) : base(_table)
    {
        List<TableFriendShipOriginData> list = new();

        list.Add(new()
        {
            key = "도원결의",
            heroes = "LiuBei, GuanYu, ZhangFei",
            statType = BattleStatType.attack_power,
            value = 10
        });

        list.Add(new()
        {
            key = "도원결의",
            statType = BattleStatType.health_max,
            value = 10
        });

        list.Add(new()
        {
            key = "촉의 기둥",
            heroes = "GuanYu, ZhangFei, ZhaYun",
            statType = BattleStatType.move_speed,
            value = 5
        });

        m_dbFriendShip = list.GroupBy(x => x.key).ToDictionary(x => x.Key, x =>
        {
            TableFriendShipData data = new();
            data.key = x.Key;
            data.heroes = x.Select(s => s.heroes).First();
            data.statData = x.Select(s => new BattleStatData()
            {
                statType = s.statType,
                value = s.value,

            }).ToList();

            return data;
        });

        m_dbList = m_dbFriendShip.Values.ToList();
    }
}

public struct TableFriendShipOriginData
{
    public string key;
    public string heroes;
    public BattleStatType statType;
    public float value;
}

public struct TableFriendShipData
{
    public string key;
    public string heroes;
    public List<BattleStatData> statData;

    // CUSTOM
    public List<GradeType> grade { get; set; }
    public GradeType minGrade { get; set; }

    string[] m_splitHero;
    public string[] splitHero
    {
        get
        {
            if (m_splitHero == null)
                m_splitHero = heroes.Replace(" ", "").Split(",");

            return m_splitHero;
        }
    }

    public string title => key;
}