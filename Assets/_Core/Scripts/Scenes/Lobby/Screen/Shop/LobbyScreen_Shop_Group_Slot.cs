using Cysharp.Threading.Tasks;
using ThreeKingdoms.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyScreen_Shop_Group_Slot : MonoBehaviour, IValidatable
{
    public TableShopProductData productData { get; private set; }
    public ShopCategoryType category => productData.shop_category_type;
    public UnityAction<LobbyScreen_Shop_Group_Slot> actionProduct { get; set; }

    protected virtual void Start()
    {
        transform.GetComponent<Button>().onClick.AddListener(() => actionProduct(this));

        // setlocalization
        if (m_element.badge != null)
            m_element.badge.transform.SetTextTable("Badge/Text", "UI_SOLDOUT");
    }

    public virtual void SetProductData(TableShopProductData _productData)
    {
        productData = _productData;

        if (m_element.txtName != null)
            m_element.txtName.text = _productData.name;

        m_element.costType.SetCostType(_productData.pay_type);
        m_element.txtCost.text = _productData.price.AmountKMBT(_isMBT: true);
        m_element.txtCost.transform.ForceRebuildLayout(1);

        SetIconAsync().Forget();
    }

    async UniTask SetIconAsync()
    {
        var prefix = "Product_";
        switch (productData.shop_category_type)
        {
            case ShopCategoryType.Package:
            case ShopCategoryType.Pass:
                prefix += "BG_";
                break;
        }

        var asset = await AddressableManager.instance.GetItemIconAsync($"{prefix}{productData.key}");

        bool isDafult = asset == null;
        if (isDafult)
            asset = await AddressableManager.instance.GetItemIconAsync("Product_Default");

        if (asset != null)
        {
            var icon = Instantiate(asset, m_element.icon);
            if (isDafult == true)
                icon.transform.SetText("Text", productData.name);
        }

    }

    #region VALIDATE
    public virtual void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    protected ElementData m_element;

    [System.Serializable]
    protected struct ElementData
    {
        public TextMeshProUGUI txtName;
        public Transform icon;
        public TextMeshProUGUI txtCost;
        public CostTypeHelper costType;

        public GameObject badge;

        public void Initialize(Transform _transform)
        {
            txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_name");
            icon = _transform.Find("Panel/Icon");
            txtCost = _transform.GetComponent<TextMeshProUGUI>("Panel/Cost/Text");
            costType = _transform.GetComponent<CostTypeHelper>("Panel/Cost/Icon");
            badge = _transform.Find("Panel/Badge")?.gameObject;
        }
    }
    #endregion VALIDATE

}
