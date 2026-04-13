using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

public class Data_Stat_Relic
{
    const string key = "PP_Stat_Relic_Hero";

    Dictionary<string, int> m_dataHero = new();
    public IReadOnlyDictionary<string, int> dataHero => m_dataHero;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_dataHero = PPWorker.Get<Dictionary<string, int>>(key);

        if (m_dataHero == null)
        {
            m_dataHero = new();
            foreach (var hero in DataManager.userInfo.myHero)
                m_dataHero.Add(hero.key, 0);

            SaveData_Hero();
        }
    }

    void SaveData_Hero()
    {
        PPWorker.Set(key, m_dataHero);
    }

    struct StatRelicLocalData
    {
        public string key;
        public int level;
    }
}