using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Data_BossRaid
{
#if UNITY_EDITOR
    const int c_timerRunning = 11;
#else
    const int c_timerRunning = 5;
#endif
    public double remainSeconds { get; private set; }


    CancellationTokenSource m_cts;

    BossRaidData m_data;
    public BossRaidData data => m_data;

    RankerData m_rankPoint = new(); // 포인트 랭킹
    public RankerData rankPoint => m_rankPoint;

    RankerData m_rankPrevRaid = new();  //이전 라운드 랭킹
    public RankerData rankPrevRaid => m_rankPrevRaid;

    List<RankerUserData> m_rankNow = new(); //현재랭킹
    public IReadOnlyList<RankerUserData> rankNow => m_rankNow;

    BossRaidStatusType m_raidStatus; public BossRaidStatusType raidStatus => m_raidStatus;

    const string c_key = "pp_bossraid";

    public async UniTask InitializeAsync()
    {
        await UniTask.NextFrame();

        m_data = PPWorker.Get<BossRaidData>(c_key);

        if( m_data == null)
        {
            m_data = new();
            m_data.TestDefault();
            SaveData();
        }


        var key = m_data.keyBoss + "_BossRaid";
        AddressableManager.instance.Load_HeroCharacterAsync(key).Forget();

        TimerAsync().Forget();
    }

    public void ReleaseCTS()
        => m_cts = m_cts.ReleaseCTS();

    async UniTask TimerAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        if (m_data.tickNextRound == 0)
        {
            await UniTask.WaitForSeconds(UnityEngine.Random.Range(10f, 20f), cancellationToken: token);
            //m_data.tickNextRound = System.DateTime.UtcNow.AddMinutes(10).Ticks;
            m_data.tickNextRound = System.DateTime.UtcNow.AddSeconds(70).Ticks;
            SaveData();
        }

        m_raidStatus = BossRaidStatusType.Wait;
        // 대기중
        await UniTask.WaitUntil(() => System.DateTime.UtcNow.Ticks > m_data.tickNextRound, cancellationToken: token);

        // 게임시작

        m_raidStatus = BossRaidStatusType.FirstPhase;
        Signal.instance.BossRaidStatus.Emit(m_raidStatus);

        remainSeconds = c_timerRunning * 60;
        var dtEnd = m_data.dtNextRound.AddMinutes(c_timerRunning);
        var tickEndRound = dtEnd.AddSeconds(-Configure.instance.timeGapFromServer).Ticks;

        m_data.tickSecondPhase = 0;
        m_data.tickEndRound = dtEnd.Ticks;
        m_data.nowGrade = (GradeType)UnityEngine.Random.Range((int)m_data.gradeMin, (int)m_data.gradeMax + 1);
        SaveData();

        await UniTask.WaitUntil(() => tickEndRound < System.DateTime.UtcNow.Ticks, cancellationToken: token);

        BossRaidWorker.instance.Finish_BossRaid(false);

    }

    public void Start_BossRaid()
    {
        m_raidStatus = BossRaidStatusType.FirstPhase;
        Signal.instance.BossRaidStatus.Emit(m_raidStatus);
    }

    public void Finish_FirstPhase()
    {
        m_raidStatus = BossRaidStatusType.Finish_FirstPhase;
        Signal.instance.BossRaidStatus.Emit(m_raidStatus);

        m_cts = m_cts.ReleaseCTS();
        remainSeconds = (m_data.dtEndRound - Utils.GetUTC()).TotalSeconds;
    }

    public void Wait_SecondPhase()
    {
        m_raidStatus = BossRaidStatusType.Wait_SecondPhase;
        Signal.instance.BossRaidStatus.Emit(m_raidStatus);
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

        m_ctsTestDamage = m_ctsTestDamage.ReleaseCTS();
        m_cts = m_cts.ReleaseCTS();
    }

    public void ExitBossRaid()
    {
        TimerAsync().Forget();
        m_rankNow.Clear();
    }

    public async UniTask DoLoadAsync_RankData()
    {
        await UniTask.NextFrame();

        m_rankPoint.ranker = new();

        for (int i = 0; i < 50; i++)
        {
            bool isMine = i == 21;
            RankerUserData userData = new();

            userData.uid = isMine ? DataManager.userInfo.uid : DataManager.userInfo.uid + i + 1;
            userData.prevRank = i + 1;
            userData.nickname = isMine ? DataManager.userInfo.nickname.ToString() : $"Nickname_{userData.prevRank:00#}";
            userData.point = UnityEngine.Random.Range(100, 10000);
            userData.power = UnityEngine.Random.Range(1000, 3000);
            userData.skin = TableManager.hero.GetHeroList().RandomFirst().key;

            if (isMine)
            {
                //userData.point = 10001;
                m_rankPoint.my = userData;
            }

            m_rankPoint.ranker.Add(userData);
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
            RankerUserData userData = new();

            userData.uid = isMine ? DataManager.userInfo.uid : DataManager.userInfo.uid + i + 1;
            userData.prevRank = i + 1;
            userData.nickname = isMine ? DataManager.userInfo.nickname.ToString() : $"Nickname_{userData.prevRank:00#}";
            userData.point = UnityEngine.Random.Range(1000, 1000000);
            userData.power = UnityEngine.Random.Range(1000, 3000);
            userData.skin = TableManager.hero.GetHeroList().RandomFirst().key;

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
        m_ctsTestDamage = m_ctsTestDamage.ReleaseCTS(true);
        var token = m_ctsTestDamage.Token;

        var nickname = Utils.GetRandomNicknameArray(20);
        m_rankNow.Clear();

        var my = new RankerUserData();
        my.nickname = DataManager.userInfo.nickname;
        my.uid = DataManager.userInfo.uid;
        m_rankNow.Add(my);

        for (int i = 0; i < nickname.Length; i++)
        {
            var user = new RankerUserData();
            user.nickname = nickname[i];
            user.uid = DataManager.userInfo.uid + i + 1;

            m_rankNow.Add(user);
        }
    }

    public void SendDamageBossAsync(long _damage)
    {
        if (m_rankNow.Count == 0)
            return;

        var my = m_rankNow[0];
        my.point += _damage;
        m_rankNow[0] = my;

        for (int i = 1; i < m_rankNow.Count; i++)
            TestAttackAsync(i, _damage).Forget();

        m_rankNow = m_rankNow.SortByDescending(x => x.point);

        var prevRank = 1;
        var prevPoint = long.MaxValue;

        for (int i = 0; i < m_rankNow.Count; i++)
        {
            var ranker = m_rankNow[i];

            if (ranker.point < prevPoint)
            {
                ranker.rank = i + 1;
                prevRank = ranker.rank;
                prevPoint = ranker.point;
            }
            else
                ranker.rank = prevRank;

            m_rankNow[i] = ranker;
        }

        RankBossRaidComponent.instance.UpdateRanker();
    }

    async UniTask TestAttackAsync(int _index, long _damage)
    {
        await UniTask.WaitForSeconds(UnityEngine.Random.Range(0.2f, 1f));

        if (m_raidStatus != BossRaidStatusType.FirstPhase &&
            m_raidStatus != BossRaidStatusType.SecondPhase)
            return;

        if (_index < m_rankNow.Count)
        {
            var damage = (long)(_damage * UnityEngine.Random.Range(0.5f, 1.5f));

            var ranker = m_rankNow[_index];
            ranker.point += damage;
            m_rankNow[_index] = ranker;

            StageManager.instance.enemyList[0].OnDamage(null, damage);
        }
    }

    void SaveData()
        => PPWorker.Set(c_key, m_data);

    [JsonObject(MemberSerialization.OptIn)]
    public class BossRaidData
    {
        [JsonProperty] public int round;

        [JsonProperty] public string keyBoss;

        [JsonProperty] public GradeType prevGrade;
        [JsonProperty] public GradeType nowGrade;

        [JsonProperty] public GradeType gradeMin;
        [JsonProperty] public GradeType gradeMax;

        [JsonProperty] public long tickEndSeason;
        [JsonProperty] public long tickPrevRound;
        [JsonProperty] public long tickNextRound;
        [JsonProperty] public long tickSecondPhase;
        [JsonProperty] public long tickEndRound;

        public System.DateTime dtEndSeason => Utils.GetDateTime(tickEndSeason);
        public System.DateTime dtPrevRound => Utils.GetDateTime(tickPrevRound);
        public System.DateTime dtNextRound => Utils.GetDateTime(tickNextRound);
        public System.DateTime dtEndRound => Utils.GetDateTime(tickEndRound);
        public System.DateTime dtSecondPhase => Utils.GetDateTime(tickSecondPhase);

        public string bossName
        {
            get
            {
                string name = $"[{TableManager.stringTable.GetGradeType(nowGrade, _isColor: true)}</color>] ";
                if (DataManager.bossRaid.raidStatus >= BossRaidStatusType.Wait_SecondPhase)
                    name += "진.";
                name += TableManager.hero.Get(keyBoss).name;
                return name;
            }
        }

        public void TestDefault()
        {
            round = 1;
            keyBoss = "LuBu";
            prevGrade = GradeType.Normal;
            tickPrevRound = System.DateTime.UtcNow.AddHours(-3).Ticks;
            tickNextRound = System.DateTime.UtcNow.AddSeconds(10).Ticks;
            tickEndSeason = Utils.GetNextMonthMidnight(1).Ticks;

            gradeMin = GradeType.Normal;
            gradeMax = GradeType.Normal;
        }
    }

    public enum BossRaidStatusType
    {
        Wait,

        FirstPhase,
        Finish_FirstPhase,

        Wait_SecondPhase,
        SecondPhase,

        Finished,
    }
}

public class RankerData
{
    public List<RankerUserData> ranker;
    public RankerUserData my;
}

[JsonObject(MemberSerialization.OptIn)]
public class RankerUserData
{
    [JsonProperty] public int rank;
    [JsonProperty] public int prevRank;
    [JsonProperty] public int indexProfile;
    [JsonProperty] public string skin;
    [JsonProperty] public string nickname;
    [JsonProperty] public int uid;
    [JsonProperty] public long point;
    [JsonProperty] public long power;
    [JsonProperty] int? tier;

    public int tierTournament => tier ?? 8;

    public void SetTier(int _tier) => tier = _tier;
}
