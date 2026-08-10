using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Rev9.Tournament
{
    public partial class TournamentWorker
    {
        public static TournamentWorker instance { get; private set; } = new();
        public void Release()
        {
            m_ctsRefresh = m_ctsRefresh.ReleaseCTS();
            instance = null;
        }

        TournamentData m_data;
        public static TournamentData data => instance.m_data;

        public bool isAttackType { get; set; }


#if SERVICE_DEV
        const int c_refreshMinute = 1;
#else
        const int c_refreshMinute = 5;
#endif

        CancellationTokenSource m_ctsRefresh;

        public async UniTask InitailizeAsync()
        {
            if (m_data.isActive == false)
            {
                PPWorker.DeleteKey(PlayerPrefsType.TOURNAMENT);
                m_data = PPWorker.Get<TournamentData>(PlayerPrefsType.TOURNAMENT);
            }

            // 날짜 확인
            var dtBattle = Utils.GetDateTime(m_data.tick);
            if (dtBattle.Date < Utils.GetUTC().Date)
            {
                SlotDayChange();
                await API_LoadBattleListAsync();
            }

            // 갱신 업데이트
            var dtRefresh = Utils.GetDateTime(m_data.tickRefresh);
            int addCount = (int)(Utils.GetUTC() - dtRefresh).TotalMinutes;
            addCount /= 5;
            m_data.countRefresh = Mathf.Min(3, m_data.countRefresh + addCount);
            if (m_data.countRefresh < 3)
                TimerRefreshCountAsync().Forget();
            else
                SaveData();

            Signal.instance.DayChange.connect = SlotDayChange;
        }

        public void StopTimer() => m_ctsRefresh = m_ctsRefresh.ReleaseCTS();

        // 승급이나 강화했을 때 
        public void UpdateHero()
        {
            if (m_data.isActive == false)
                m_data = PPWorker.Get<TournamentData>(PlayerPrefsType.TOURNAMENT);

            for (int i = 0; i < 4; i++)
            {
                if (m_data.teamAttack.heroes != null && i < m_data.teamAttack.heroes.Count)
                {
                    var newData = DataManager.userInfo.GetHeroInfoData(m_data.teamAttack.heroes[i].key);
                    var data = m_data.teamAttack.heroes[i];
                    newData.sortIdx = data.sortIdx;
                    m_data.teamAttack.heroes[i] = newData;
                }
                if (m_data.teamDefence.heroes != null && i < m_data.teamDefence.heroes.Count)
                {
                    var newData = DataManager.userInfo.GetHeroInfoData(m_data.teamDefence.heroes[i].key);
                    var data = m_data.teamDefence.heroes[i];
                    newData.sortIdx = data.sortIdx;
                    m_data.teamDefence.heroes[i] = newData;
                }
            }

            SaveData();
        }

        public TournamentBatchData GetBatchData(bool _isAttack)
        {
            var team = _isAttack ? m_data.teamAttack : m_data.teamDefence;
            if (team.isActive == false)
            {
                team.Default();

                if (_isAttack == true)
                {
                    team.heroes.AddRange(TeamManager.instance.members.Select(x => x.Value.info).ToList());
                    team.treasure.AddRange(DataManager.stat.relic.dataTreasure.Where(x => x.isBatch == true).Select(x => x.key).ToList());

                    foreach (var member in TeamManager.instance.members)
                    {
                        var idxPosition = -1;
                        switch (member.Key)
                        {
                            case TeamPositionType.Front:
                                idxPosition = 1;
                                break;
                            case TeamPositionType.Top:
                                idxPosition = 3;
                                break;
                            case TeamPositionType.Bottom:
                                idxPosition = 5;
                                break;
                            case TeamPositionType.Back:
                                idxPosition = 7;
                                break;
                        }

                        var idxHeroInfo = team.heroes.FindIndex(x => x.skin == member.Value.info.skin);
                        var heroInfo = team.heroes[idxHeroInfo];
                        heroInfo.sortIdx = idxPosition;
                        team.heroes[idxHeroInfo] = heroInfo;
                    }

                    m_data.teamAttack = team;
                }
                else
                {
                    team.heroes.AddRange(m_data.teamAttack.heroes);
                    team.treasure.AddRange(m_data.teamAttack.treasure);

                    m_data.teamDefence = team;
                }

                SaveData();
            }

            TournamentBatchData result = new();
            result.Default();

            result.heroes.AddRange(team.heroes);
            result.treasure.AddRange(team.treasure);

            return result;
        }

        void SaveData()
        {
            PPWorker.Set(PlayerPrefsType.TOURNAMENT, m_data);
        }

        void SlotDayChange()
        {
            m_data.SetChangeDate();
            m_data.tick = Utils.GetUTC().Ticks;
            SaveData();

            m_dbRankData.Clear();
            m_ctsRefresh = m_ctsRefresh.ReleaseCTS();
        }

        public async UniTask<bool> ShowAdsAsync()
        {
            if (m_data.countAD > 0 && await AdsManager.instance.ShowAsync())
            {
                m_data.countPlay++;
                m_data.countAD--;
                SaveData();
                return true;
            }
            return false;
        }

        public async UniTask EnterBattleAsync()
        {
            if (m_data.countPlay == 0)
                return;

            m_data.countPlay--;
            SaveData();

            await UniTask.NextFrame();
        }

        public async UniTask RefreshListAsync()
        {
            if (m_data.countRefresh <= 0)
                return;

            await API_LoadBattleListAsync();

            m_data.countRefresh--;
            if (m_ctsRefresh == null)
            {
                m_data.tickRefresh = Utils.GetUTC().AddMinutes(c_refreshMinute).Ticks;
                TimerRefreshCountAsync().Forget();
            }
        }

        public async UniTask TimerRefreshCountAsync()
        {
            m_ctsRefresh = m_ctsRefresh.ReleaseCTS(true);
            var token = m_ctsRefresh.Token;

            SaveData();

            while (Utils.GetUTC().Ticks < m_data.tickRefresh)
                await UniTask.NextFrame(cancellationToken: token);

            m_data.countRefresh++;
            if (m_data.countRefresh < 3)
                TimerRefreshCountAsync().Forget();
            else
                m_ctsRefresh = m_ctsRefresh.ReleaseCTS();
        }

        public int GetPositionByClass(TournamentBatchData _batchData, HeroClassType _classType)
        {
            switch (_classType)
            {
                case HeroClassType.Champion:
                    {
                        int countChampion = _batchData.heroes.Count(x => x.sortIdx < 3);

                        if (countChampion == 0)
                            return 1;
                        else if (countChampion == 1)
                        {
                            if (_batchData.heroes.Any(x => x.sortIdx == 0))
                                return 2;
                            else
                                return 0;
                        }
                        else if (countChampion == 2)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                if (_batchData.heroes.Any(x => x.sortIdx == i) == false)
                                    return i;
                            }
                        }
                    }
                    return 4;
                case HeroClassType.Strategist:
                case HeroClassType.Archer:
                    {
                        int countBack = _batchData.heroes.Count(x => x.sortIdx >= 6);

                        if (countBack == 0)
                            return 7;
                        else if (countBack == 1)
                        {
                            if (_batchData.heroes.Any(x => x.sortIdx == 6))
                                return 8;
                            else
                                return 6;
                        }
                        else if (countBack == 2)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                int idx = 6 + i;
                                if (_batchData.heroes.Any(x => x.sortIdx == idx) == false)
                                    return idx;
                            }
                        }
                    }
                    return 4; // 뒤에 3명이면 앞 4번이 비어있을거야.
                default:
                    {
                        int countMiddle = _batchData.heroes.Count(x => 3 <= x.sortIdx && x.sortIdx < 6);

                        if (countMiddle == 0)
                            return 4;
                        else if (countMiddle == 1)
                        {
                            if (_batchData.heroes.Any(x => x.sortIdx == 3))
                                return 5;
                            else
                                return 3;
                        }
                        else if (countMiddle == 2)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                int idx = 3 + i;
                                if (_batchData.heroes.Any(x => x.sortIdx == idx) == false)
                                    return idx;
                            }
                        }
                    }
                    return 1;
            }
        }

        public void AddTreasure(string _key)
        {

        }
        public void DeleteTreasure(string _key)
        {

        }
    }

    public struct TournamentData
    {
        public GradeType grade;
        public int rank;
        public int point;

        public List<TournamentHistoryData> history;

        public TournamentBatchData teamAttack;
        public TournamentBatchData teamDefence;

        public int countPlay;
        public int countAD;
        public int countRefresh;

        public long tick;
        public long tickRefresh;

        public RankerUserData[] battleUserList;

        public void SetChangeDate()
        {
            countPlay = 2;
            countAD = 3;
            countRefresh = 3;

            tick = 0;
            tickRefresh = 0;
        }

        public bool isActive => tick > 0;
        public bool isFreeRefresh => countRefresh > 0;

        public TournamentBatchData GetTeam(bool _isAttackType)
            => _isAttackType ? teamAttack : teamDefence;
    }

    public struct TournamentBatchData
    {
        public int uid;

        public List<HeroInfoData> heroes;
        public List<string> treasure;

        public bool isActive => heroes != null;

        public void Default()
        {
            heroes = new();
            treasure = new();
        }
    }

    public struct TournamentHistoryData
    {
        public string nickname;
        public int power;
        public int indexProfile;
        public string skin;
        public bool isWin;
        public int rewardPoint;
        public bool isAttack;
    }
}
