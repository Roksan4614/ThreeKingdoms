using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public enum RewardSpawnType
{
    Character,
    UI_Front,
    Popup,
}

public class RewardItemComponent : TargetComponent, IValidatable
{
    RewardWorker.RewardItemData m_data;
    public RewardWorker.RewardItemData data => m_data;

    Transform m_target;
    Transform m_prevParent;

    private void Start()
    {
        m_prevParent = transform.parent;

        Signal.instance.CloseLobbyScreenFinished.connectLambda = new(this, () =>
        {
            if (gameObject.activeInHierarchy == true)
                m_element.sg.sortingLayerID = m_element.psRenderer.sortingLayerID = m_element.layerPopup;
        });

        Signal.instance.OpenLobbyScreen.connectLambda = new(this, _screen =>
        {
            if (gameObject.activeInHierarchy == true)
                m_element.sg.sortingLayerID = m_element.psRenderer.sortingLayerID = m_element.layerCharacter;
        });
    }

    List<ItemType> m_ignoreLog = new();
    public bool Initialize(RewardWorker.RewardItemData _itemData, RewardSpawnType _spawnType, bool _isFXStart, Transform _target)
    {
        m_target = _target;
        transform.SetParent(_target.parent);

        isSwitchSorting = true;
        m_element.sg.sortingOrder = 1;
        m_element.character.gameObject.SetActive(true);
        m_element.ps.gameObject.SetActive(_isFXStart);
        m_element.sg.sortingLayerID = m_element.psRenderer.sortingLayerID =
            _spawnType == RewardSpawnType.Character ? m_element.layerCharacter : m_element.layerPopup;

        //if (_spawnType > RewardSpawnType.Character)
        m_element.sg.sortingOrder = (int)OrderLayerType.MAX;

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
            if (m_ignoreLog.Contains(_itemData.itemType) == false)
            {
                IngameLog.Add("RewardItemComponent: Initialize: FAILED: " + _itemData.itemType);
                m_ignoreLog.Add(_itemData.itemType);
            }
            return false;
        }

        obj.SetActive(true);
        m_element.txtCount.text = _itemData.name;
        m_element.txtCount.gameObject.SetActive(true);
        if (_itemData.count > 1)
            m_element.txtCount.text = $"x{_itemData.count.AmountKMBT()}";


        return true;
    }

    public async UniTask ThrowStart(float _moveDuration)
    {
        isSwitchSorting = false;

        m_element.ps.gameObject.SetActive(true);

        // 방향 곡선!!
        {
            Vector3 startPos = transform.localPosition;
            var endPos = m_target.localPosition;

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
        transform.SetParent(m_prevParent);

        var prevScale = m_target.localScale;
        prevScale.x = prevScale.y = prevScale.z;
        var scale = prevScale;
        scale *= 1.1f;
        scale.z = prevScale.z;
        m_target.localScale = scale;

        m_target.DOKill();
        m_target.DOScale(prevScale, .2f).Forget();

        // 금화와 군량일 경우 올려주는 연출
        if (m_data.isGoldRice)
            Signal.instance.UpdateAsset.Emit((true, m_data.itemType));

        FinishedAsync().Forget();
    }

    async UniTask FinishedAsync()
    {
        var emission = m_element.ps.emission;
        var prevValue = emission.rateOverDistanceMultiplier;
        emission.rateOverDistanceMultiplier = 0f;

        await UniTask.WaitUntil(() => m_element.ps.particleCount == 0);
        emission.rateOverDistanceMultiplier = prevValue;

        gameObject.SetActive(false);
    }

    protected override void LateUpdate()
    {
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
        public int layerPopup;
        public int layerCharacter;

        public SortingGroup sg;
        public ParticleSystem ps;
        public ParticleSystemRenderer psRenderer;

        public Transform character;
        public Transform panel;
        public TextMeshProUGUI txtCount;

        public List<ItemObjectData> objectData;

        public void Initialize(Transform _transform)
        {
            sg = _transform.GetComponent<SortingGroup>("Character");
            ps = _transform.GetComponent<ParticleSystem>("RewardEffect");
            psRenderer = _transform.GetComponent<ParticleSystemRenderer>("RewardEffect");

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

            layerCharacter = SortingLayer.NameToID("Character");
            layerPopup = SortingLayer.NameToID("Popup");

            if (layerCharacter == 0 || layerPopup == 0)
                IngameLog.Add($"{_transform.name}: layerError: layerCharacter{layerCharacter} / layserPopup{layerPopup}");
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

