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

        Dictionary<ContentsMarketTabType, List<TableProductData>> m_db;

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
                    List<TableProductData> lstData = new();

                    lstData.Add(new()
                    {
                        reward_item_key = "rice",
                        price = 300,
                        reward_count = 100,
                        buy_limit = 5
                    });

                    lstData.Add(new()
                    {
                        reward_item_key = "free_gold",
                        price = 600,
                        reward_count = 100,
                        buy_limit = 5
                    });

                    lstData.Add(new()
                    {
                        reward_item_key = "rice",
                        periodType = PeriodType.Week,
                        price = 7000,
                        reward_count = 1000,
                        buy_limit = 3
                    });

                    lstData.Add(new()
                    {
                        reward_item_key = "free_gold",
                        periodType = PeriodType.Week,
                        price = 7000,
                        reward_count = 1000,
                        buy_limit = 3
                    });

                    if (i == ContentsMarketTabType.Daily)
                    {
                        lstData.Add(new()
                        {
                            reward_item_key = "time_stone",
                            price = 2000,
                            reward_count = 10,
                            buy_limit = 3
                        });
                        lstData.Add(new()
                        {
                            reward_item_key = $"dedicated_soul_stone_{Utils.ToSnakeCase(CharacterName.LiuBei.ToString())}",
                            price = 2000,
                            reward_count = 10,
                            buy_limit = 3
                        });
                    }
                    else if (i == ContentsMarketTabType.Tournament)
                    {
                        lstData.Add(new()
                        {
                            reward_item_key = "tournament_point",
                            periodType = PeriodType.Week,
                            price = 2500,
                            reward_count = 10,
                            buy_limit = 3
                        });

                        lstData.Add(new()
                        {
                            reward_item_key = "public_soul_stone",
                            periodType = PeriodType.Week,
                            price = 2500,
                            reward_count = 10,
                            buy_limit = 3
                        });
                    }
                    else if (i == ContentsMarketTabType.Raid)
                    {

                        lstData.Add(new()
                        {
                            reward_item_key = "public_soul_stone",
                            periodType = PeriodType.Week,
                            price = 3500,
                            reward_count = 10,
                            buy_limit = 3
                        });
                        lstData.Add(new()
                        {
                            reward_item_key = $"dedicated_soul_stone_{Utils.ToSnakeCase(CharacterName.LiuBei.ToString())}",
                            periodType = PeriodType.Season,
                            price = 4500,
                            reward_count = 5,
                            buy_limit = 3
                        });

                        lstData.Add(new()
                        {
                            reward_item_key = $"dedicated_soul_stone_{Utils.ToSnakeCase(CharacterName.CaoCao.ToString())}",
                            periodType = PeriodType.Season,
                            price = 4500,
                            reward_count = 5,
                            buy_limit = 3
                        });
                        lstData.Add(new()
                        {
                            reward_item_key = $"dedicated_soul_stone_{Utils.ToSnakeCase(CharacterName.SunQuan.ToString())}",
                            periodType = PeriodType.Season,
                            price = 4500,
                            reward_count = 5,
                            buy_limit = 3
                        });
                    }

                    for (int j = 0; j < lstData.Count; j++)
                    {
                        var d = lstData[j];
                        d.idx = j;
                        d.pay_type = i == ContentsMarketTabType.Tournament ? PayType.TournamentPoint : i == ContentsMarketTabType.Raid ? PayType.RaidPoint : PayType.FreeGold;
                        lstData[j] = d;
                    }

                    m_db.Add(i, lstData);
                }
            }
        }
        public List<TableProductData> GetProducts(ContentsMarketTabType _tabType)
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

        public async UniTask<bool> API_ProductBuy(ContentsMarketTabType _tabType, TableProductData _productData, int _countProduct)
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

    public enum PeriodType
    {
        Daily,
        Week,
        Season,
    }
}