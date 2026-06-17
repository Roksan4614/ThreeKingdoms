using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_BossRaid
{
    BossRaidData m_data;
    public BossRaidData data => m_data;

    BossRaidRankerData m_rankPoint = new();
    public BossRaidRankerData rankPoint => m_rankPoint;

    BossRaidRankerData m_rankPrevRaid = new();
    public BossRaidRankerData rankPrevRaid => m_rankPrevRaid;

    List<BossRaidRankerUserData> m_rankNow;
    public IReadOnlyList<BossRaidRankerUserData> rankNow => m_rankNow;

    const string c_key = "pp_bossraid";

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_data = PPWorker.Get<BossRaidData>(c_key);

        // TEST
        {
            if (m_data.isActive == false)
            {
                m_data.TestDefault();
                SaveData();
            }
        }
    }

    public async UniTask DoLoadAsync_RankData()
    {
        await UniTask.Yield();

        m_rankPoint.ranker = new();

        for (int i = 0; i < 50; i++)
        {
            bool isMine = i == 21;
            BossRaidRankerUserData userData = new();

            userData.uid = isMine ? DataManager.userInfo.uid : DataManager.userInfo.uid + i + 1;
            userData.prevRank = i + 1;
            userData.nickname = isMine ? DataManager.userInfo.uid.ToString() : $"Nickname_{userData.prevRank:00#}";
            userData.point = UnityEngine.Random.Range(100, 10000);
            userData.power = UnityEngine.Random.Range(1000, 3000);
            userData.skin = TableManager.hero.list.RandomFirst().key;

            m_rankPoint.ranker.Add(userData);

            if (isMine)
                m_rankPoint.my = userData;
        }

        m_rankPoint.ranker = m_rankPoint.ranker.SortByDescending(x => x.point);

        for (int i = 0; i < m_rankPoint.ranker.Count; i++)
        {
            var data = m_rankPoint.ranker[i];
            data.rank = i + 1;
            m_rankPoint.ranker[i] = data;

            if (data.uid == m_rankPoint.my.uid)
                m_rankPoint.my.rank = data.rank;
        }

        m_rankPrevRaid.ranker = new();

        for (int i = 0; i < 50; i++)
        {
            bool isMine = i == 21;
            BossRaidRankerUserData userData = new();

            userData.uid = isMine ? DataManager.userInfo.uid : DataManager.userInfo.uid + i + 1;
            userData.prevRank = i + 1;
            userData.nickname = isMine ? DataManager.userInfo.uid.ToString() : $"Nickname_{userData.prevRank:00#}";
            userData.point = UnityEngine.Random.Range(1000, 1000000);
            userData.power = UnityEngine.Random.Range(1000, 3000);
            userData.skin = TableManager.hero.list.RandomFirst().key;

            m_rankPrevRaid.ranker.Add(userData);

            if (isMine)
                m_rankPrevRaid.my = userData;
        }

        m_rankPrevRaid.ranker = m_rankPrevRaid.ranker.SortByDescending(x => x.point);

        for (int i = 0; i < m_rankPrevRaid.ranker.Count; i++)
        {
            var data = m_rankPrevRaid.ranker[i];
            data.rank = i + 1;
            m_rankPrevRaid.ranker[i] = data;

            if (data.uid == m_rankPrevRaid.my.uid)
                m_rankPrevRaid.my.rank = data.rank;
        }
    }

    public void StartBossRaid()
    {
        m_data.nowGrade = (GradeType)UnityEngine.Random.Range((int)m_data.gradeMin, (int)m_data.gradeMax + 1);
        SaveData();
    }

    public async UniTask TestAutoAttackDamageAsync()
    {
        //m_rankNow.OrderByDescending
    }

    public void FinishedBossRaid()
    {
        m_rankNow = null;
    }

    void SaveData()
        => PPWorker.Set(c_key, m_data);

    public struct BossRaidData
    {
        public int round;

        public string keyBoss;

        public GradeType prevGrade;
        public GradeType nowGrade;

        public GradeType gradeMin;
        public GradeType gradeMax;

        public long tickEndSeason;
        public long tickPrevRound;
        public long tickNextRound;

        public bool isActive => keyBoss.IsActive();
        public System.DateTime dtPrevRound => new System.DateTime(tickPrevRound, System.DateTimeKind.Utc);
        public System.DateTime dtNextRound => new System.DateTime(tickNextRound, System.DateTimeKind.Utc);
        public System.DateTime dtEndSeason => new System.DateTime(tickEndSeason, System.DateTimeKind.Utc);

        public void TestDefault()
        {
            round = 1;
            keyBoss = "LuBu";
            prevGrade = GradeType.Normal;
            tickPrevRound = System.DateTime.UtcNow.AddHours(-3).Ticks;
            tickNextRound = System.DateTime.UtcNow.AddSeconds(90).Ticks;
            tickEndSeason = System.DateTime.UtcNow.AddDays(20).Ticks;

            gradeMin = GradeType.Normal;
            gradeMax = GradeType.Legend;
        }
    }

    public struct BossRaidRankerData
    {
        public List<BossRaidRankerUserData> ranker;
        public BossRaidRankerUserData my;
    }

    public struct BossRaidRankerUserData
    {
        public int rank;
        public int prevRank;
        public int indexProfile;
        public string skin;
        public string nickname;
        public int uid;
        public long point;
        public int power;
    }
}
