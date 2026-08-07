using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rev9.Tournament
{
    public partial class TournamentWorker
    {
        async UniTask API_LoadBattleListAsync()
        {
            await UniTask.NextFrame();

            m_data.battleUserList = new RankerUserData[4];

            // todo
            var randomNickname = Utils.GetRandomNicknameArray(4);
            var heroes = TableManager.hero.GetHeroList().Where(x => x.regionType == RegionType.SHU).ToList();
            for (int i = 0; i < 4; i++)
            {
                m_data.battleUserList[i] = new()
                {
                    uid = i,
                    nickname = randomNickname[i],
                    power = UnityEngine.Random.Range(1500, 2000),
                    point = UnityEngine.Random.Range(500, 1000) * (i + 1),
                    skin = heroes.RandomFirst().key
                };
            }

            m_data.battleUserList = m_data.battleUserList.SortByDescending(x => x.point);

            SaveData();

            m_dbRankUserInfoData.Clear();
        }

        Dictionary<int, TournamentBatchData> m_dbRankUserInfoData = new();
        public async UniTask<TournamentBatchData> API_LoadUserInfoData(int _uid)
        {
            if (m_dbRankUserInfoData.ContainsKey(_uid))
                return m_dbRankUserInfoData[_uid];

            await UniTask.NextFrame();

            TournamentBatchData result = new()
            {
                uid = _uid,
                heroInfo = new HeroInfoData[4],
                position = new int[4],
                treasure = new string[3],
            };

            var heroes = TableManager.hero.GetHeroList().Shuffle().Take(4).ToList();
            var arrayPosition = new int[4];

            var countChampion = heroes.Count(x => x.classType == HeroClassType.Champion);
            var countBack = heroes.Count(x => x.classType == HeroClassType.Strategist || x.classType == HeroClassType.Archer);
            var countMiddle = 4 - countChampion - countBack;

            int idxFront = 0, idxMiddle = 0, idxBack = 0;
            for (int i = 0; i < 4; i++)
            {
                result.heroInfo[i] = new HeroInfoData(heroes[i].key,
                    GradeType.Normal + UnityEngine.Random.Range(0, (int)GradeType.MAX),
                    _skin: heroes[i].key, _enchantLevel: UnityEngine.Random.Range(1, 17), _isMine: false);

                if (heroes[i].classType == HeroClassType.Champion)
                {
                    if (countChampion == 1)
                        result.position[i] = 1;
                    else if (countChampion == 2)
                        result.position[i] = idxFront * 2;
                    else
                        result.position[i] = idxFront < 3 ? idxFront : 4;

                    idxFront++;
                }
                else if (heroes[i].classType == HeroClassType.Archer ||
                    heroes[i].classType == HeroClassType.Strategist)
                {
                    if (countBack == 1)
                        result.position[i] = 7;
                    else if (countBack == 2)
                        result.position[i] = idxBack == 0 ? 6 : 8;
                    else if (countBack == 3)
                        result.position[i] = 6 + idxBack;
                    else
                        result.position[i] = idxBack == 0 ? 4 : 5 + idxBack;
                    idxBack++;
                }
                else
                {
                    if (countMiddle == 1)
                        result.position[i] = 4;
                    else if (countMiddle == 2)
                        result.position[i] = idxMiddle == 0 ? 3 : 5;
                    else if (countMiddle == 3)
                        result.position[i] = 3 + idxMiddle;
                    else
                        result.position[i] = idxMiddle == 0 ? 1 : 2 + idxMiddle;
                    idxMiddle++;
                }

                if (i < result.treasure.Length)
                    result.treasure[i] = "Treasure_" + (i + 1);
            }

            m_dbRankUserInfoData.Add(_uid, result);

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
                    result.my = userData;

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

            m_dbRankData.Add(_tabType, result);
            return result;
        }
    }
}