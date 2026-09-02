using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rev9.ContentsMarket
{
    public class ContentsMarketWorker : MonoSingleton<ContentsMarketWorker>
    {
        Dictionary<ContentsMarketTabType, List<ContentsMarketProductData>> m_db;

        protected override void OnDestroy()
        {
            m_db = null;
            base.OnDestroy();
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
                        key = ItemType.rice,
                        cost = 1000,
                        count = 100,
                        countMax = 5
                    });

                    lstData.Add(new()
                    {
                        key = ItemType.gold,
                        cost = 1000,
                        count = 100,
                        countMax = 5
                    });

                    lstData.Add(new()
                    {
                        key = ItemType.rice,
                        peroidType = PeroidType.Week,
                        cost = 7000,
                        count = 1000,
                        countMax = 3
                    });

                    lstData.Add(new()
                    {
                        key = ItemType.gold,
                        peroidType = PeroidType.Week,
                        cost = 7000,
                        count = 1000,
                        countMax = 3
                    });

                    if (i == ContentsMarketTabType.Daily)
                    {
                        lstData.Add(new()
                        {
                            key = ItemType.time_stone,
                            cost = 2000,
                            count = 10,
                            countMax = 3
                        });
                        lstData.Add(new()
                        {
                            key = ItemType.dedicated_soul_stone,
                            cost = 2000,
                            count = 10,
                            countMax = 3
                        });
                    }
                    else if (i == ContentsMarketTabType.Tournament)
                    {
                        lstData.Add(new()
                        {
                            key = ItemType.tournament_point,
                            peroidType = PeroidType.Week,
                            cost = 2500,
                            count = 10,
                            countMax = 3
                        });

                        lstData.Add(new()
                        {
                            key = ItemType.public_soul_stone,
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
                            key = ItemType.public_soul_stone,
                            peroidType = PeroidType.Week,
                            cost = 3500,
                            count = 10,
                            countMax = 3
                        });
                        lstData.Add(new()
                        {
                            key = ItemType.dedicated_soul_stone,
                            peroidType = PeroidType.Season,
                            value = CharacterName.LiuBei.ToString(),
                            cost = 4500,
                            count = 5,
                            countMax = 3
                        });

                        lstData.Add(new()
                        {
                            key = ItemType.dedicated_soul_stone,
                            peroidType = PeroidType.Season,
                            value = CharacterName.CaoCao.ToString(),
                            cost = 4500,
                            count = 5,
                            countMax = 3
                        });
                        lstData.Add(new()
                        {
                            key = ItemType.dedicated_soul_stone,
                            peroidType = PeroidType.Season,
                            value = CharacterName.SunQuan.ToString(),
                            cost = 4500,
                            count = 5,
                            countMax = 3
                        });
                    }

                    for (int j = 0; j < lstData.Count; j++)
                    {
                        var d = lstData[j];
                        d.idx = j;
                        d.costType = i == ContentsMarketTabType.Tournament ? ItemType.tournament_point : i == ContentsMarketTabType.Raid ? ItemType.raid_point : ItemType.gold;
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
        [JsonProperty] public ItemType key;
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

        ItemType? cost_type;
        public ItemType costType
        {
            get => cost_type ?? ItemType.gold;
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