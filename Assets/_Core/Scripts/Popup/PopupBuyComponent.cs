using Cysharp.Threading.Tasks;
using Rev9.ContentsMarket;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeKingdoms.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupBuyComponent : BasePopupComponent
{
    protected PopupBuyComponent() : base(PopupType.Buy) { }

    public StatusType statusType { get; private set; }
    public TableProductData productData { get; private set; }
    public int buyCount { get; private set; }

    long m_myCurrency, m_totalCost;

    bool isEnoughCurrency => m_myCurrency >= m_totalCost;
    public bool isLockEscape { get; set; } = false;

    private void Start()
    {
        m_element.btnBuy.onClick.AddListener(OnButton_Confirm);

        var controller = transform.Find("Panel/Controll");
        var btnMin = controller.GetComponent<ButtonHelper>("btn_min");
        var btnMax = controller.GetComponent<ButtonHelper>("btn_max");
        controller.GetComponent<Button>("btn_minus").onClick.AddListener(() => OnButton_Increase(true));
        controller.GetComponent<Button>("btn_plus").onClick.AddListener(() => OnButton_Increase(false));
        btnMin.onClick.AddListener(() => OnButton_MinMax(true));
        btnMax.onClick.AddListener(() => OnButton_MinMax(false));

        // SETTAB
        {

        }

        //setlocalization
        {
            transform.SetTextTable("Panel/Title/Text", "POPUP_BUY_TITLE");
            transform.SetTextTable("Panel/txt_title_item", "UI_PRODUCT");
            btnMin.text = TableManager.stringTable.GetString("UI_MIN");
            btnMax.text = TableManager.stringTable.GetString("UI_MAX");
            m_element.btnBuy.text = TableManager.stringTable.GetString("BUTTON_BUY");
        }

        Utils.WaitEscape(this, () => { if (isLockEscape == false) Close(); });
    }

    public override void OpenPopup(params object[] _args)
    {
        gameObject.SetActive(true);
        productData = (TableProductData)_args[0];
        statusType = StatusType.Wait;
        m_myCurrency = productData.currencyMyCount;

        OnButton_MinMax(true);

        Utils.SetActivePunch(m_element.panel, true);

        m_element.txtLimitCount.gameObject.SetActive(productData.hasLimit);
        if (productData.hasLimit)
        {
            if (productData.limit_reset_type == LimitResetType.Permanent)
            {
                m_element.txtLimitCount.text = TableManager.stringTable.GetStringFormat("UI_BUY_REMAIN_COUNT", productData.strRemainCount);
            }
            else
            {
                string periodType = TableManager.stringTable.GetString("PEROID_TYPE_" + productData.limit_reset_type.ToString().ToUpper());
                m_element.txtLimitCount.text = $"({periodType} {productData.strRemainCount})";
            }
        }

        //Reward
        {
            var content = m_element.scrollRewards.content;

            int i = 0;
            //상점 상품이라면
            if (productData is TableShopProductData)
            {
                var shopProductData = (TableShopProductData)productData;

                foreach (var p in shopProductData.rewards)
                {
                    var slot = (i == content.childCount ? Instantiate(content.GetChild(0), content) : content.GetChild(i))
                        .GetComponent<ItemComponent>();

                    slot.SetItemData(p);
                    i++;
                }

                //아이콘
                SetItemIconAsync(shopProductData).Forget();

                m_element.txtName.text = shopProductData.name;
                m_element.txtDesc.text = shopProductData.desc;
                if (m_element.txtDesc.text?.StartsWith("DESC_") ?? false)
                    m_element.txtDesc.text = "";
            }
            //일반 상품이라면
            else
            {
                //아이콘
                SetItemIconAsync().Forget();

                content.GetChild(0).GetComponent<ItemComponent>().SetItemData(productData.itemData);
                i++;

                m_element.txtName.text = productData.itemData.name;
                m_element.txtDesc.text = productData.itemData.desc;
                if (m_element.txtDesc.text?.StartsWith("DESC_") ?? false)
                    m_element.txtDesc.text = "";
            }

            for (; i < content.childCount; i++)
                content.GetChild(i).gameObject.SetActive(false);

            content.ForceRebuildLayout();
            content.anchoredPosition = Vector2.zero;
        }

        m_element.txtCurrencyCount.text =
            productData.pay_type == PayType.Cash ? "" : TableManager.stringTable.GetStringFormat("UI_MY_AMOUNT", m_myCurrency.AmountKMBT(_isMBT: true));
    }

    void SetCost()
    {
        m_totalCost = productData.price * buyCount;
        m_element.txtCost.text = m_totalCost.AmountKMBT(_isMBT: true);

        m_element.costType.SetCostType(productData.pay_type);
    }

    async UniTask SetItemIconAsync(TableShopProductData _shopProductData = null)
    {
        string key = "";

        if (_shopProductData == null)
            key = productData.itemData.type.ToString();
        else
            key = $"Product_{_shopProductData.key}";

        for (int i = 0; i < m_element.iconPanel.childCount; i++)
        {
            var obj = m_element.iconPanel.GetChild(i).gameObject;
            if (key.IsActive() == true && obj.name.Equals(key))
            {
                obj.SetActive(true);
                key = null;
            }
            else
                obj.SetActive(false);
        }

        if (key.IsActive() == true)
        {
            m_element.iconPanel.gameObject.SetActive(false);
            var asset = await AddressableManager.instance.GetItemIconAsync(key);

            bool isDefault = _shopProductData != null && asset == null;
            if (isDefault == true)
                asset = await AddressableManager.instance.GetItemIconAsync("Product_Default");

            if (asset != null)
            {
                var icon = Instantiate(asset, m_element.iconPanel);
                icon.AutoResizeParent(true);
                icon.name = key;

                if (isDefault == true)
                    icon.transform.SetText("Text", _shopProductData.name);
            }
        }

        m_element.iconPanel.gameObject.SetActive(true);
    }

    void OnButton_Increase(bool _isMinus)
    {
        if (_isMinus)
            buyCount = Mathf.Max(1, buyCount - 1);
        else
        {
            if ((buyCount + 1) * productData.price > m_myCurrency)
                return;

            buyCount = Mathf.Min(productData.buy_limit, buyCount + 1);
        }

        m_element.txtCount.text = $"{buyCount:#,0}";

        SetCost();
    }

    void OnButton_MinMax(bool _isMin)
    {
        buyCount = _isMin ? 1
            : Mathf.Max(1, productData.hasLimit
                ? Mathf.Min(productData.buy_limit, (int)(m_myCurrency / productData.price)) :
                (int)(m_myCurrency / productData.price));

        m_element.txtCount.text = $"{buyCount:#,0}";

        SetCost();
    }

    void OnButton_Confirm()
    {
        if (isEnoughCurrency == false && productData.pay_type != PayType.Cash)
        {
            PopupManager.instance.AlertShow_Table("NOT_ENOUGH_CURRENCY");
            return;
        }

        m_element.btnBuy.interactable = false;
        statusType = StatusType.Success;
    }

    public async UniTask<StatusType> WaitAsync()
    {
        await UniTask.WaitUntil(() => statusType != StatusType.Wait);
        return statusType;
    }

    public void SetResult(bool _isSuccessed)
    {
        if (_isSuccessed)
        {
            PopupManager.instance.AlertShow_Table("BUY_SUCCESS");
            Close();
        }
        else
            PopupManager.instance.AlertShow_Table("BUY_FAILED");

        m_element.btnBuy.interactable = true;
    }

    public override void Close()
    {
        Utils.SetActivePunch(m_element.panel, false, _callback: () =>
        {
            if (statusType == StatusType.Wait)
                statusType = StatusType.Cancel;
            gameObject.SetActive(false);
        });
    }

    public void BaseClose()
        => base.Close();

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtCount;
        public TextMeshProUGUI txtLimitCount;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtDesc;
        public TextMeshProUGUI txtCost;
        public TextMeshProUGUI txtCurrencyCount;

        public ScrollRect scrollRewards;

        public ButtonHelper btnBuy;
        public Transform iconPanel;
        public CostTypeHelper costType;

        public void Initialize(Transform _transform)
        {
            txtLimitCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_limit_count");
            txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/Controll/Count/Text");

            txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            txtDesc = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_desc");
            txtCost = _transform.GetComponent<TextMeshProUGUI>("Panel/btn_buy/Count/Text");
            txtCurrencyCount = _transform.GetComponent<TextMeshProUGUI>("Panel/btn_buy/txt_count");

            scrollRewards = _transform.GetComponent<ScrollRect>("Panel/Rewards");

            btnBuy = _transform.GetComponent<ButtonHelper>("Panel/btn_buy");
            iconPanel = _transform.Find("Panel/Icon/Panel");
            costType = _transform.GetComponent<CostTypeHelper>("Panel/btn_buy/Count/Text/Icon");
        }

        public Transform panel => txtLimitCount.transform.parent;
    }
    #endregion VALIDATE

}
