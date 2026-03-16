using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
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

    public void Run(Transform _from, ItemType _itemType, long _count = 1)
    {
        RewardData rewardData = new();
        rewardData.startPos = _from.position;
        rewardData.rewards = new()
        {
            new(){ itemType = _itemType,  count = _count}
        };

        Run(rewardData);
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

                if (item.Initialize(reward) == false)
                    continue;

                item.gameObject.SetActive(true);
                rewardComps.Add(item);

                item.transform.position = data.startPos;

                Vector3 lookAt = (Vector3)UnityEngine.Random.insideUnitCircle.normalized;
                Vector3 dist = lookAt * UnityEngine.Random.Range(m_actionData.distInstantiateMIN, m_actionData.distInstantiateMAX);

                item.transform.DOMove(data.startPos + dist, m_actionData.durationInstantiate).SetEase(Ease.OutCubic);
            }
        }

        // 생성하고 조금 기다려주자
        await UniTask.WaitForSeconds(m_actionData.durationWait + m_actionData.durationInstantiate);
        await UniTask.WaitUntil(() => isSwitchReceive == true);

        //목적지까지 날려주자
        for (int i = 0; i < rewardComps.Count; i++)
            rewardComps[i].ThrowStart(GetThrowTarget(rewardComps[i].data), m_actionData.durationMove).Forget();
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

        public void SetDefault()
        {
            durationWait = 1f;
            durationInstantiate = 0.2f;
            durationMove = 0.5f;

            distInstantiateMAX = 2f;
            distInstantiateMIN = 0.5f;
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
            itemType = _itemType; count = _count;
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
