using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_HeroPosition : MonoBehaviour
{
    public Dictionary<CategoryType_HeroPositon, List<HeroPositionData>> data { get; private set; } = new();

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        // TODO TEST DATA
        List<TableHeroPositionData> db = new();

        List<BattleStatType> dbStat = new();
        for (var type = BattleStatType.NONE + 1; type < BattleStatType.MAX; type++)
            dbStat.Add(type);

        dbStat = dbStat.SortBy(x => Random.value);
        float percent = .5f;
        for (var type = HeroPositionType.NONE + 1; type < HeroPositionType.MAX; type++)
        {
            int idx = (int)type;
            db.Add(new()
            {
                position = type,
                battleStatType = dbStat[idx],
                value = .1f
            });

            while (Random.value < percent)
            {
                idx++;
                db.Add(new()
                {
                    position = type,
                    battleStatType = dbStat[idx],
                    value = .1f
                });
                percent *= .5f;
            }
            percent = .5f;
            dbStat = dbStat.SortBy(x => Random.value);
        }

        data = db.GroupBy(x => x.category).ToDictionary(x => x.Key, x =>
        {
            return x.GroupBy(x => x.position).Select(_group =>
            {
                return new HeroPositionData()
                {
                    key = _group.Key,
                    bonusStat = _group.ToDictionary(x => x.battleStatType, x => x.value)
                };
            }).ToList();
        });
    }

    void SetBindHero(CategoryType_HeroPositon _category, HeroPositionType _key, string _heroKey)
    {
        if (data.ContainsKey(_category) == false)
        {
            IngameLog.Add("HeroPosition Cant Find: " + _category);
            return;
        }

        int idx = data[_category].FindIndex(x => x.heroKey.Equals(_heroKey));
        if (idx > -1)
        {
            var d = data[_category][idx];
            d.heroKey = null;
            data[_category][idx] = d;

            //TODO 보너스 스탯에서 차감해줘야 해
        }

        idx = data[_category].FindIndex(x => x.key == _key);
        {
            var d = data[_category][idx];
            d.heroKey = _heroKey;
            data[_category][idx] = d;

            //TODO 보너스 스탯에서 증가시켜줘야 해
        }
    }
}

public enum CategoryType_HeroPositon
{
    GENERAL,
    ETC,
}

public enum HeroPositionType
{
    NONE = -1,

    PRIME_MINISTER,
    GENERAL_CHIEF,
    GENERAL_VANGUARD,
    GENERAL_LEFT,
    GENERAL_RIGHT,
    GENERAL_REAR,

    MAX
}

public struct TableHeroPositionData
{
    public CategoryType_HeroPositon category;
    public HeroPositionType position;
    public BattleStatType battleStatType;
    public float value;
}


[System.Serializable]
public struct HeroPositionData
{
    public HeroPositionType key;
    public Dictionary<BattleStatType, float> bonusStat;

    //custom
    string m_heroKey;
    public string heroKey
    {
        get => m_heroKey;
        set => m_heroKey = value;
    }

    public string name => TableManager.stringTable.GetHeroPositionType(key);
    public string stringAttribute
    {
        get
        {
            string result = "";

            int idx = 0;
            foreach (var s in bonusStat)
            {
                if (idx > 0)
                    result += "\n";

                result += $"{s.Key} +{s.Value:0.00}%";
                idx++;
            }
            return result;
        }
    }
}