using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Data_BossRaid
{
#if UNITY_EDITOR
    public int timerRunning { get; private set; } = 1;
#else
    public int timerRunning { get; private set; } = 5;
#endif

    CancellationTokenSource m_cts;

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
        //if (m_data.isActive == false)
        {
            m_data.TestDefault();
            SaveData();
        }

        TimerAsync().Forget();
    }

    public void ReleaseCTS()
        => m_cts = m_cts.Release();

    async UniTask TimerAsync()
    {
        ReleaseCTS();
        m_cts = new();
        var token = m_cts.Token;

        if (m_data.tickNextRound == 0)
        {
            await UniTask.WaitForSeconds(UnityEngine.Random.Range(10f, 20f));
            //m_data.tickNextRound = System.DateTime.UtcNow.AddMinutes(10).Ticks;
            m_data.tickNextRound = System.DateTime.UtcNow.AddSeconds(70).Ticks;
            SaveData();
        }

        // 대기중
        await UniTask.WaitUntil(() => System.DateTime.UtcNow.Ticks > m_data.tickNextRound);

        // 게임시작
        var tickEndRound = m_data.dtNextRound
            .AddMinutes(timerRunning)
            .AddSeconds(-Configure.instance.timeGapFromServer).Ticks;

        await UniTask.WaitUntil(() => tickEndRound < System.DateTime.UtcNow.Ticks, cancellationToken: token);

        m_data.tickPrevRound = m_data.tickNextRound;
        m_data.tickNextRound = 0;
        m_data.round++;
        m_data.prevGrade = m_data.nowGrade;

        SaveData();

        TimerAsync().Forget();
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
            userData.nickname = isMine ? DataManager.userInfo.nickname.ToString() : $"Nickname_{userData.prevRank:00#}";
            userData.point = UnityEngine.Random.Range(100, 10000);
            userData.power = UnityEngine.Random.Range(1000, 3000);
            userData.skin = TableManager.hero.list.RandomFirst().key;
            userData.indexProfile = -1;

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
            userData.nickname = isMine ? DataManager.userInfo.nickname.ToString() : $"Nickname_{userData.prevRank:00#}";
            userData.point = UnityEngine.Random.Range(1000, 1000000);
            userData.power = UnityEngine.Random.Range(1000, 3000);
            userData.skin = TableManager.hero.list.RandomFirst().key;
            userData.indexProfile = -1;

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

    CancellationTokenSource m_ctsTestDamage;
    public async UniTask TestAutoAttackDamageAsync()
    {
        m_ctsTestDamage = m_ctsTestDamage.Release(true);
        var token = m_ctsTestDamage.Token;

        var nickname = Utils.GetRandomNicknameArray(20);
        m_rankNow = new();

        var my = new BossRaidRankerUserData();
        my.nickname = DataManager.userInfo.nickname;
        my.uid = DataManager.userInfo.uid;
        m_rankNow.Add(my);

        for (int i = 0; i < nickname.Length; i++)
        {
            var user = new BossRaidRankerUserData();
            user.nickname = nickname[i];
            user.uid = DataManager.userInfo.uid + i + 1;

            m_rankNow.Add(user);
        }

        //while (true)
        //{
        //    await UniTask.WaitForSeconds(TeamManager.instance.mainHero.stat.attackSpeed);

        //    for (int i = 0; i < m_rankNow.Count; i++)
        //    {
        //        if (m_rankNow[i].uid == DataManager.userInfo.uid)
        //            continue;

        //        var damage = UnityEngine.Random.Range(50, 500);

        //        var ranker = m_rankNow[i];
        //        ranker.point += damage;
        //        m_rankNow[i] = ranker;

        //        StageManager.instance.enemyList[0].OnDamage(null, damage);

        //        RankBossRaidComponent.instance.UpdateRanker();
        //    }
        //}
    }

    public void TestDamageBoss(long _damage)
    {
        var my = m_rankNow[0];
        my.point += _damage;
        m_rankNow[0] = my;

        RankBossRaidComponent.instance.UpdateRanker();

        for (int i = 1; i < m_rankNow.Count; i++)
        {
            TestAttackAsync(i, _damage).Forget();
        }
    }

    async UniTask TestAttackAsync(int _index, long _damage)
    {
        await UniTask.WaitForSeconds(UnityEngine.Random.Range(0.2f, 1f));

        var damage = (long)(_damage * UnityEngine.Random.Range(0.5f, 1.5f));

        var ranker = m_rankNow[_index];
        ranker.point += damage;
        m_rankNow[_index] = ranker;

        StageManager.instance.enemyList[0].OnDamage(null, damage);

        RankBossRaidComponent.instance.UpdateRanker();
    }

    public void FinishedBossRaid()
    {
        m_ctsTestDamage = m_ctsTestDamage.Release();
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
        public System.DateTime dtPrevRound => Utils.GetDateTime(tickPrevRound);
        public System.DateTime dtNextRound => Utils.GetDateTime(tickNextRound);
        public System.DateTime dtEndSeason => Utils.GetDateTime(tickEndSeason);

        public void TestDefault()
        {
            round = 1;
            keyBoss = "LuBu";
            prevGrade = GradeType.Normal;
            tickPrevRound = System.DateTime.UtcNow.AddHours(-3).Ticks;
            tickNextRound = System.DateTime.UtcNow.AddSeconds(10).Ticks;
            tickEndSeason = System.DateTime.UtcNow.AddDays(20).Ticks;

            gradeMin = GradeType.Normal;
            gradeMax = GradeType.Normal;
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
