using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

namespace Rev9.ContentsMarket
{
    public class ContentsMarketWorker
    {
        static ContentsMarketWorker m_instance;
        public static ContentsMarketWorker instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new();
                return m_instance;
            }
        }

        Dictionary<ContentsMarketTabType, List<ContentsMarketProductData>> m_db;

        public static void Release()
        {
            if (m_instance != null)
                m_instance = null;
        }

        public async UniTask InitializeAsync()
        {
            await UniTask.NextFrame();

            if (m_db == null)
            {
                m_db = new();
                for (ContentsMarketTabType i = 0; i < ContentsMarketTabType.MAX; i++)
                {
                    List<ContentsMarketProductData> lstData = new();

                    lstData.Add(new()
                    {
                        key = "rice",
                        cost = 1000,
                        count = 100,
                        countMax = 5
                    });

                    lstData.Add(new()
                    {
                        key = "gold",
                        cost = 1000,
                        count = 100,
                        countMax = 5
                    });

                    lstData.Add(new()
                    {
                        key = "rice",
                        peroidType = PeroidType.Week,
                        cost = 7000,
                        count = 1000,
                        countMax = 3
                    });

                    lstData.Add(new()
                    {
                        key = "gold",
                        peroidType = PeroidType.Week,
                        cost = 7000,
                        count = 1000,
                        countMax = 3
                    });

                    if (i == ContentsMarketTabType.Daily)
                    {
                        lstData.Add(new()
                        {
                            key = "time_stone",
                            cost = 2000,
                            count = 10,
                            countMax = 3
                        });
                        lstData.Add(new()
                        {
                            key = $"dedicated_soul_stone_{Utils.ToSnakeCase(CharacterName.LiuBei.ToString())}",
                            cost = 2000,
                            count = 10,
                            countMax = 3
                        });
                    }
                    else if (i == ContentsMarketTabType.Tournament)
                    {
                        lstData.Add(new()
                        {
                            key = "point_tournament",
                            peroidType = PeroidType.Week,
                            cost = 2500,
                            count = 10,
                            countMax = 3
                        });

                        lstData.Add(new()
                        {
                            key = "public_soul_stone",
                            peroidType = PeroidType.Week,
                            cost = 2500,
                            count = 10,
                            countMax = 3
                        });
                    }
                    else if (i == ContentsMarketTabType.Raid)
                    {

                        lstData.Add(new()
                        {
                            key = "public_soul_stone",
                            peroidType = PeroidType.Week,
                            cost = 3500,
                            count = 10,
                            countMax = 3
                        });
                        lstData.Add(new()
                        {
                            key = $"dedicated_soul_stone_{Utils.ToSnakeCase(CharacterName.LiuBei.ToString())}",
                            peroidType = PeroidType.Season,
                            cost = 4500,
                            count = 5,
                            countMax = 3
                        });

                        lstData.Add(new()
                        {
                            key = $"dedicated_soul_stone_{Utils.ToSnakeCase(CharacterName.CaoCao.ToString())}",
                            peroidType = PeroidType.Season,
                            cost = 4500,
                            count = 5,
                            countMax = 3
                        });
                        lstData.Add(new()
                        {
                            key = $"dedicated_soul_stone_{Utils.ToSnakeCase(CharacterName.SunQuan.ToString())}",
                            peroidType = PeroidType.Season,
                            cost = 4500,
                            count = 5,
                            countMax = 3
                        });
                    }

                    for (int j = 0; j < lstData.Count; j++)
                    {
                        var d = lstData[j];
                        d.idx = j;
                        d.costType = i == ContentsMarketTabType.Tournament ? ItemDetailType.PointTournament : i == ContentsMarketTabType.Raid ? ItemDetailType.PointRaid : ItemDetailType.Gold;
                        lstData[j] = d;
                    }

                    m_db.Add(i, lstData);
                }
            }
        }
        public List<ContentsMarketProductData> GetProducts(ContentsMarketTabType _tabType)
            => m_db[_tabType];

        Dictionary<ContentsMarketTabType, string> m_dbMessage = new();
        public string GetMessage(ContentsMarketTabType _tabType)
            => m_dbMessage.ContainsKey(_tabType) ? m_dbMessage[_tabType] : null;
        public void SetMessage(ContentsMarketTabType _tabType, string _message)
        {
            if (m_dbMessage.ContainsKey(_tabType))
                m_dbMessage[_tabType] = _message;
            else
                m_dbMessage.Add(_tabType, _message);
        }

        public async UniTask<bool> API_ProductBuy(ContentsMarketTabType _tabType, ContentsMarketProductData _productData, int _countProduct)
        {
            await UniTask.NextFrame();

            var db = m_db[_tabType];
            int idx = db.FindIndex(x => x.idx == _productData.idx);

            var data = db[idx];
            data.countBuy += _countProduct;
            db[idx] = data;

            return true;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ContentsMarketProductData
    {
        [JsonProperty] public int idx;
        [JsonProperty] public string key;
        [JsonProperty] public int count;
        [JsonProperty] public string value;

        [JsonProperty] public int cost;

        [JsonProperty] public int countMax;
        [JsonProperty] public int countBuy;

        PeroidType? peroid_type;
        public PeroidType peroidType
        {
            get => peroid_type ?? PeroidType.Daily;
            set => peroid_type = value;
        }

        ItemDetailType? cost_type;
        public ItemDetailType costType
        {
            get => cost_type ?? ItemDetailType.Gold;
            set => cost_type = value;
        }

        public bool isLimit => countMax > 0;
        public int remainCount => countMax - countBuy;
        public string strRemainCount => $"{remainCount}/{countMax}";

        ItemData m_itemData;
        public ItemData itemData
        {
            get
            {
                if (m_itemData == null)
                {
                    m_itemData = TableManager.item.GetItemData(key, count);
                    m_itemData.value = value;
                }
                return m_itemData;
            }
        }
    }

    public enum PeroidType
    {
        Daily,
        Week,
        Season,
    }
}