using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Rev9.Tournament
{
    public partial class TournamentWorker
    {
        async UniTask API_LoadBattleListAsync()
        {
            await UniTask.NextFrame();

            m_data.battleUserList = new TournamentRankerUserData[4];

            m_dbRankUserInfoData.Clear();

            // todo
            var randomNickname = Utils.GetRandomNicknameArray(4);
            var heroes = TableManager.hero.GetHeroList().Where(x => x.regionType == RegionType.SHU).ToList();
            for (int i = 0; i < 4; i++)
            {
                int uid = DataManager.userInfo.uid + i;
                var batchData = await API_LoadUserInfoData(uid);
                m_data.battleUserList[i] = new()
                {
                    info = new RankerUserData()
                    {
                        uid = uid,
                        nickname = randomNickname[i],
                        power = batchData.totalPower,
                        point = UnityEngine.Random.Range(500, 1000) * (i + 1),
                        skin = heroes.RandomFirst().key,
                    },
                    batchData = batchData
                };
            }

            m_data.battleUserList = m_data.battleUserList.SortByDescending(x => x.info.point);

            SaveData();
        }

        Dictionary<int, TournamentBatchData> m_dbRankUserInfoData = new();

        public async UniTask<TournamentBatchData> API_LoadUserInfoData(int _uid)
        {
            if (m_dbRankUserInfoData.ContainsKey(_uid))
                return m_dbRankUserInfoData[_uid];

            TournamentBatchData result = new() { uid = _uid };
            result.Default();

            var heroes = TableManager.hero.GetHeroList().Shuffle().Take(4).ToList();
            var arrayPosition = new int[4];

            var countChampion = heroes.Count(x => x.classType == HeroClassType.Champion);
            var countBack = heroes.Count(x => x.classType == HeroClassType.Strategist || x.classType == HeroClassType.Archer);
            var countMiddle = 4 - countChampion - countBack;

            int idxFront = 0, idxMiddle = 0, idxBack = 0;
            for (int i = 0; i < heroes.Count; i++)
            {
                var heroInfo = new HeroInfoData(heroes[i].key,
                    GradeType.Normal + UnityEngine.Random.Range(0, (int)GradeType.MAX),
                    _skin: heroes[i].key, _enchantLevel: UnityEngine.Random.Range(1, 17), _isMine: false);

                if (heroes[i].classType == HeroClassType.Champion)
                {
                    if (countChampion == 1)
                        heroInfo.sortIdx = 1;
                    else if (countChampion == 2)
                        heroInfo.sortIdx = idxFront * 2;
                    else
                        heroInfo.sortIdx = idxFront < 3 ? idxFront : 4;

                    idxFront++;
                }
                else if (heroes[i].classType == HeroClassType.Archer ||
                    heroes[i].classType == HeroClassType.Strategist)
                {
                    if (countBack == 1)
                        heroInfo.sortIdx = 7;
                    else if (countBack == 2)
                        heroInfo.sortIdx = idxBack == 0 ? 6 : 8;
                    else if (countBack == 3)
                        heroInfo.sortIdx = 6 + idxBack;
                    else
                        heroInfo.sortIdx = idxBack == 0 ? 4 : 5 + idxBack;
                    idxBack++;
                }
                else
                {
                    if (countMiddle == 1)
                        heroInfo.sortIdx = 4;
                    else if (countMiddle == 2)
                        heroInfo.sortIdx = idxMiddle == 0 ? 3 : 5;
                    else if (countMiddle == 3)
                        heroInfo.sortIdx = 3 + idxMiddle;
                    else
                        heroInfo.sortIdx = idxMiddle == 0 ? 1 : 2 + idxMiddle;
                    idxMiddle++;
                }

                result.heroes.Add(heroInfo);
            }

            result.treasure = TableManager.treasure.list.Where(x => x.isActive).ToList().Shuffle().Take(3)
                .Select(x => new Data_Stat_Relic.TreasureBatchData()
                {
                    key = x.key,
                    isBatch = true,
                    tickBatch = System.DateTime.UtcNow.Ticks
                }).ToList();

            m_dbRankUserInfoData.Add(_uid, result);

            await UniTask.NextFrame();

            return result;
        }

        Dictionary<PopupLobbyBossRaid_PopupRanking.TabType, RankerData> m_dbRankData = new();
        public async UniTask<RankerData> API_LoadRankerData(PopupLobbyBossRaid_PopupRanking.TabType _tabType)
        {
            if (m_dbRankData.ContainsKey(_tabType))
                return m_dbRankData[_tabType];

            await UniTask.NextFrame();

            RankerData result = new();
            result.ranker = new();

            for (int i = 0; i < 50; i++)
            {
                bool isMine = i == 21;
                RankerUserData userData = new();

                userData.uid = isMine ? DataManager.userInfo.uid : DataManager.userInfo.uid + i + 1;
                userData.prevRank = i + 1;
                userData.nickname = isMine ? DataManager.userInfo.nickname.ToString() : $"Nickname_{userData.prevRank:00#}";
                userData.point = _tabType == PopupLobbyBossRaid_PopupRanking.TabType.Tutorial_Winning ?
                    UnityEngine.Random.Range(10, 100) :
                    UnityEngine.Random.Range(100, 10000);
                userData.power = UnityEngine.Random.Range(1000, 3000);
                userData.skin = TableManager.hero.GetHeroList().RandomFirst().key;

                if (isMine)
                {
                    userData.indexProfile = DataManager.userInfo.profileIdx;
                    userData.skin = DataManager.userInfo.profileSkin;
                    result.my = userData;
                }

                result.ranker.Add(userData);
            }

            result.ranker = result.ranker.SortByDescending(x => x.point);

            for (int i = 0; i < result.ranker.Count; i++)
            {
                var data = result.ranker[i];
                data.rank = i + 1;
                result.ranker[i] = data;

                if (data.uid == result.my.uid)
                    result.my.rank = data.rank;
            }

            m_data.rankData = result.my;

            m_dbRankData.Add(_tabType, result);
            return result;
        }

        public TournamentBatchData ChangePosition(TournamentBatchData _batchData, int _prev, int _next)
        {
            int idxPrev = -1, idxNext = -1;
            for (int i = 0; i < _batchData.heroes.Count; i++)
            {
                if (_batchData.heroes[i].isActive == false)
                    continue;

                if (_batchData.heroes[i].sortIdx == _prev)
                    idxPrev = i;
                else if (_batchData.heroes[i].sortIdx == _next)
                    idxNext = i;

                if (idxNext > -1 && idxPrev > -1)
                    break;
            }

            var prevHero = _batchData.heroes[idxPrev];
            prevHero.sortIdx = _next;
            _batchData.heroes[idxPrev] = prevHero;

            if (idxNext > -1 && idxPrev > -1)
            {
                var nextHero = _batchData.heroes[idxNext];

                nextHero.sortIdx = _prev;

                _batchData.heroes[idxNext] = nextHero;
            }

            _batchData.heroes = _batchData.heroes.SortBy(x => x.sortIdx);

            return _batchData;
        }

        public async UniTask API_UpdateTeamData(bool _isAttackType, TournamentBatchData _batchData, UnityAction _callback = null)
        {
            await UniTask.NextFrame();

#if SERVICE_DEV
            //var msg = Newtonsoft.Json.JsonConvert.SerializeObject(_batchData.heroes.Select(x => x.key).ToList());
            //IngameLog.Add($"API_UpdateTeamData: {(_isAttackType ? "ATTACK" : "DEFENCE")}: {msg}");
#endif
            var team = m_data.GetTeam(_isAttackType);

            team.heroes.Clear();
            team.heroes.AddRange(_batchData.heroes);
            SaveData();

            _callback?.Invoke();
        }


        string c_keyHistory = "PP_TOURNAMENT_HISTORY";
        public async UniTask API_AddHistoryData(bool _isAttackType, bool _isWin, TournamentRankerUserData _userData, int _idxRevenge = -1)
        {
            if (m_history == null)
                await API_LoadHistoryData();

            TournamentHistoryData historyData = new()
            {
                index = m_history.Count == 0 ? 1 : m_history[m_history.Count - 1].index + 1,
                uid = _userData.info.uid,
                nickname = _userData.info.nickname,
                batchData = _userData.batchData,
                indexProfile = _userData.info.indexProfile,
                skin = _userData.info.skin,
                isAttack = _isAttackType,
                isWin = _isWin,
                tick = Utils.GetUTC().Ticks
            };

            var userBatchData = m_data.GetTeam(_isAttackType);

            // 방어에 실패한 경우, 복수 준비하자

            //포인트 계산
            historyData.resultPoint = 100 * UnityEngine.Random.Range(1, 4);
            if (historyData.isWin == false)
                historyData.resultPoint = (int)(historyData.resultPoint * UnityEngine.Random.Range(-0.4f, -0.8f));

            m_history.Add(historyData);

            if (_idxRevenge > -1)
            {
                var idx = m_history.FindIndex(x => x.index == _idxRevenge);
                if (idx > -1)
                {
                    var h = m_history[idx];
                    h.tick = 0;
                    m_history[idx] = h;
                }
            }

            while (m_history.Count > 100)
                m_history.RemoveAt(0);

            PPWorker.Set(c_keyHistory, m_history);

            await UniTask.NextFrame();
        }

        public async UniTask<List<TournamentHistoryData>> API_LoadHistoryData()
        {
            await UniTask.NextFrame();

            if (m_history == null)
            {
                m_history = PPWorker.Get<List<TournamentHistoryData>>(c_keyHistory);

                if (m_history == null)
                {
                    m_history = new();
                    PPWorker.Set(c_keyHistory, m_history);
                }
            }

            return m_history;
        }

        public async UniTask<RankerUserData> API_Result()
        {
            bool isWin = TournamentHeroInfoManager.instance.IsWin();

            RankerUserData result = m_data.rankData;

            result.rank += Mathf.Max(1, (isWin ? 1 : -1) * UnityEngine.Random.Range(3, 10));
            result.point += GetResultPoint(isWin);

            await UniTask.NextFrame();

            return result;
        }
    }
}