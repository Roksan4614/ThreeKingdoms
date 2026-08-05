using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rev9.Tournament
{
    public partial class TournamentWorker
    {
        async UniTask LoadBattleListAsync()
        {
            await UniTask.NextFrame();

            m_data.battleUserList = new RankerUserData[5];

            // todo
            var randomNickname = Utils.GetRandomNicknameArray(5);
            for (int i = 0; i < 5; i++)
            {
                m_data.battleUserList[i] = new()
                {
                    nickname = randomNickname[i],
                    power = UnityEngine.Random.Range(1500, 2000),
                    point = UnityEngine.Random.Range(500, 1000) * (i + 1),
                };
            }

            m_data.battleUserList = m_data.battleUserList.SortByDescending(x => x.point);

            SaveData();
        }
    }
}