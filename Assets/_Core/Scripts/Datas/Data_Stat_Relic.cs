using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_Stat_Relic
{
    const string key_Relic = "PP_Stat_Relic_Relic";
    const string key_Treasure = "PP_Stat_Relic_Treasure";

    Dictionary<string, int> m_dataRelic;
    public IReadOnlyDictionary<string, int> dataRelic => m_dataRelic;

    Dictionary<HeroClassType, float> m_bonusClassBonus = new();
    public IReadOnlyDictionary<HeroClassType, float> bonusClassBonus => m_bonusClassBonus;


    List<TreasureBatchData> m_dataTreasure;
    public IReadOnlyList<TreasureBatchData> dataTreasure => m_dataTreasure;

    Dictionary<BattleStatType, BattleStatData> m_bonusTreasureBonus = new();
    public IReadOnlyDictionary<BattleStatType, BattleStatData> bonusTreasureBonus => m_bonusTreasureBonus;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_dataRelic = PPWorker.Get<Dictionary<string, int>>(key_Relic);

        // 히어로 유물 관련
        {
            if (m_dataRelic == null)
            {
                m_dataRelic = new();

                foreach (var hero in TableManager.hero.list)
                    m_dataRelic.Add(hero.key, 0);

                SaveData_Hero();
            }

            for (var t = HeroClassType.NONE + 1; t < HeroClassType.MAX; t++)
                m_bonusClassBonus.Add(t, 0);

            foreach (var d in m_dataRelic)
            {
                if (d.Value == 0)
                    continue;

                var classType = TableManager.hero.Get(d.Key).classType;
                m_bonusClassBonus[classType] += d.Value * 0.1f;
            }
        }

        PlayerPrefs.DeleteKey(key_Treasure);
        m_dataTreasure = PPWorker.Get<List<TreasureBatchData>>(key_Treasure);

        // 보물
        {
            if (m_dataTreasure == null)
            {
                m_dataTreasure = TableManager.treasure.list.Where(x => x.isActive)
                    .Select(x => new TreasureBatchData()
                    {
                        key = x.key,
                        isBatch = false
                    }).ToList();

                SaveData_Treasure();
            }

            for (int i = 0; i < m_dataTreasure.Count; i++)
            {
                if (m_dataTreasure[i].isBatch == true)
                    SetBonusTreasureBonus(m_dataTreasure[i].key, true);
            }
        }
    }

    public TreasureBatchData GetTreasureData(string _key)
        => m_dataTreasure.Find(x => x.key == _key);

    public void Upgrade_HeroRelic(HeroInfoData _heroInfoData)
    {
        var key = _heroInfoData.key;
        var classType = _heroInfoData.classType;

        int level = m_dataRelic[key];
        m_bonusClassBonus[classType] -= level * 0.1f;

        m_dataRelic[key]++;
        m_bonusClassBonus[classType] += m_dataRelic[key] * 0.1f;

        SaveData_Hero();
    }

    void SaveData_Hero()
    {
        PPWorker.Set(key_Relic, m_dataRelic);
    }

    public void SetTreasureStatus(string _key, bool _isBatch)
    {
        var idx = m_dataTreasure.FindIndex(x => x.key == _key);

        if (_isBatch == true && m_dataTreasure.Count(x => x.isBatch) >= 3)
            return;

        if (idx > -1)
        {
            var data = m_dataTreasure[idx];
            data.isBatch = _isBatch;
            data.tickBatch = _isBatch ? System.DateTime.UtcNow.Ticks : 0;

            m_dataTreasure[idx] = data;
            SaveData_Treasure();

            SetBonusTreasureBonus(_key, _isBatch);
        }
    }

    void SetBonusTreasureBonus(string _key, bool _isAdd)
    {
        var treasureData = TableManager.treasure.Get(_key);

        foreach (var d in treasureData.dbEffect)
        {
            if (m_bonusTreasureBonus.ContainsKey(d.Key) == false)
            {
                m_bonusTreasureBonus.Add(d.Key, new() { statType = d.Key });
                m_bonusTreasureBonus = m_bonusTreasureBonus.OrderBy(x => x.Value.statType).ToDictionary(x => x.Value.statType, x => x.Value);
            }

            var prev = m_bonusTreasureBonus[d.Key];
            prev.value += d.Value.value * (_isAdd ? 1 : -1);
            m_bonusTreasureBonus[d.Key] = prev;

            if (prev.value.Approximately(0))
                m_bonusTreasureBonus.Remove(prev.statType);
        }
    }

    void SaveData_Treasure()
    {
        PPWorker.Set(key_Treasure, m_dataTreasure);
    }

    public struct TreasureBatchData
    {
        public string key;
        public bool isBatch;
        public long tickBatch;
    }
}


