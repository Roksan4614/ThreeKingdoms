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
                    nickname = randomNickname[i],
                    power = UnityEngine.Random.Range(1500, 2000),
                    point = UnityEngine.Random.Range(500, 1000) * (i + 1),
                    skin = heroes.RandomFirst().key
                };
            }

            m_data.battleUserList = m_data.battleUserList.SortByDescending(x => x.point);

            SaveData();
        }
    }
}