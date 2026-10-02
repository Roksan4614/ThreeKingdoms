using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Shop : LobbyScreen_Base
{
    ShopCategoryType m_curTab = ShopCategoryType.Pass;

    Dictionary<ShopCategoryType, TabData> m_dbTab = new();

    List<LobbyScreen_Shop_Group> m_groups = new();

    long m_currencyAmount;

    private void Start()
    {
        // SetTab
        {
            List<ShopCategoryType> sortTab = new() {
                ShopCategoryType.Pass, ShopCategoryType.Package, ShopCategoryType.GoldPaid,ShopCategoryType.GoldFree,ShopCategoryType.Rice
            };

            var content = m_element.scrollTab.content;
            var dot = m_element.scrollTab.transform.Find("Dot");
            for (var i = 0; i < sortTab.Count; i++)
            {
                var tab = sortTab[i];
                var slot = i < content.childCount ? content.GetChild(i) : Instantiate(content.GetChild(0), content);

                TabData data = new();
                data.button = slot.GetComponent<ButtonHelper>();
                data.button.text = TableManager.stringShop.GetString("CATEGORY_" + tab.ToString().ToUpper());
                data.imgDot = (i < dot.childCount ? dot.GetChild(i) : Instantiate(dot.GetChild(0), dot)).GetComponent<Image>();

                m_dbTab.Add(tab, data);
            }
            content.parent.ForceRebuildLayout();

            if (sortTab.Count < 5)
                dot.gameObject.SetActive(false);
        }

        foreach (var tab in m_dbTab)
            tab.Value.button.onClick.AddListener(() => OnButton_Tab(tab.Key));

        SetLayoutList();
        OnButton_Tab(ShopCategoryType.Pass, true);

        //setlocalization
        {
            transform.SetTextTable("Panel/Top/txt_title", "UI_CURRENCY_SHOP_TITLE");
        }

        Signal.instance.Inventory_UpdateCount.connectLambda = new(this, _itemData =>
        {
            if (_itemData.key == ItemKey.GoldPaid)
            {
                DOTween.To(() => m_currencyAmount,
                    _result =>
                    {
                        IngameLog.Add(_result);
                        m_element.txtCurrency.text = _result.AmountKMBT(_isMBT: true);
                    },
                    _itemData.count, 0.2f).OnComplete(() => m_currencyAmount = _itemData.count);
            }
        });
    }

    public override void Open(LobbyScreenType _prevScreen)
    {
        base.Open(_prevScreen);
        OnButton_Tab(ShopCategoryType.Pass);

        m_currencyAmount = InventoryWorker.instance.GetItemCount(ItemKey.GoldPaid);
        m_element.txtCurrency.text = m_currencyAmount.AmountKMBT(_isMBT: true);
    }

    bool m_isLockEscape = false;
    protected override bool IsEscapeloseScreen() => m_isLockEscape == false;

    void OnButton_Tab(ShopCategoryType _tabType, bool _isForce = false)
    {
        if (m_curTab == _tabType && _isForce == false)
            return;

        TabData curTabData = new();
        if (_isForce)
        {
            foreach (var tab in m_dbTab)
            {
                bool isCurrent = tab.Key == _tabType;
                tab.Value.button.SetDrawSelect(isCurrent);
                tab.Value.imgDot.color = isCurrent ? Color.black : Color.gray8;

                if (isCurrent == true)
                    curTabData = tab.Value;
            }
        }
        else
        {
            m_dbTab[m_curTab].button.SetDrawSelect(false);
            m_dbTab[m_curTab].imgDot.color = Color.gray8;

            curTabData = m_dbTab[_tabType];
            curTabData.button.SetDrawSelect(true);
            curTabData.imgDot.color = Color.black;
        }

        //닷 펀치해주자
        curTabData.imgDot.transform.DOPunchScale(Vector3.one * .1f, .1f);

        m_curTab = _tabType;

        // 스크롤을 이동해줄거야.
        m_element.scroll.velocity = Vector2.zero;
        m_element.scroll.content.SetAnchoredPositionY(m_groups.Find(x => x.category == m_curTab).posY);
    }

    void SetLayoutList()
    {
        m_element.scroll.content.anchoredPosition = Vector2.zero;

        // 2개 더 추가해줄거야. 무료재화, 군량
        var content = m_element.scroll.content;
        {
            for (int i = 0; i < 2; i++)
                Instantiate(content.GetChild(2), content);
        }

        {
            int i = 0;
            foreach (var tab in m_dbTab)
            {
                var group = content.GetChild(i).GetComponent<LobbyScreen_Shop_Group>();
                group.name = tab.Key.ToString();

                var products = TableManager.shopProduct.GetProducts(tab.Key);
                group.actionProduct = OnButton_Product;
                group.Initialize(products.ToArray());
                m_groups.Add(group);
                i++;
            }
        }

        content.ForceRebuildLayout();
    }

    void OnButton_Product(LobbyScreen_Shop_Group_Slot _slot)
        => BuyProductAsync(_slot).Forget();

    PopupBuyComponent m_popupBuy;
    async UniTask BuyProductAsync(LobbyScreen_Shop_Group_Slot _slot)
    {
        var productData = _slot.productData;
        if (productData.isEnoughCurrency == false)
        {
            var itemName = TableManager.item.GetItemData(productData.currencyKey).name;
            itemName = KoreanHelper.AppendJosa(itemName, KoreanHelper.JosaType.IgA, "[{0}]");

            var result = await PopupManager.instance.OpenModalAsync(
                TableManager.alertString.GetStringFormat("MODAL_BUY_CURRENCY_NOT_ENOUGH", itemName));

            if (result == StatusType.Success)
            {
                if (productData.currencyKey == ItemKey.GoldPaid)
                {
                    var group = m_groups.Find(x => x.category == ShopCategoryType.GoldPaid);
                    var slots = group.slots.SortBy(x => x.productData.reward_count);

                    var price = slots[slots.Count - 1].productData.price;

                    foreach (var s in slots)
                    {
                        if (s.productData.reward_count >= productData.price)
                        {
                            price = s.productData.price;
                            break;
                        }
                    }

                    DataManager.shop.OpenURL_GoldPaid(price);
                }
                else
                {
                    OnButton_Tab(productData switch
                    {
                        _ => ShopCategoryType.GoldPaid
                    });
                }
            }

            return;
        }

        m_isLockEscape = true;

        if (m_popupBuy == null)
            m_popupBuy = await PopupManager.instance.OpenPopupAsync<PopupBuyComponent>(PopupType.Buy, productData);
        else
            m_popupBuy.OpenPopup(productData);

        if (await m_popupBuy.WaitAsync() == StatusType.Success)
        {
            if (productData.pay_type == PayType.Cash)
            {
                DataManager.shop.OpenURL_GoldPaid(productData.price);
                m_popupBuy.SetResult(true);
            }
            else
            {
                m_popupBuy.isLockEscape = true;
                Utils.SetActivePunch(m_element.panel, false);
                await UniTask.WaitForSeconds(.1f);
                m_popupBuy.SetResult(await DataManager.shop.API_BuyItemAsync(productData));

                if (productData.product_type == ShopProductType.Pass)
                    DataManager.pass.SetBuyBattlePass();

                Signal.instance.Buy_Item.Emit(productData);

                Utils.SetActivePunch(m_element.panel, true);
                m_popupBuy.isLockEscape = false;
            }
        }

        m_isLockEscape = false;
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ScrollRect scrollTab;
        public ScrollRect scroll;

        public TextMeshProUGUI txtCurrency;

        public void Initialize(Transform _transform)
        {
            scrollTab = _transform.GetComponent<ScrollRect>("Panel/Tab");
            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");

            txtCurrency = _transform.GetComponent<TextMeshProUGUI>("Panel/Asset/txt_amount");
        }

        public Transform panel => scroll.transform.parent;
    }
    #endregion VALIDATE

    struct TabData
    {
        public ButtonHelper button;
        public Image imgDot;
    }
}
