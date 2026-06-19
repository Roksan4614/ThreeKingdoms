using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Data_BossRaid
{
#if UNITY_EDITOR
    const int c_timerRunning = 1;
#else
    const int c_timerRunning = 5;
#endif
    public double remainSeconds { get; private set; }


    CancellationTokenSource m_cts;

    BossRaidData m_data;
    public BossRaidData data => m_data;

    BossRaidRankerData m_rankPoint = new(); // 포인트 랭킹
    public BossRaidRankerData rankPoint => m_rankPoint;

    BossRaidRankerData m_rankPrevRaid = new();  //이전 라운드 랭킹
    public BossRaidRankerData rankPrevRaid => m_rankPrevRaid;

    List<BossRaidRankerUserData> m_rankNow = new(); //현재랭킹
    public IReadOnlyList<BossRaidRankerUserData> rankNow => m_rankNow;

    BossRaidStatusType m_raidStatus; public BossRaidStatusType raidStatus => m_raidStatus;

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
        m_cts = m_cts.Release(true);
        var token = m_cts.Token;

        if (m_data.tickNextRound == 0)
        {
            await UniTask.WaitForSeconds(UnityEngine.Random.Range(10f, 20f));
            //m_data.tickNextRound = System.DateTime.UtcNow.AddMinutes(10).Ticks;
            m_data.tickNextRound = System.DateTime.UtcNow.AddSeconds(70).Ticks;
            SaveData();
        }

        m_raidStatus = BossRaidStatusType.Wait;
        // 대기중
        await UniTask.WaitUntil(() => System.DateTime.UtcNow.Ticks > m_data.tickNextRound);

        // 게임시작

        m_raidStatus = BossRaidStatusType.FirstPhase;
        Signal.instance.BossRaidStatus.Emit(m_raidStatus);

        remainSeconds = c_timerRunning * 60;
        var dtEnd = m_data.dtNextRound.AddMinutes(c_timerRunning);
        var tickEndRound = dtEnd.AddSeconds(-Configure.instance.timeGapFromServer).Ticks;

        m_data.tickEndRound = dtEnd.Ticks;

        await UniTask.WaitUntil(() => tickEndRound < System.DateTime.UtcNow.Ticks, cancellationToken: token);

        Finish_BossRaid();
        TimerAsync().Forget();
    }

    public void Start_BossRaid()
    {
        m_data.nowGrade = (GradeType)UnityEngine.Random.Range((int)m_data.gradeMin, (int)m_data.gradeMax + 1);
        SaveData();
    }

    public void Finish_FirstPhase()
    {
        m_raidStatus = BossRaidStatusType.Finish_FirstPhase;
        Signal.instance.BossRaidStatus.Emit(m_raidStatus);

        m_cts = m_cts.Release();
        remainSeconds = (m_data.dtEndRound - Utils.GetUTC()).TotalSeconds;


    }

    public void Start_SecondPhase()
    {
        var dtNow = Utils.GetUTC();
        m_data.tickSecondPhase = dtNow.Ticks;
        //남은 시간 + 3분 일껄?
        m_data.tickEndRound = dtNow.AddSeconds(remainSeconds + 60 * 3).Ticks;

        m_raidStatus = BossRaidStatusType.SecondPhase;
        Signal.instance.BossRaidStatus.Emit(m_raidStatus);
    }

    public void Finish_BossRaid()
    {
        m_raidStatus = BossRaidStatusType.Finished;
        Signal.instance.BossRaidStatus.Emit(m_raidStatus);

        m_data.tickPrevRound = m_data.tickNextRound;
        m_data.tickNextRound = m_data.tickEndRound = m_data.tickSecondPhase = 0;
        m_data.round++;
        m_data.prevGrade = m_data.nowGrade;

        SaveData();

        m_ctsTestDamage = m_ctsTestDamage.Release();
        m_rankNow = null;
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

    CancellationTokenSource m_ctsTestDamage;
    public void TestAddTestUser()
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
        public long tickSecondPhase;
        public long tickEndRound;

        public bool isActive => keyBoss.IsActive();
        public System.DateTime dtEndSeason => Utils.GetDateTime(tickEndSeason);
        public System.DateTime dtPrevRound => Utils.GetDateTime(tickPrevRound);
        public System.DateTime dtNextRound => Utils.GetDateTime(tickNextRound);
        public System.DateTime dtEndRound => Utils.GetDateTime(tickEndRound);
        public System.DateTime dtSecondPhase => Utils.GetDateTime(tickSecondPhase);

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

    public enum BossRaidStatusType
    {
        Wait,
        FirstPhase,
        Finish_FirstPhase,
        SecondPhase,
        Finished,
    }
}
