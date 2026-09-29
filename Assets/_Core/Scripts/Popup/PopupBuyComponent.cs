using Cysharp.Threading.Tasks;
using Rev9.ContentsMarket;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    bool m_isLockEscape = false;

    private void Start()
    {
        m_element.btnBuy.onClick.AddListener(() => OnButtonAsync_Confirm().Forget());

        var controller = transform.Find("Panel/Controll");
        var btnMin = controller.GetComponent<ButtonHelper>("btn_min");
        var btnMax = controller.GetComponent<ButtonHelper>("btn_max");
        controller.GetComponent<Button>("btn_minus").onClick.AddListener(() => OnButton_Increase(true));
        controller.GetComponent<Button>("btn_plus").onClick.AddListener(() => OnButton_Increase(false));
        btnMin.onClick.AddListener(() => OnButton_MinMax(true));
        btnMax.onClick.AddListener(() => OnButton_MinMax(false));

        //setlocalization
        {
            transform.SetTextTable("Panel/Title/Text", "POPUP_BUY_TITLE");
            transform.SetTextTable("Panel/txt_title_item", "UI_PRODUCT");
            btnMin.text = TableManager.stringTable.GetString("UI_MIN");
            btnMax.text = TableManager.stringTable.GetString("UI_MAX");
            m_element.btnBuy.text = TableManager.stringTable.GetString("BUTTON_BUY");
        }

        Utils.WaitEscape(this, () => { if (m_isLockEscape == false) Close(); });
    }

    public override void OpenPopup(params object[] _args)
    {
        gameObject.SetActive(true);
        productData = (TableProductData)_args[0];
        statusType = StatusType.Wait;
        m_myCurrency = productData.currencyMyCount;

        OnButton_MinMax(true);

        Utils.SetActivePunch(m_element.panel, true);

        string periodType = TableManager.stringTable.GetString("PERIOD_TYPE_" + productData.periodType.ToString().ToUpper());
        m_element.txtLimitCount.text = $"({periodType} {productData.strRemainCount})";

        m_element.txtName.text = productData.itemData.name;
        m_element.txtDesc.text = productData.itemData.desc;
        if (m_element.txtDesc.text.StartsWith("DESC_"))
            m_element.txtDesc.text = "";

        //아이콘
        SetItemIconAsync().Forget();

        //Reward
        {
            var content = m_element.scrollRewards.content;

            // todo 갯수가 여러개 있을 수 있어. 그거 작업해야 해
            content.GetChild(0).GetComponent<ItemComponent>().SetItemData(productData.itemData);
        }

        m_element.txtCurrencyCount.text = TableManager.stringTable.GetStringFormat("UI_MY_AMOUNT", m_myCurrency.AmountKMBT(_isMBT: true));
    }

    void SetCost()
    {
        m_totalCost = productData.price * buyCount;
        m_element.txtCost.text = m_totalCost.AmountKMBT(_isMBT: true);

        string costType = productData.pay_type.ToString();
        for (int i = 0; i < m_element.costPanel.childCount; i++)
        {
            var obj = m_element.costPanel.GetChild(i).gameObject;
            obj.SetActive(obj.name.Equals(costType));
        }
    }

    async UniTask SetItemIconAsync()
    {
        string key = productData.itemData.type.ToString();
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
            if (asset != null)
            {
                var icon = Instantiate(asset, m_element.iconPanel);
                icon.AutoResizeParent(true);
                icon.name = key;
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

    async UniTask OnButtonAsync_Confirm()
    {
        if (isEnoughCurrency == false)
        {
            PopupManager.instance.AlertShow_Table("NOT_ENOUGH_CURRENCY");
            return;
        }

        m_element.btnBuy.interactable = false;
        bool isSuccess = true;
        //= await ContentsMarketWorker.instance.API_ProductBuy(m_tabType, m_productData, m_buyCount);

        if (isSuccess)
        {




            List<ItemData> rewards = new();
            //for (int i = 0; i < m_buyCount; i++)
            //    rewards.Add(m_productData.itemData);

            RewardWorker.OpenRewardPopup(rewards.ToArray());
            Close();
        }
        else
            PopupManager.instance.AlertShow_Table("BUY_FAILED");
    }

    public override void Close()
    {
        Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
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
        public Transform costPanel;

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
            costPanel = _transform.Find("Panel/btn_buy/Count/Text/Icon");
        }

        public Transform panel => txtLimitCount.transform.parent;
    }
    #endregion VALIDATE

}
