using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.AddressableAssets.Build.Layout.BuildLayout;
using static UnityEngine.Rendering.DebugUI;

public class RewardItemComponent : TargetComponent, IValidatable
{
    public RewardWorker.RewardItemData data { get; private set; }
    public bool Initialize(RewardWorker.RewardItemData _itemData)
    {
        isSwitchSorting = true;
        m_element.sg.sortingLayerID = m_element.layerStart;
        m_element.ps.gameObject.SetActive(false);
        m_element.character.gameObject.SetActive(true);

        data = _itemData;

        for (int i = 0; i < m_element.panel.childCount; i++)
            m_element.panel.GetChild(i).gameObject.SetActive(false);

        var obj = m_element.GetObject(_itemData.itemType);
        if (obj == null)
        {
            IngameLog.Add("RewardItemComponent: Initialize: FAILED: " + _itemData.itemType);
            return false;
        }

        obj.SetActive(true);
        m_element.txtCount.gameObject.SetActive(_itemData.count > 1);
        if (_itemData.count > 1)
            m_element.txtCount.text = $"x{_itemData.count.AmountKMBT()}";

        return true;
    }

    public async UniTask ThrowStart(Transform _target, float _moveDuration)
    {
        isSwitchSorting = false;
        m_element.sg.sortingLayerID = m_element.layerAction;
        m_element.sg.sortingOrder = 1;

        var prevParent = transform.parent;
        transform.SetParent(_target.parent);

        m_element.ps.gameObject.SetActive(true);

        await transform.DOLocalMove(_target.localPosition, _moveDuration).SetEase(Ease.InBack).AsyncWaitForCompletion();

        m_element.character.gameObject.SetActive(false);
        transform.SetParent(prevParent);

        var prevScale = _target.localScale;
        _target.localScale *= 1.1f;
        _target.DOKill();
        _target.DOScale(prevScale, .2f);

        await UniTask.WaitUntil(() => m_element.ps.particleCount == 0);
        gameObject.SetActive(false);
    }

    #region VALIDATA
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_element.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public int layerStart;
        public int layerAction;

        public SortingGroup sg;
        public ParticleSystem ps;

        public Transform character;
        public Transform panel;
        public TextMeshProUGUI txtCount;

        public List<ItemObjectData> objectData;

        public void Initialize(Transform _transform)
        {
            sg = _transform.GetComponent<SortingGroup>();
            ps = _transform.GetComponent<ParticleSystem>("RewardEffect");

            character = _transform.Find("Character");
            panel = character.Find("Panel");

            objectData = new();
            for (var itemType = ItemType.NONE + 1; itemType < ItemType.MAX; itemType++)
            {
                var item = panel.Find(itemType.ToString());
                if (item != null)
                    objectData.Add(new() { type = itemType, obj = item.gameObject });
            }

            txtCount = _transform.GetComponent<TextMeshProUGUI>("Character/Canvas/txt_count");

            layerStart = SortingLayer.NameToID("Character");
            layerAction = SortingLayer.NameToID("UI");

            if (layerStart == 0 || layerAction == 0)
                IngameLog.Add($"{_transform.name}: layerError: layerStart{layerStart} / layerAction{layerAction}");
        }

        public GameObject GetObject(ItemType _itemType)
            => objectData.Find(x => x.type == _itemType).obj;
    }

    [Serializable]
    struct ItemObjectData
    {
        public ItemType type;
        public GameObject obj;
    }
    #endregion
}

