using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Summon_Result : MonoBehaviour, IValidatable
{
    bool m_isSkip = false;
    bool m_isNextStep = false;

    public void AllSkip() => m_isSkip = true;
    public void NextStep() => m_isNextStep = true;
    public bool isSkip
    {
        get
        {
            if (m_isSkip)
                return true;
            else if (m_isNextStep)
            {
                m_isNextStep = false;
                return true;
            }
            return false;
        }
    }

    List<ItemComponent> m_itemComps = new();
    List<Vector3> m_prevPos;

    private void Awake()
    {
        for (int i = 0; i < 10; i++)
        {
            m_itemComps.Add(i == 0 ? m_element.baseItem : Instantiate(m_element.baseItem, transform));
            m_itemComps[i].gameObject.SetActive(false);
        }
    }

    public async UniTask StartAsync(RegionType _regionType, string _hostKey, bool _isSkip)
    {
        m_isNextStep = m_isSkip = _isSkip;
        gameObject.SetActive(true);

        var rewards = await Request_Summon(_regionType, _hostKey);

        // 문뒤 양쪽으로 움직여주기
        InitializePos();

        // 문 가운데로 이동하기
        await MoveToCenterAsync();

        // 하나씩 결과창으로 이동하기
        await OpenItemAsync();

        gameObject.SetActive(false);
    }

    async UniTask<List<TableItemData>> Request_Summon(RegionType _regionType, string _hostKey)
    {
        await UniTask.WaitForEndOfFrame();

        List<TableItemData> result = new();

        // 영웅 가져오기
        List<TableHeroData> dbHeros = new();
        dbHeros.AddRange(TableManager.hero.list);

        // 특정 국가면 하나 더 넣자
        if (_regionType > RegionType.NONE)
            dbHeros.AddRange(TableManager.hero.list
                .Where(x => x.regionType == _regionType && x.key.Equals(_hostKey) == false).ToList());

        int i = 0;

        //일단 영웅 뽑기
        for (; i < m_element.dbRate.Count; i++)
        {
            float rnd = UnityEngine.Random.Range(0, 100f);
            if (rnd >= m_element.dbRate[i])
                break;

            TableItemData itemData = new();
            itemData.key = ItemType.Stone_Soul;

            if (i == 0)
                itemData.value = _hostKey;
            else
            {
                var randomIdx = UnityEngine.Random.Range(0, dbHeros.Count);
                itemData.value = dbHeros[randomIdx].key;
                dbHeros.RemoveAt(randomIdx);
            }

            result.Add(itemData);

            m_itemComps[i].SetItemData(itemData.value, 10, true);
        }

        for (; i < 10; i++)
        {
            TableItemData itemData = new();
            itemData.key = ItemType.Gold + UnityEngine.Random.Range(0, 2);
            itemData.count = UnityEngine.Random.Range(1, 10) * 10;
            result.Add(itemData);

            m_itemComps[i].SetItemData(itemData.value, itemData.count, false);
        }

        return result;
    }

    async UniTask WaitSkipAsync() => await UniTask.WaitUntil(() => isSkip);
    void AfterNextStep(float _duration) => AfterNextStepAsync(_duration).Forget();
    async UniTask AfterNextStepAsync(float _duration)
    {
        await UniTask.WaitForSeconds(_duration);
        NextStep();
    }

    void InitializePos()
    {
        if (m_prevPos == null)
        {
            m_prevPos = new();

            m_element.layout.enabled = true;
            for (int i = 0; i < m_itemComps.Count; i++)
                m_itemComps[i].gameObject.SetActive(true);

            transform.ForceRebuildLayout();

            for (int i = 0; i < m_itemComps.Count; i++)
                m_prevPos.Add(m_itemComps[i].transform.position);

            m_element.layout.enabled = false;
        }

        for (int i = 0; i < m_itemComps.Count; i++)
        {
            m_itemComps[i].transform.position = m_element.pCenter.position;
            m_itemComps[i].rt.anchoredPosition += new Vector2(300 * (UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1), 0);

            m_itemComps[i].transform.SetParent(m_element.pBack);
        }
    }

    async UniTask MoveToCenterAsync()
    {
        await WaitSkipAsync();
    }

    async UniTask OpenItemAsync()
    {
        await WaitSkipAsync();
    }
    

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public ItemComponent baseItem;
        public GridLayoutGroup layout;

        public Transform pBack;
        public Transform pCenter;

        public List<float> dbRate;

        public void Initialize(Transform _transform)
        {
            baseItem = _transform.GetComponentInChildren<ItemComponent>();
            layout = _transform.GetComponent<GridLayoutGroup>();
            pBack = _transform.parent.parent.parent.Find("Back_Hero");
            pCenter = _transform.parent.Find("Center");

            SetRateValue();
        }

        void SetRateValue()
        {
            dbRate = new();
            float rate = 100f;
            for (int i = 0; i < 10; i++, rate *= .5f)
                dbRate.Add(rate);
        }
    }
    #endregion VALIDATA
}
