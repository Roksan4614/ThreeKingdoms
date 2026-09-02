using Cysharp.Threading.Tasks;
using DG.Tweening;
using Rev9.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopComponent : Singleton<TopComponent>, IValidatable
{
    Dictionary<ItemType, AssetData> m_assets = new();

    PopupInventoryComponent m_inventory;

    private void Start()
    {
        for (int i = 0; i < m_element.assets.Count; i++)
        {
            var data = m_element.assets[i];
            m_assets.Add(data.type, data);
            UpdateAsset(data.type, -1, false);
        }

        m_element.btnMenu.onClick.AddListener(() => OnButtonAsync_PopupMenu().Forget());
        m_element.popupMenu.SetActive(false);

        Signal.instance.UpdateAsset.connectLambda = new(this,
            _data =>
            {
                if (_data.itemType == ItemType.NONE)
                {
                    for (int i = 0; i < m_element.assets.Count; i++)
                        UpdateAsset(m_element.assets[i].type, -1, _data.isTween);
                }
                else
                    UpdateAsset(_data.itemType, -1, _data.isTween);
            });

        m_element.assets.Find(x=>x.type == ItemType.gold).button.onClick.AddListener(() => OpenInventoryAsync().Forget());
    }

    async UniTask OpenInventoryAsync()
    {
        if (m_inventory == null)
            m_inventory = await PopupManager.instance.OpenPopupAsync<PopupInventoryComponent>(PopupType.Inventory);

        var scale = m_element.arrowInventory.localScale;
        if (scale.y > 0)
        {
            scale.y *= -1;
            m_element.arrowInventory.localScale = scale;
        }

        m_inventory.SetActivePunchAsync(true).Forget();

        await UniTask.WaitUntil(() => m_inventory.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

        scale.y *= -1;
        m_element.arrowInventory.localScale = scale;
    }

    protected override void OnDestroy()
    {
        if (m_inventory != null)
            Destroy(m_inventory.gameObject);

        base.OnDestroy();
    }

    async UniTask OnButtonAsync_PopupMenu()
    {
        m_element.btnMenu.interactable = false;
        m_element.popupMenu.SetActive(true);

        await UniTask.WaitUntil(() => m_element.popupMenu.activeSelf == false, cancellationToken: destroyCancellationToken);

        await UniTask.WaitForSeconds(0.1f, cancellationToken: destroyCancellationToken);

        m_element.btnMenu.interactable = true;
    }

    public bool isSwitchUpdateAsset { get; set; } = true;

    public Transform GetAssetIcon(ItemType _type)
        => m_assets[_type].icon;

    public void UpdateAsset(ItemType _type, long _amount = -1, bool _isTween = true)
    {
        if (isSwitchUpdateAsset == false)
            return;

        var asset = m_element.assets.Find(x => x.type == _type);
        if (asset == null)
            return;

        long amount = _amount == -1 ? DataManager.userInfo.GetAssetAmount(_type) : _amount;

        string tweenKey = $"AssetTween_{_type}";
        DOTween.Kill(tweenKey);

        if (_isTween)
        {
            DOTween.To(() => m_assets[_type].amount,
                _result => SetAmountData(_type, _result),
                amount, 0.2f).SetId(tweenKey);
        }
        else
            SetAmountData(_type, amount);
    }

    void SetAmountData(ItemType _type, long _amount)
    {
        var data = m_assets[_type];
        data.amount = _amount;
        m_assets[_type] = data;

        data.button.text = _amount.AmountKMBT(_isMBT: true);
    }

    #region VALIDATA
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;

    [Serializable]
    public struct ElementData
    {
        public List<AssetData> assets;

        public Button btnMenu;
        public GameObject popupMenu;

        public Transform arrowInventory;

        public void Initialize(Transform _transform)
        {
            List<ItemType> assetTypes = new() { ItemType.gold, ItemType.rice };

            assets = new();
            foreach (var t in assetTypes)
            {
                AssetData asset = new();
                asset.type = t;
                asset.button = _transform.GetComponent<ButtonHelper>($"{t}");
                asset.icon = _transform.Find($"{t}/Icon");
                assets.Add(asset);
            }

            btnMenu = _transform.GetComponent<Button>("Menu");
            popupMenu = _transform.Find("Menu/Popup/Menu").gameObject;
            arrowInventory = _transform.Find("gold/img_arrow");
        }
    }

    [Serializable]
    public class AssetData
    {
        public ItemType type;
        public ButtonHelper button;
        public Transform icon;
        public long amount;
    }
    #endregion
}
