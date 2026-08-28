using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class Data_Stat_Relic
{
    const string key_Treasure = "PP_Stat_Relic_Treasure";


    Dictionary<HeroClassType, float> m_bonusClassBonus = new();
    public IReadOnlyDictionary<HeroClassType, float> bonusClassBonus => m_bonusClassBonus;


    List<TreasureBatchData> m_dataTreasure;
    public IReadOnlyList<TreasureBatchData> dataTreasure => m_dataTreasure;

    Dictionary<BattleStatType, BattleStatData> m_bonusTreasureBonus = new();
    public IReadOnlyDictionary<BattleStatType, BattleStatData> bonusTreasureBonus => m_bonusTreasureBonus;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        // 히어로 유물 관련
        {
            for (var t = HeroClassType.NONE + 1; t < HeroClassType.MAX; t++)
                m_bonusClassBonus.Add(t, 0);

            var myHero = DataManager.userInfo.myHero;
            for (int i = 0; i < myHero.Count; i++)
                m_bonusClassBonus[myHero[i].classType] += myHero[i].relicLevel;
        }

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
    public int GetRelicLevel(string _key)
        => DataManager.userInfo.GetHeroInfoData(_key)?.relicLevel ?? 0;

    public void Upgrade_HeroRelic(HeroInfoData _heroInfoData)
    {
        var heroData = DataManager.userInfo.GetHeroInfoData(_heroInfoData.key);
        var classType = _heroInfoData.classType;

        m_bonusClassBonus[classType] -= heroData.relicLevel;

        heroData.relicLevel++;
        m_bonusClassBonus[classType] += heroData.relicLevel;
        
        DataManager.userInfo.Update(heroData);
        DataManager.userInfo.ResetResultStat();
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
        DataManager.userInfo.ResetResultStat();
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


