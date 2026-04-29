using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class Data_Stat_FriendShip
{
    List<TableFriendShipData> m_dbFriendShip = new();
    public IReadOnlyList<TableFriendShipData> dbFriendShip => m_dbFriendShip;

    Dictionary<BattleStatType, BattleStatData> m_bonusStatBonus = new();
    public IReadOnlyDictionary<BattleStatType, BattleStatData> bonusStatBonus => m_bonusStatBonus;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        Reload();
    }

    public void Reload()
    {
        m_dbFriendShip.Clear();
        m_dbFriendShip.AddRange(TableManager.friendShip.dbList);

        m_bonusStatBonus.Clear();

        for (int i = 0; i < m_dbFriendShip.Count; i++)
        {
            var db = m_dbFriendShip[i];
            db.minGrade = GradeType.MAX;
            db.grade = db.splitHero.Select(x =>
            {
                var heroInfoData = DataManager.userInfo.GetHeroInfoData(x);

                var result = heroInfoData.isActive ? heroInfoData.grade : GradeType.NONE;
                if (db.minGrade > result)
                    db.minGrade = result;

                return result;
            }).ToList();
            m_dbFriendShip[i] = db;

            var countNone = db.grade.Count(x => x == GradeType.NONE);
            if (countNone == 0)
            {
                foreach (var d in db.statData)
                {
                    if (m_bonusStatBonus.ContainsKey(d.statType) == false)
                    {
                        m_bonusStatBonus.Add(d.statType, new() { statType = d.statType });
                        m_bonusStatBonus = m_bonusStatBonus.OrderBy(x => x.Value.statType).ToDictionary(x => x.Value.statType, x => x.Value);
                    }

                    var prev = m_bonusStatBonus[d.statType];
                    prev.value += d.value + d.value * ((int)db.minGrade * 0.1f);
                    m_bonusStatBonus[d.statType] = prev;
                }
            }
        }
    }
}
