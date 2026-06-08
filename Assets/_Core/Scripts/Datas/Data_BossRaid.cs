using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Data_BossRaid
{
    BossRaidData m_data;
    public BossRaidData data => m_data;

    List<BossRaidRankerData> m_dataRank = new();
    public IReadOnlyList<BossRaidRankerData> dataRank => m_dataRank;

    const string c_key = "pp_bossraid";

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_data = PPWorker.Get<BossRaidData>(c_key);

        // TEST
        {
            if (m_data.isActive == false)
            {
                m_data.Default();
                SaveData();
            }

            DoLoad_RankData().Forget();
        }
    }

    public async UniTask DoLoad_RankData()
    {
        await UniTask.Yield();

        for (int i = 0; i < 50; i++)
        {
            BossRaidRankerData rankerData = new();

            rankerData.prevRank = i + 1;
            rankerData.nickname = i == 21 ? DataManager.userInfo.uid.ToString() : $"Nickname_{rankerData.prevRank:00#}";
            rankerData.point = UnityEngine.Random.Range(1000, 1000000);
            rankerData.power = UnityEngine.Random.Range(1000, 3000);

            m_dataRank.Add(rankerData);
        }
    }

    void SaveData()
        => PPWorker.Set(c_key, m_data);

    public struct BossRaidData
    {
        public int round;

        public string keyBoss;

        public long tickPrevRound;
        public long tickNextRound;

        public bool isActive => keyBoss.IsActive();

        public void Default()
        {
            round = 1;
            keyBoss = "ZhangFei";
            tickPrevRound = System.DateTime.UtcNow.AddHours(-3).Ticks;
        }
    }

    public struct BossRaidRankerData
    {
        public int prevRank;
        public string skin;
        public string nickname;
        public int uid;
        public long point;
        public int power;
    }
}
