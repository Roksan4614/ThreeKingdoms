using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Rev9.Post
{
    public class PostWorker : MonoSingleton<PostWorker>
    {
        public static bool isReady => instance.m_data != null;
        public static bool isRedDot => instance.IsRedDot();
        public static IReadOnlyList<PostInfoData> data => instance.GetData_RefreshTimer();

        PostData m_data;
        const string c_key = "pp_post";

        private void Start()
        {
            InitializeAsync().Forget();
        }

        async UniTask InitializeAsync()
        {
            m_data = PPWorker.Get<PostData>(c_key);
            //if (m_data == null)
            {
                m_data = new();

                m_data.posts.Add(new()
                {
                    title = "아침 접속 보상",
                    index = 0,
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
                    rewards = new()
                    {
                        TableManager.item.GetItemData(ItemType.dedicated_soul_stone, 10, "CaoCao"),
                        TableManager.item.GetItemData(ItemType.public_soul_stone, 100),
                    },
                    tick_end = Utils.GetUTC().AddSeconds(20).Ticks
                });
                m_data.posts.Add(new()
                {
                    title = "그냥 접속 보상",
                    index = 3,
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

        void SaveData()
            => PPWorker.Set(c_key, m_data);

        public PostInfoData GetPostData(int _idx)
            => m_data.posts.Find(x => x.index == _idx);

        public void SetRead(int _idx)
        {
            var postData = GetPostData(_idx);
            if (postData == null)
                return;

            postData.isRead = true;
            SaveData();
        }

        IReadOnlyList<PostInfoData> GetData_RefreshTimer(bool _isSave = true)
        {
            int i = 0;
            var tickNow = Utils.GetUTC().Ticks;
            bool isRemove = false;
            while (i < m_data.posts.Count)
            {
                var data = m_data.posts[i];

                if (data.tick_end > 0 && tickNow >= data.tick_end)
                {
                    m_data.posts.RemoveAt(i);
                    isRemove = true;
                }
                else
                    i++;
            }

            if (_isSave == true && isRemove == true)
                SaveData();

            return m_data.posts;
        }

        public void SetRemoveAll()
        {
            int i = 0;
            while (i < m_data.posts.Count)
            {
                if (SetRemove(m_data.posts[i].index, false) == false)
                    i++;
            }

            SaveData();
        }

        public bool SetRemove(int _idx, bool _isSave = true)
        {
            var data = GetPostData(_idx);
            if (data == null || (data.isReceiveReward == false && data.rewards.Count > 0))
                return false;

            m_data.posts.Remove(m_data.posts.Find(x => x.index == _idx));

            if (_isSave)
                SaveData();

            return true;
        }

        public IReadOnlyList<ItemData> GetRewardAll()
        {
            List<ItemData> result = new();
            var datas = GetData_RefreshTimer();

            foreach (var data in datas)
            {
                var rewards = GetReward(data.index);

                foreach (var reward in rewards)
                {
                    var resultData = result.Find(x => x.EqaulsItemData(reward));
                    if (resultData == null)
                        result.Add(reward.DeepClone());
                    else
                        resultData.count += reward.count;
                }
            }

            return result;
        }

        public IReadOnlyList<ItemData> GetReward(int _index)
        {
            var data = GetPostData(_index);
            return data?.rewards ?? new();
        }

        public void SetReddotRefresh_OpenPost()
        {
            var datas = GetData_RefreshTimer(false);

            m_data.readIndex.Clear();
            foreach (var d in datas)
                m_data.readIndex.Add(d.index);

            SaveData();
        }

        bool IsRedDot()
        {
            var datas = GetData_RefreshTimer(false);

            if (datas.Count != m_data.readIndex.Count)
                return true;

            foreach (var d in datas)
                if (m_data.readIndex.Contains(d.index) == false)
                    return true;

            return false;
        }
    }

    public class PostData
    {
        public List<int> readIndex = new();
        public List<PostInfoData> posts = new();
    }

    public class PostInfoData
    {
        public int index;
        public string title;
        public string content;
        public List<ItemData> rewards = new();
        public long tick_end;
        public bool isRead;
        public bool isReceiveReward;
    }
}