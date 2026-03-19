using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class RewardWorker : Singleton<RewardWorker>, IValidatable
{
    [SerializeField]
    RewardActionData m_actionData;

    List<RewardItemComponent> m_dbItems = new();

    CharacterComponent m_mainHero;

    public bool isSwitchReceive { get; set; } = true;
    public float durationInstantiate => m_actionData.durationInstantiate;

    private void Start()
    {
        if (m_actionData.durationWait == 0)
            m_actionData.SetDefault();

        m_element.baseRewardItem.gameObject.SetActive(false);

        Signal.instance.ConnectMainHero.connectLambda = new(this, _mainhero => m_mainHero = _mainhero);
    }

    public void Run(Vector3 _posFrom, ItemType _itemType, long _count = 1, bool _isStartPunch = true
        , bool _isFXStart = false, float _distMax = 0
        , bool _isCanvas = false, float _durationWait = -1, bool _isTargetPunch = false, Vector3 _posTargetPunch = default)
        => RunAsync(_posFrom, _itemType, _count, _isStartPunch, _isFXStart, _distMax, _isCanvas, _durationWait, _isTargetPunch, _posTargetPunch).Forget();

    public async UniTask RunAsync(Vector3 _posFrom, ItemType _itemType, long _count = 1, bool _isStartPunch = true
        , bool _isFXStart = false, float _distMax = 0
        , bool _isCanvas = false, float _durationWait = -1, bool _isTargetPunch = false, Vector3 _posTargetPunch = default)
    {
        RewardData rewardData = new();
        rewardData.startPos = _posFrom;
        rewardData.rewards = new()
        {
            new(){ itemType = _itemType,  count = _count }
        };

        m_actionData.distInstantiateMAX = _distMax > 0 ? _distMax : m_actionData.distInstantiateMAX;
        if (m_actionData.distInstantiateMAX < m_actionData.distInstantiateMIN)
            m_actionData.distInstantiateMIN = m_actionData.distInstantiateMAX;

        m_actionData.isFXStart = _isFXStart;
        m_actionData.isStartPunch = _isStartPunch;
        m_actionData.isTargetPunch = _isTargetPunch;
        m_actionData.posTargetPunch = _posTargetPunch;
        m_actionData.durationThrow = _durationWait == -1 ? m_actionData.durationWait + m_actionData.durationInstantiate : _durationWait;
        m_actionData.isCanvas = _isCanvas;

        await RunAsync(rewardData);
    }

    public void Run(params RewardData[] rewardData)
        => RunAsync(rewardData).Forget();

    public async UniTask RunAsync(params RewardData[] _rewardData)
    {
        List<RewardItemComponent> rewardComps = new();
        for (int i = 0; i < _rewardData.Length; i++)
        {
            var data = _rewardData[i];

            for (int j = 0; j < data.rewards.Count; j++)
            {
                var reward = data.rewards[j];

                RewardItemComponent item = m_dbItems.Find(x => x.gameObject.activeSelf == false);
                if (item == null)
                {
                    item = Instantiate(m_element.baseRewardItem, transform);
                    m_dbItems.Add(item);
                }

                if (item.Initialize(reward, m_actionData.isCanvas, m_actionData.isFXStart) == false)
                    continue;

                item.gameObject.SetActive(true);
                rewardComps.Add(item);

                item.transform.position = data.startPos;

                if (m_actionData.isStartPunch)
                {
                    Vector3 targetPos = m_actionData.isTargetPunch
                        ? m_actionData.posTargetPunch
                        : GetPositionStartPunch(data.startPos);

                    item.transform.DOMove(targetPos, m_actionData.durationInstantiate).SetEase(Ease.OutCubic);
                }
            }
        }

        if (m_actionData.durationWait > -1)
        {
            // 생성하고 조금 기다려주자
            await UniTask.WaitForSeconds(m_actionData.durationWait);
            await UniTask.WaitUntil(() => isSwitchReceive == true);
        }

        //목적지까지 날려주자
        for (int i = 0; i < rewardComps.Count; i++)
            rewardComps[i].ThrowStart(GetThrowTarget(rewardComps[i].data), m_actionData.durationMove, m_actionData.isCanvas).Forget();
    }

    public Vector3 GetPositionStartPunch(Vector3 _startPos)
    {
        Vector3 lookAt = (Vector3)UnityEngine.Random.insideUnitCircle.normalized;
        Vector3 dist = lookAt * UnityEngine.Random.Range(m_actionData.distInstantiateMIN, m_actionData.distInstantiateMAX);

        return _startPos + dist;
    }

    public Transform GetThrowTarget(RewardItemData _rewardItemData)
    {
        Transform target = _rewardItemData.isCurrency ?
            TopComponent.instance?.GetAssetIcon(_rewardItemData.itemType) :
            BottomComponent.instance?.GetIconScreen(_rewardItemData.itemType);

        if (target == null)
            target = m_mainHero?.transform;

        return target;
    }

    [Serializable]
    struct RewardActionData
    {
        public float durationWait;
        public float durationInstantiate;
        public float durationMove;

        public float distInstantiateMIN;
        public float distInstantiateMAX;

        public bool isFXStart;          // 시작할 때부터 이펙트 터지기
        public bool isStartPunch;       // 시작할 때 아이템 퍼트릴꺼?
        public bool isTargetPunch;      // 퍼트리는데 타켓 설정할꺼?
        public Vector3 posTargetPunch;  // 그 위치는?
        public float durationThrow;     // 던지기전에 기다리는 시간
        public bool isCanvas;           // 캔버스라면??

        public void SetDefault()
        {
            durationWait = 1f;
            durationInstantiate = 0.2f;
            durationMove = 0.5f;

            distInstantiateMAX = 2f;
            distInstantiateMIN = 0.5f;

            isStartPunch = true;
            durationThrow = -1;
        }
    }

    public struct RewardData
    {
        public List<RewardItemData> rewards;
        public Vector3 startPos;
    }

    public struct RewardItemData
    {
        public ItemType itemType;
        public long count;

        public RewardItemData(ItemType _itemType, long _count = 1)
        {
            itemType = _itemType;
            count = _count;
        }

        public bool isCurrency => itemType == ItemType.Gold || itemType == ItemType.Rice;
    }

    #region VALIDATA
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RewardItemComponent baseRewardItem;

        public void Initialize(Transform _transform)
        {
            baseRewardItem = _transform.GetComponent<RewardItemComponent>("Item");
        }
    }
    #endregion
}
