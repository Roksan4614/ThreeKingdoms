using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Rev9.Post
{
    public partial class PostWorker
    {
        async UniTask API_Load_PostData()
        {
            await UniTask.NextFrame();

            m_data = PPWorker.Get<PostData>(c_key);
            //if (m_data == null)
            {
                m_data = new();

                m_data.posts.Add(new()
                {
                    title = "아침 접속 보상",
                    index = 0,
                    content = "아침 접속 보상입니다. 즐거운 하루 보내세요.",
                    rewards = new()
                    {
                        TableManager.item.GetItemData(ItemType.gold, 100),
                        TableManager.item.GetItemData(ItemType.rice, 100),
                    }
                });
                m_data.posts.Add(new()
                {
                    title = "시간 테스트 용",
                    index = 2,
                    content = "시간 테스트용. 보상 없음.\n줄바꿈 테스트",
                    rewards = new()
                    {
                    },
                    tick_end = Utils.GetUTC().AddSeconds(20).Ticks
                });
                m_data.posts.Add(new()
                {
                    title = "그냥 접속 보상",
                    index = 3,
                    content = "시간석 영혼석 같은 거 테스트",
                    rewards = new()
                    {
                        TableManager.item.GetItemData(ItemType.public_soul_stone, 10),
                        TableManager.item.GetItemData(ItemType.dedicated_soul_stone, 100, "CaoCao"),
                        TableManager.item.GetItemData(ItemType.time_stone, 100000),
                        TableManager.item.GetItemData(ItemType.gold, 100),
                        TableManager.item.GetItemData(ItemType.rice, 100),
                    },
                    tick_end = Utils.GetUTC().AddHours(26).Ticks
                });

                SaveData();
            }
        }

        public async UniTask<bool> API_ReceivePost(params PostInfoData[] _postData)
        {
            await UniTask.NextFrame();

            bool isSuccessed = false;
            List<ItemData> rewards = new();

            GetData_RefreshTimer(false);

            if (_postData.Length == 0)
                _postData = m_data.posts.ToArray();

            foreach (var post in _postData)
            {
                var idx = m_data.posts.FindIndex(x => x.index == post.index);
                if (idx == -1)
                    continue;

                var data = m_data.posts[idx];

                if (data.isReceiveReward == false)
                {
                    rewards.AddRange(data.rewards);
                    data.isReceiveReward = true;
                    isSuccessed = true;
                }
            }

            SaveData();

            if (rewards.Count > 0)
                RewardWorker.OpenRewardPopup(rewards.ToArray());
            else
                PopupManager.instance.AlertShow("받을_보상이_없습니다.");

            return isSuccessed;
        }

        public async UniTask<bool> API_DeletePost(params PostInfoData[] _postData)
        {
            await UniTask.NextFrame();

            bool isSuccessed = false;

            if (_postData.Length == 0)
                _postData = m_data.posts.ToArray();

            foreach (var post in _postData)
            {
                var idx = m_data.posts.FindIndex(x => x.index == post.index);
                if (idx == -1)
                    continue;

                var data = m_data.posts[idx];

                if (data.rewards.Count == 0 || data.isReceiveReward == true)
                {
                    m_data.posts.RemoveAt(idx);
                    isSuccessed = true;
                }
            }

            PopupManager.instance.AlertShow(isSuccessed ? "삭제_완료" : "삭제할_우편이_없습니다.");

            SaveData();

            return isSuccessed;
        }
    }
}