using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Rev9.Tournament
{
    public partial class TournamentWorker : Singleton<TournamentWorker>
    {
        TournamentData m_data;
        public TournamentData data => m_data;

        const int c_refreshMinute = 5;

        CancellationTokenSource m_ctsRefresh;

        public async UniTask InitailizeAsync()
        {
            PPWorker.DeleteKey(PlayerPrefsType.TOURNAMENT);
            m_data = PPWorker.Get<TournamentData>(PlayerPrefsType.TOURNAMENT);

            // 날짜 확인
            var dtBattle = Utils.GetDateTime(m_data.tickBattle);
            if (dtBattle.Date < Utils.GetUTC().Date)
            {
                SlotDayChange();
                await LoadBattleListAsync();
            }

            // 갱신 업데이트
            var dtRefresh = Utils.GetDateTime(m_data.tickRefresh);
            m_data.countRefresh = Mathf.Min(3, m_data.countRefresh + (int)((Utils.GetUTC() - dtRefresh).TotalMinutes / 5));
            if (m_data.countRefresh < 3)
                TimerRefreshCountAsync().Forget();

            Signal.instance.DayChange.connect = SlotDayChange;
        }

        public TournamentHeroData[] GetHeroes(bool _isAttack)
        {
            var team = _isAttack ? m_data.teamAttack : m_data.teamDefence;
            if (team == null)
            {
                team = new();

                if (_isAttack == true)
                {
                    foreach (var member in TeamManager.instance.members)
                    {
                        var heroData = new TournamentHeroData();
                        heroData.skinKey = member.Value.info.skin;

                        switch (member.Key)
                        {
                            case TeamPositionType.Front:
                                heroData.position = 1;
                                break;
                            case TeamPositionType.Top:
                                heroData.position = 3;
                                break;
                            case TeamPositionType.Bottom:
                                heroData.position = 5;
                                break;
                            case TeamPositionType.Back:
                                heroData.position = 7;
                                break;
                        }

                        team.Add(heroData);
                    }

                    m_data.teamAttack = team;
                    m_data.treasureAttack = DataManager.stat.relic.dataTreasure.Where(x => x.isBatch == true).Select(x => x.key).ToList();
                }
                else
                {
                    team.AddRange(m_data.teamAttack);
                    m_data.teamDefence = team;
                    m_data.treasureDefence.AddRange(m_data.treasureAttack);
                }

                SaveData();
            }

            return team.ToArray();
        }

        void SaveData()
        {
            PPWorker.Set(PlayerPrefsType.TOURNAMENT, m_data);
        }

        void SlotDayChange()
        {
            m_data.SetChangeDate();
            SaveData();

            m_ctsRefresh = m_ctsRefresh.ReleaseCTS();
        }

        public async UniTask EnterBattleAsync()
        {
            if (m_data.countPlay == 0)
                return;

            m_data.countPlay--;
            m_data.tickBattle = Utils.GetUTC().Ticks;
            SaveData();

            await UniTask.NextFrame();
        }

        public async UniTask RefreshListAsync()
        {
            if (m_data.countRefresh <= 0)
                return;

            await LoadBattleListAsync();

            m_data.countRefresh--;
            TimerRefreshCountAsync().Forget();
        }

        public async UniTask TimerRefreshCountAsync()
        {
            m_ctsRefresh = m_ctsRefresh.ReleaseCTS(true);
            var token = m_ctsRefresh.Token;

            m_data.tickRefresh = Utils.GetUTC().AddMinutes(c_refreshMinute).Ticks;
            SaveData();

            while (Utils.GetUTC().Ticks < m_data.tickRefresh)
                await UniTask.NextFrame(cancellationToken: token);

            m_data.countRefresh++;
            if (m_data.countRefresh < 3)
                TimerRefreshCountAsync().Forget();
        }

        bool isRunnig_TimerRefresh => m_ctsRefresh != null;
    }

    public struct TournamentData
    {
        public int rank;
        public int point;

        public List<TournamentHistoryData> history;

        public List<TournamentHeroData> teamAttack;
        public List<TournamentHeroData> teamDefence;

        public List<string> treasureAttack;
        public List<string> treasureDefence;

        public int countPlay;
        public int countAD;
        public int countRefresh;

        public long tickBattle;
        public long tickRefresh;

        public RankerUserData[] battleUserList;

        public void SetChangeDate()
        {
            countPlay = 2;
            countAD = 3;
            countRefresh = 3;

            tickBattle = 0;
            tickRefresh = 0;
        }
    }

    public struct TournamentHeroData
    {
        public string skinKey;
        public int position;
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
