using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public void AddAsset(long _gold, long _rice, Transform _fromTarget = null, bool _isPunch = true)
    {
        DataManager.userInfo.AddAsset(_gold, _rice, false);

        Vector3 posFrom = _fromTarget == null ? CameraManager.posPointer : _fromTarget.position;

        if (_gold > 0)
            Run(posFrom, ItemType.Gold, _gold, _isPopup: true, _isStartPunch: _isPunch, _durationWait: UnityEngine.Random.Range(0.5f, 1f));
        if (_rice > 0)
            Run(posFrom, ItemType.Rice, _rice, _isPopup: true, _isStartPunch: _isPunch, _durationWait: UnityEngine.Random.Range(0.5f, 1f));
    }

    public async UniTask RunAsync(Vector3 _posFrom, bool _isPopup = true, bool _isStartPunch = false, params ItemData[] _itemData)
    {
        List<UniTask> tasks = new();

        for (int i = 0; i < _itemData.Length; i++)
        {
            var data = _itemData[i];

            DataManager.userInfo.AddItem(false, _isAction: false, _itemData: _itemData);

            tasks.Add(RunAsync(_posFrom, _itemData[i].key, _itemData[i].count, _isPopup: _isPopup, _isStartPunch: _isStartPunch));
        }

        await UniTask.WhenAll(tasks.ToArray());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_posFrom"></param>
    /// <param name="_itemType"></param>
    /// <param name="_count"></param>
    /// <param name="_isStartPunch">시작할 때 흐트러 트릴거야</param>
    /// <param name="_isFXStart">시작부터 FX 켜줄거야</param>
    /// <param name="_distMax">거리 최대거리</param>
    /// <param name="_isField">필드인지</param>
    /// <param name="_isScreen">로비 스크린인지</param>
    /// <param name="_isPopup">팝업인지</param>
    /// <param name="_durationWait">기다리는 시간</param>
    /// <param name="_isTargetPunch">방향으로 흐터질거야</param>
    /// <param name="_posTargetPunch">흐터지는 위치</param>
    public void Run(Vector3 _posFrom, ItemType _itemType, long _count = 1, bool _isStartPunch = true
        , bool _isFXStart = false, float _distMax = 0
        , bool _isField = false, bool _isScreen = false, bool _isPopup = false,
        float _durationWait = -1, bool _isTargetPunch = false, Vector3 _posTargetPunch = default)
        => RunAsync(_posFrom, _itemType, _count, _isStartPunch, _isFXStart, _distMax, _isField, _isScreen, _isPopup, _durationWait, _isTargetPunch, _posTargetPunch).Forget();

    public async UniTask RunAsync(Vector3 _posFrom, ItemType _itemType, long _count = 1, bool _isStartPunch = true
        , bool _isFXStart = false, float _distMax = 0
        , bool _isField = false, bool _isScreen = false, bool _isPopup = false,
        float _durationWait = -1, bool _isTargetPunch = false, Vector3 _posTargetPunch = default)
    {
        RewardData rewardData = new();
        rewardData.startPos = _posFrom;
        rewardData.rewards = new()
        {
            new(_itemType, _count)
        };

        m_actionData.distInstantiateMAX = _distMax > 0 ? _distMax : m_actionData.distInstantiateMAX;
        if (m_actionData.distInstantiateMAX < m_actionData.distInstantiateMIN)
            m_actionData.distInstantiateMIN = m_actionData.distInstantiateMAX;
        else
            m_actionData.distInstantiateMIN = 1;

        m_actionData.isFXStart = _isFXStart;
        m_actionData.isStartPunch = _isStartPunch;
        m_actionData.isTargetPunch = _isTargetPunch;
        m_actionData.posTargetPunch = _posTargetPunch;
        m_actionData.durationWait = _durationWait;

        if (_isField)
        {
            if (LobbyScreenManager.instance.curScreen == LobbyScreenType.None)
                m_actionData.spawnType = RewardSpawnType.UI_Front;
            else
                m_actionData.spawnType = RewardSpawnType.Character;
        }
        else if (_isScreen)
            m_actionData.spawnType = RewardSpawnType.UI_Front;
        else
            m_actionData.spawnType = RewardSpawnType.Popup;

        await RunAsync(rewardData);
    }

    void Run(params RewardData[] rewardData)
        => RunAsync(rewardData).Forget();

    async UniTask RunAsync(params RewardData[] _rewardData)
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

                if (item.Initialize(reward, m_actionData.spawnType, m_actionData.isFXStart, GetThrowTarget(reward)) == false)
                    continue;

                item.gameObject.SetActive(true);
                rewardComps.Add(item);

                item.transform.position = data.startPos;

                if (m_actionData.isStartPunch)
                {
                    Vector3 targetPos = m_actionData.isTargetPunch
                        ? m_actionData.posTargetPunch
                        : GetPositionStartPunch(data.startPos);

                    item.transform.DOMove(targetPos, m_actionData.durationInstantiate).SetEase(Ease.OutCubic).Forget();
                }
            }
        }

        await UniTask.WaitForSeconds(m_actionData.durationInstantiate);

        if (m_actionData.durationWait > 0)
        {
            // 생성하고 조금 기다려주자
            await UniTask.WaitForSeconds(m_actionData.durationWait);
            await UniTask.WaitUntil(() => isSwitchReceive == true);
        }

        //목적지까지 날려주자
        List<UniTask> tasks = new();
        for (int i = 0; i < rewardComps.Count; i++)
            tasks.Add(rewardComps[i].ThrowStart(m_actionData.durationMove));
        await UniTask.WhenAll(tasks.ToArray());
    }

    public Vector3 GetPositionStartPunch(Vector3 _startPos)
    {
        // Vector3 lookAt = (Vector3)UnityEngine.Random.insideUnitCircle.normalized;

        Vector3 lookAt = _startPos;
        float percentMax = 1f;

        if (UnityEngine.Random.value > .5f)
        {
            lookAt.x = UnityEngine.Random.value > .5f ? -2 : 2;
            lookAt.y = UnityEngine.Random.Range(-1f, 3f);
        }
        else
        {
            lookAt.x = UnityEngine.Random.Range(-2f, 2f);
            lookAt.y -= .5f;
            percentMax = .8f;
        }

        Vector3 dist = lookAt.normalized * UnityEngine.Random.Range(m_actionData.distInstantiateMIN * percentMax, m_actionData.distInstantiateMAX * percentMax);

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
        // public float durationThrow;     // 던지기전에 기다리는 시간
        public RewardSpawnType spawnType; // 필드에서 생성? 스크린?? 팝업??

        public void SetDefault()
        {
            durationWait = -1;
            durationInstantiate = 0.2f;
            durationMove = 0.5f;

            distInstantiateMAX = 3f;
            distInstantiateMIN = 1f;

            isStartPunch = true;

            spawnType = RewardSpawnType.UI_Front;
        }
    }

    public class RewardData
    {
        public List<RewardItemData> rewards;
        public Vector3 startPos;
    }

    public class RewardItemData
    {
        public ItemType itemType;
        public long count;

        public RewardItemData(ItemType _itemType, long _count = 1)
        {
            itemType = _itemType;
            count = _count;
            //  spawnType = _spawnType;
        }

        public bool isCurrency => itemType == ItemType.Gold || itemType == ItemType.Rice;

        public string name => TableManager.stringTable.GetString($"ITEM_NAME_{itemType.ToString().ToUpper()}");
    }

    private void OnValidate()
    {
        
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

    public static void OpenRewardPopup(params ItemData[] _items)
        => OpenRewardPopupAsync(_items).Forget();
    public static async UniTask<PopupRewardComponent> OpenRewardPopupAsync(params ItemData[] _items)
    {
        List<ItemData> rewards = new();
        foreach (var i in _items)
            rewards.Add(i);

        return await PopupManager.instance.OpenPopupAsync<PopupRewardComponent>(PopupType.Reward, rewards);
    }
}
