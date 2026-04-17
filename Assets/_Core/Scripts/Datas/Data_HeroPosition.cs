using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_HeroPosition : MonoBehaviour
{
    public Dictionary<CategoryType_HeroPositon, List<HeroPositionData>> data { get; private set; } = new();

    public async UniTask InitializeAsync()
    {
        await UniTask.WaitForSeconds(1f);
        // TODO TEST DATA
        List<TableHeroPositionData> db = new();
        db.Add(new() { position = HeroPositionType.GENERAL_WU, statType = StatType.attack_power, value = .1f });
        db.Add(new() { position = HeroPositionType.GENERAL_WU, statType = StatType.health_max, value = .1f });
        db.Add(new() { position = HeroPositionType.GENERAL_WU, statType = StatType.defence, value = .1f });
        db.Add(new() { position = HeroPositionType.GENERAL_PYO, statType = StatType.attack_power, value = .1f });
        db.Add(new() { category = CategoryType_HeroPositon.ETC, position = HeroPositionType.ETC_HOO, statType = StatType.health_max, value = .1f });

        data = db.GroupBy(x => x.category).ToDictionary(x => x.Key, x =>
        {
            return x.GroupBy(x => x.position).Select(_group =>
            {
                return new HeroPositionData()
                {
                    key = _group.Key,
                    bonusStat = _group.ToDictionary(x => x.statType, x => x.value)
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
    GENERAL_WU,
    GENERAL_PYO,
    ETC_HOO,
}

public struct TableHeroPositionData
{
    public CategoryType_HeroPositon category;
    public HeroPositionType position;
    public StatType statType;
    public float value;
}


[Serializable]
public struct HeroPositionData
{
    public HeroPositionType key;
    public Dictionary<StatType, float> bonusStat;

    //custom
    string m_heroKey;
    public string heroKey
    {
        get => m_heroKey;
        set => m_heroKey = value;
    }

    public string name => TableManager.stringTable.GetString(key.ToString());
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