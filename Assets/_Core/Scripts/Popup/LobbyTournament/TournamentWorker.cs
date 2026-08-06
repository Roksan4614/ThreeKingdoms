using Cysharp.Threading.Tasks;
using System;
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
                //PPWorker.DeleteKey(PlayerPrefsType.TOURNAMENT);
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

        public TournamentBatchData GetBatchData(bool _isAttack)
        {
            var team = _isAttack ? m_data.teamAttack : m_data.teamDefence;
            if (team.isActive == false)
            {
                if (_isAttack == true)
                {
                    team.skinKey = TeamManager.instance.members.Select(x => x.Value.info.skin).ToArray();
                    team.position = new int[team.skinKey.Length];
                    team.treasure = DataManager.stat.relic.dataTreasure.Where(x => x.isBatch == true).Select(x => x.key).ToArray();

                    int idx = 0;
                    foreach (var member in TeamManager.instance.members)
                    {
                        switch (member.Key)
                        {
                            case TeamPositionType.Front:
                                team.position[idx] = 1;
                                break;
                            case TeamPositionType.Top:
                                team.position[idx] = 3;
                                break;
                            case TeamPositionType.Bottom:
                                team.position[idx] = 5;
                                break;
                            case TeamPositionType.Back:
                                team.position[idx] = 7;
                                break;
                        }
                        idx++;
                    }

                    m_data.teamAttack = team;
                }
                else
                {
                    for (int i = 0; i < m_data.teamAttack.skinKey.Length; i++)
                    {
                        team.skinKey[i] = m_data.teamAttack.skinKey[i];
                        team.position[i] = m_data.teamAttack.position[i];
                    }

                    for (int i = 0; i < m_data.teamAttack.treasure.Length; i++)
                        team.treasure[i] = m_data.teamAttack.treasure[i];
                }

                SaveData();
            }

            return team;
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

        bool isRunnig_TimerRefresh => m_ctsRefresh != null;
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
    }

    public struct TournamentBatchData
    {
        public string[] skinKey;
        public int[] position;
        public string[] treasure;

        public bool isActive => skinKey != null;
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
