using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class Data_Stat_Relic
{
    const string key = "PP_Stat_Relic_Hero";
    const string key_Relic = "PP_Stat_Relic_Relic";

    Dictionary<string, int> m_dataHero;
    public IReadOnlyDictionary<string, int> dataHero => m_dataHero;

    Dictionary<HeroClassType, float> m_bonusClassBonus = new();
    public IReadOnlyDictionary<HeroClassType, float> bonusClassBonus => m_bonusClassBonus;


    List<(string key, bool isBatch)> m_dataRelic;
    public IReadOnlyList<(string key, bool isBatch)> dataRelic => m_dataRelic;

    Dictionary<BattleStatType, BattleStatData> m_bonusRelicBonus = new();
    public IReadOnlyDictionary<BattleStatType, BattleStatData> bonusRelicBonus => m_bonusRelicBonus;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_dataHero = PPWorker.Get<Dictionary<string, int>>(key);

        // 히어로 유물 관련
        {
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

        m_dataRelic = PPWorker.Get<List<(string key, bool isBatch)>>(key_Relic);

        // 보물
        {
            if (m_dataRelic == null)
            {
                m_dataRelic = new();
                m_dataRelic.Add(("막야검", false));
                m_dataRelic.Add(("적토마", false));
                m_dataRelic.Add(("손자병법서", false));
                SaveData_Relic();
            }

            for (int i = 0; i < m_dataRelic.Count; i++)
            {
                if (m_dataRelic[i].isBatch == true)
                    SetBonusRelicBonus(m_dataRelic[i].key, true);
            }
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

    public void SetRelicStatus(string _key, bool _isBatch)
    {
        var idx = m_dataRelic.FindIndex(x => x.key == _key);

        if (_isBatch == true && m_dataRelic.Count(x => x.isBatch) >= 3)
            return;

        if (idx > -1)
        {
            var data = m_dataRelic[idx];
            data.isBatch = _isBatch;
            m_dataRelic[idx] = data;
            SaveData_Relic();

            SetBonusRelicBonus(_key, _isBatch);
        }
    }

    void SetBonusRelicBonus(string _key, bool _isAdd)
    {
        var relicData = TableManager.relic.GetGroupData(_key);

        foreach (var d in relicData.statData)
        {
            if (m_bonusRelicBonus.ContainsKey(d.statType) == false)
            {
                m_bonusRelicBonus.Add(d.statType, new() { statType = d.statType });
                m_bonusRelicBonus = m_bonusRelicBonus.OrderBy(x => x.Value.statType).ToDictionary(x => x.Value.statType, x => x.Value);
            }

            var prev = m_bonusRelicBonus[d.statType];
            prev.value += d.value * (_isAdd ? 1 : -1);
            m_bonusRelicBonus[d.statType] = prev;

            if (prev.value.Approximately(0))
                m_bonusRelicBonus.Remove(prev.statType);
        }
    }

    void SaveData_Relic()
    {
        PPWorker.Set(key_Relic, m_dataRelic);
    }
}


