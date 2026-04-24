using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class Data_Stat_Relic
{
    const string key = "PP_Stat_Relic_Hero";

    Dictionary<string, int> m_dataHero;
    public IReadOnlyDictionary<string, int> dataHero => m_dataHero;


    Dictionary<HeroClassType, float> m_bonusClassBonus = new();
    public IReadOnlyDictionary<HeroClassType, float> bonusClassBonus => m_bonusClassBonus;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_dataHero = PPWorker.Get<Dictionary<string, int>>(key);

        if (m_dataHero == null)
        {
            m_dataHero = new();

            foreach (var hero in TableManager.hero.list)
                m_dataHero.Add(hero.key, 0);

            SaveData_Hero();
        }

        for (var t = HeroClassType.NONE + 1; t < HeroClassType.MAX; t++)
            m_bonusClassBonus.Add(t, 0);

        foreach (var d in m_dataHero)
        {
            if (d.Value == 0)
                continue;

            var classType = TableManager.hero.Get(d.Key).classType;
            m_bonusClassBonus[classType] += d.Value * 0.1f;
        }
    }

    public void Upgrade_HeroRelic(HeroInfoData _heroInfoData)
    {
        var key = _heroInfoData.key;
        var classType = _heroInfoData.classType;

        int level = m_dataHero[key];
        m_bonusClassBonus[classType] -= level * 0.1f;

        m_dataHero[key]++;
        m_bonusClassBonus[classType] += m_dataHero[key] * 0.1f;

        SaveData_Hero();
    }

    void SaveData_Hero()
    {
        PPWorker.Set(key, m_dataHero);
    }
}