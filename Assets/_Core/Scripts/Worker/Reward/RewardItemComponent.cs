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
    RewardWorker.RewardItemData m_data;
    public RewardWorker.RewardItemData data => m_data;
    public bool Initialize(RewardWorker.RewardItemData _itemData, bool _isCanvas, bool _isFXStart)
    {
        isSwitchSorting = true;
        m_element.sg.sortingLayerID = _isCanvas ? m_element.layerPopup : m_element.layerStart;
        m_element.character.gameObject.SetActive(true);
        m_element.ps.gameObject.SetActive(_isFXStart);

        var main = m_element.ps.main;
        var minMax = main.startDelay;
        minMax.constantMin = _isFXStart ? 0 : 0.2f;
        minMax.constantMax = _isFXStart ? 0 : 0.3f;
        main.startDelay = minMax;

        m_data = _itemData;

        for (int i = 0; i < m_element.panel.childCount; i++)
            m_element.panel.GetChild(i).gameObject.SetActive(false);

        var obj = m_element.GetObject(_itemData.itemType);
        if (obj == null)
        {
            IngameLog.Add("RewardItemComponent: Initialize: FAILED: " + _itemData.itemType);
            return false;
        }

        obj.SetActive(true);
        m_element.txtCount.text = _itemData.name;
        m_element.txtCount.gameObject.SetActive(true);
        if (_itemData.count > 1)
            m_element.txtCount.text = $"x{_itemData.count.AmountKMBT()}";

        return true;
    }

    public async UniTask ThrowStart(Transform _target, float _moveDuration, bool _isPopup)
    {
        isSwitchSorting = false;
        if (_isPopup == false)
        {
            m_element.sg.sortingLayerID = m_element.layerAction;
            m_element.sg.sortingOrder = 1;
        }

        var prevParent = transform.parent;
        transform.SetParent(_target.parent);

        m_element.ps.gameObject.SetActive(true);
        //m_element.txtCount.gameObject.SetActive(false);

        // 방향 곡선!!
        {
            Vector3 startPos = transform.localPosition;
            var endPos = _target.localPosition;

            Vector3 lookAt = endPos - startPos;
            float distance = lookAt.magnitude;

            Vector3 backPos = startPos + lookAt.normalized * -UnityEngine.Random.Range(0.1f, 0.15f) * distance;

            // 수직벡터
            Vector3 sideStep = new Vector3(-lookAt.y, lookAt.x, 0).normalized;

            float randomStrength = UnityEngine.Random.Range(-0.1f, 0.1f) * distance;

            Vector3 midPos = Vector3.Lerp(startPos, endPos, UnityEngine.Random.Range(0.1f, 0.5f));
            midPos += sideStep * randomStrength;// * randomDir;

            // 경로 패스 생성
            Vector3[] path = new Vector3[] { backPos, midPos, endPos };

            await transform.DOLocalPath(path, _moveDuration, PathType.CatmullRom)
                .SetEase(Ease.InCubic)
                .AsyncWaitForCompletion();
        }

        m_element.character.gameObject.SetActive(false);
        transform.SetParent(prevParent);

        var prevScale = _target.localScale;
        prevScale.x = prevScale.y = prevScale.z;
        var scale = prevScale;
        scale *= 1.1f;
        scale.z = prevScale.z;
        _target.localScale = scale;

        _target.DOKill();
        _target.DOScale(prevScale, .2f);

        // 금화와 군량일 경우 올려주는 연출
        if (m_data.isCurrency)
            Signal.instance.UpdateAsset.Emit((true, m_data.itemType));

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
        public int layerPopup;

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
            layerPopup = SortingLayer.NameToID("Popup");

            if (layerStart == 0 || layerAction == 0 || layerPopup == 0)
                IngameLog.Add($"{_transform.name}: layerError: layerStart{layerStart} / layerAction{layerAction} / layserPopup{layerPopup}");
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

