using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

public class Data_Stat_Relic
{
    const string key = "PP_Stat_Relic";

    Dictionary<string, int> m_data = new();
    public IReadOnlyDictionary<string, int> data => m_data;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_data = PPWorker.Get<Dictionary<string, int>>(key);

        if (m_data.Count == 0)
        {
            foreach (var hero in DataManager.userInfo.myHero)
                m_data.Add(hero.key, 0);

            SaveData();
        }
    }

    void SaveData()
    {
        PPWorker.Set(key, m_data);
    }

    struct StatRelicLocalData
    {
        public string key;
        public int level;
    }
}