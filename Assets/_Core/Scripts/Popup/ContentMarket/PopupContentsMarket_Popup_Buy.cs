using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.ContentsMarket
{
    public class PopupContentsMarket_Popup_Buy : PopupBuyComponent
    {
        TableProductData m_productData;
        ContentsMarketTabType m_tabType;
        int m_buyCount = 0;

        private void Start()
        {
            transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);
            transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(Close);

        }

        public bool CloseEscape()
        {
            if (gameObject.activeSelf)
            {
                if (PopupManager.instance.IsOpenPopup(PopupType.Reward) == false)
                    Close();

                return true;
            }

            return false;
        }

        public void SetProductData(TableProductData _productData, ContentsMarketTabType _tabType)
        {
            m_tabType = _tabType;
            m_productData = _productData;

            gameObject.SetActive(true);
            Utils.SetActivePunch(m_elementContentMarket.panel, true);

            m_elementContentMarket.rewardItem.SetItemData(_productData.itemData);
            string periodType = TableManager.stringTable.GetString("PERIOD_TYPE_" + _productData.limit_reset_type.ToString().ToUpper());
            m_elementContentMarket.txtLimitCount.text = $"({periodType} {_productData.strRemainCount})";

            OnButton_MinMax(true);
        }

        void OnButton_Increase(bool _isMinus)
        {
            if (_isMinus)
                m_buyCount = Mathf.Max(1, m_buyCount - 1);
            else
                m_buyCount = Mathf.Min(m_productData.buy_limit, m_buyCount + 1);

            m_elementContentMarket.txtCount.text = $"{m_buyCount:#,0}";
        }

        void OnButton_MinMax(bool _isMin)
        {
            m_buyCount = _isMin ? 1 : m_productData.buy_limit;
            m_elementContentMarket.txtCount.text = $"{m_buyCount:#,0}";
        }

        async UniTask OnButtonAsync_Confirm()
        {
            bool isSuccess = await ContentsMarketWorker.instance.API_ProductBuy(m_tabType, m_productData, m_buyCount);

            if (isSuccess)
            {
                List<ItemData> rewards = new();
                for (int i = 0; i < m_buyCount; i++)
                    rewards.Add(m_productData.itemData);

                RewardWorker.OpenRewardPopup(rewards.ToArray());
                PopupManager.instance.GetPopup<PopupContentsMarketComponent>(PopupType.ContentsMarket).SetProductLayout();
                Close();
            }
            else
                PopupManager.instance.AlertShow_Table("BUY_FAILED");
        }

        void Close()
        {
            Utils.SetActivePunch(m_elementContentMarket.panel, false, _callback: () => gameObject.SetActive(false));
        }

        #region VALIDATE
        public override void OnManualValidate()
        {
            base.OnManualValidate();
            m_elementContentMarket.Initialize(transform);
        }

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_elementContentMarket;

        [System.Serializable]
        struct ElementData
        {
            public ItemComponent rewardItem;
            public TextMeshProUGUI txtCount;
            public TextMeshProUGUI txtLimitCount;

            public void Initialize(Transform _transform)
            {
                rewardItem = _transform.GetComponent<ItemComponent>("Panel/Reward/Slot");
                txtLimitCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_limit_count");
                txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/Controll/Count/Text");
            }

            public Transform panel => txtLimitCount.transform.parent;
        }
        #endregion VALIDATE
    }
}
