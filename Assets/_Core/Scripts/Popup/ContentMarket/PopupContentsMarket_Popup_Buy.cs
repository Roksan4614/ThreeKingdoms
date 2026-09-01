using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.ContentsMarket
{
    public class PopupContentsMarket_Popup_Buy : MonoBehaviour, IValidatable
    {
        ContentsMarketProductData m_productData;
        ContentsMarketTabType m_tabType;
        int m_buyCount = 0;

        private void Awake()
        {
            transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);
            transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(Close);

            var btnConfirm = transform.GetComponent<ButtonHelper>("Panel/Button/btn_confirm");
            btnConfirm.onClick.AddListener(() => OnButtonAsync_Confirm().Forget());

            var controller = transform.Find("Panel/Controll");
            var btnMin = controller.GetComponent<ButtonHelper>("btn_min");
            var btnMax = controller.GetComponent<ButtonHelper>("btn_max");
            controller.GetComponent<Button>("btn_minus").onClick.AddListener(() => OnButton_Increase(true));
            controller.GetComponent<Button>("btn_plus").onClick.AddListener(() => OnButton_Increase(false));
            btnMin.onClick.AddListener(() => OnButton_MinMax(true));
            btnMax.onClick.AddListener(() => OnButton_MinMax(false));

            //setlocalization
            btnMin.text = "_최소_";
            btnMax.text = "_최대_";
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

        public void SetProductData(ContentsMarketProductData _productData, ContentsMarketTabType _tabType)
        {
            m_tabType = _tabType;
            m_productData = _productData;

            gameObject.SetActive(true);
            Utils.SetActivePunch(m_element.panel, true);

            m_element.rewardItem.SetItemData(_productData.itemData);
            string peroidType = TableManager.stringTable.GetString("PEROID_TYPE_" + _productData.peroidType.ToString().ToUpper());
            m_element.txtLimitCount.text = $"({peroidType} {_productData.strRemainCount})";

            OnButton_MinMax(true);
        }

        void OnButton_Increase(bool _isMinus)
        {
            if (_isMinus)
                m_buyCount = Mathf.Max(1, m_buyCount - 1);
            else
                m_buyCount = Mathf.Min(m_productData.countMax, m_buyCount + 1);

            m_element.txtCount.text = $"{m_buyCount:#,0}";
        }

        void OnButton_MinMax(bool _isMin)
        {
            m_buyCount = _isMin ? 1 : m_productData.countMax;
            m_element.txtCount.text = $"{m_buyCount:#,0}";
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
                PopupManager.instance.AlertShow("_구매 실패_");
        }

        void Close()
        {
            Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

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
