using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TopComponent : Singleton<TopComponent>, IValidatable
{
    Dictionary<ItemType, AssetData> m_assets = new();

    private void Start()
    {
        for (int i = 0; i < m_element.assets.Count; i++)
        {
            var data = m_element.assets[i];
            m_assets.Add(data.type, data);
            UpdateAsset(data.type, -1, false);
        }

        Signal.instance.UpdateAsset.connectLambda = new(this,
            _data =>
            {
                if (_data._itemType == ItemType.NONE)
                {
                    for (int i = 0; i < m_element.assets.Count; i++)
                        UpdateAsset(m_element.assets[i].type, -1, _data._isTween);
                }
                else
                    UpdateAsset(_data._itemType, -1, _data._isTween);
            });
    }

    public bool isSwitchUpdateAsset { get; set; } = true;

    public Transform GetAssetIcon(ItemType _type)
        => m_assets[_type].icon;

    public void UpdateAsset(ItemType _type, long _amount = -1, bool _isTween = true)
    {
        if (isSwitchUpdateAsset == false)
            return;

        var asset = m_element.assets.Find(x => x.type == _type);
        if (asset.isActive == false)
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

        data.txtAmount.text = _amount.AmountKMBT(_isMBT: true);
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
        public void Initialize(Transform _transform)
        {
            List<ItemType> assetTypes = new() { ItemType.Gold, ItemType.Rice };

            assets = new();
            foreach (var t in assetTypes)
            {
                AssetData asset = new();
                asset.type = t;
                asset.txtAmount = _transform.GetComponent<TextMeshProUGUI>($"{t}/txt_amount");
                asset.icon = _transform.Find($"{t}/Icon");
                assets.Add(asset);
            }
        }
    }

    [Serializable]
    public struct AssetData
    {
        public ItemType type;
        public TextMeshProUGUI txtAmount;
        public Transform icon;
        public long amount;

        public bool isActive => type > ItemType.NONE;
    }
    #endregion
}
