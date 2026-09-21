using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Rev9.Pass
{
    public class Data_Pass
    {
        const string c_key = "pp_pass";

        PassData m_data;

        public int level => m_data.level;
        public int exp => m_data.exp;
        public bool isPaid => m_data.isPaid;

        public List<PassQuestData> quests => m_data.quests;

        public async UniTask InitializeAsync()
        {
            if (m_data == null)
            {
                await UniTask.NextFrame();
                m_data = PPWorker.Get<PassData>(c_key);
                if (m_data == null)
                {
                    RefreshSeason();

                    //test
                    m_data.level = 11;
                    m_data.exp = 100;

                    await API_RefreshQuest();
                }
            }
        }

        public void RefreshSeason()
        {
            m_data = new();
            m_data.level = 1;
            m_data.tickRefresh = Utils.GetUTC().Ticks;
            SaveData();
        }

        void SaveData()
            => PPWorker.Set(c_key, m_data);

        public bool IsReceiveReward(int _level, bool _isPaid)
        {
            var receiveData = _isPaid ? m_data.receiveLevel_Paid : m_data.receiveLevel;

            return receiveData.Contains(_level);
        }

        public async UniTask API_RefreshQuest()
        {
            await UniTask.NextFrame();

            // 더미데이터만 넣자

            int idx = 1;
            long ticks = Utils.GetUTC().Ticks;
            // 일일 2개랑 특사 1 넣자
            {
                for (int i = 0; i < 3; i++)
                {
                    m_data.quests.Add(new()
                    {
                        idx = idx++,
                        tick = ticks,
                        isPaid = i == 2,
                        data = TableManager.passQuest.GetRandomQuest(false)
                    });
                }
            }
            // 시즌도 넣어주자

            {
                for (int i = 0; i < 12; i++)
                {
                    m_data.quests.Add(new()
                    {
                        idx = idx++,
                        tick = ticks,
                        isPaid = (i + 1) % 3 == 0,
                        data = TableManager.passQuest.GetRandomQuest(true)
                    });
                }
            }

            SaveData();
        }

        public async UniTask API_QuestComplete(int _idx)
        {
            await UniTask.NextFrame();

            var index = m_data.quests.FindIndex(x => x.idx == _idx);
            m_data.quests.RemoveAt(index);
            SaveData();
        }

        public async UniTask<bool> API_ReceiveReward(PopupPass_Reward_Slot _slot, bool _isPaid)
        {
            if (_slot.rewardData.level > m_data.level)
                return false;

            await UniTask.NextFrame();
            var level = _slot.rewardData.level;

            if (_isPaid && m_data.isPaid == false)
            {
                PopupManager.instance.AlertShow_Table("PASS_CAN_AFTER_PAID");
                return false;
            }

            var receiveData = _isPaid ? m_data.receiveLevel_Paid : m_data.receiveLevel;
            if (receiveData.Contains(level))
            {
                //PopupManager.instance.AlertShow("이미_보상을_받았습니다.");
                return false;
            }

            level = m_data.level;
            List<ItemData> resultRewards = new();

            while (level > 0)
            {
                if (m_data.receiveLevel.Contains(level) == false)
                {
                    resultRewards.Add(TableManager.passReward.GetRewardItem(level, false));
                    m_data.receiveLevel.Add(level);
                }

                if (m_data.isPaid && m_data.receiveLevel_Paid.Contains(level) == false)
                {
                    resultRewards.Add(TableManager.passReward.GetRewardItem(level, true));
                    m_data.receiveLevel_Paid.Add(level);
                }

                level--;
            }

            if (resultRewards.Count == 1)
                RewardWorker.instance.Run(_slot.transform.position, _itemData: resultRewards[0]);
            else
                RewardWorker.OpenRewardPopup(resultRewards.ToArray());

            SaveData();

            return true;
            //return TableManager.passReward.GetRewardItem(_level, _isPaid);
        }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PassData
    {
        [JsonProperty] public int level;
        [JsonProperty] public int exp;
        [JsonProperty] public long tickEndPaid;
        [JsonProperty] public long tickRefresh;

        [JsonProperty] public List<int> receiveLevel = new();
        [JsonProperty] public List<int> receiveLevel_Paid = new();

        [JsonProperty] public List<PassQuestData> quests = new();

        public bool isPaid => tickEndPaid > Utils.GetUTC().Ticks;
        public System.DateTime dtEndPaid => Utils.GetDateTime(tickEndPaid);
        public System.DateTime dtRefresh => Utils.GetDateTime(tickRefresh);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PassQuestData
    {
        [JsonProperty] public int idx;
        [JsonProperty] public long tick;
        [JsonProperty] public bool isPaid;
        [JsonProperty] public TablePassQuestData data;

        public bool isComplete
            => TableManager.passQuest.GetCount(data) <= data.count;

        public System.DateTime dt => Utils.GetDateTime(tick);

        public bool isDaily
            => data.type == 0;
    }
}