using Cysharp.Threading.Tasks;
using DG.Tweening;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Progress;

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

        for (int i = 0; i < m_element.pHero.childCount; i++)
            m_element.pHero.GetChild(i).gameObject.SetActive(false);

        m_element.newHero.gameObject.SetActive(false);
    }

    public async UniTask StartAsync(RegionType _regionType, string _hostKey, bool _isSkip)
    {
        await UniTask.WaitForEndOfFrame();

        m_isNextStep = m_isSkip = _isSkip;
        gameObject.SetActive(true);

        await Request_Summon(_regionType, _hostKey);

        // 문뒤 양쪽으로 움직여주기
        InitializePos();

        await ReceiveProduct();
    }

    public async UniTask FinishAsync()
    {
        m_isNextStep = false;
        await UniTask.WaitUntil(() => m_isNextStep);

        gameObject.SetActive(false);
    }

    async UniTask Request_Summon(RegionType _regionType, string _hostKey)
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
        for (; i < 10; i++)
        {
            if (UnityEngine.Random.value > m_element.dbRate[i])
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

            GradeType grade = GradeType.Normal;
            while (UnityEngine.Random.value <= m_element.dbRate[i + 2] && grade < GradeType.MAX - 1)
                grade++;

            itemData.count = TableManager.hero.GetNeedSoul(grade);
            result.Add(itemData);
        }

        for (; i < 10; i++)
        {
            TableItemData itemData = new();
            itemData.key = ItemType.Gold + UnityEngine.Random.Range(0, 2);
            itemData.value = itemData.key.ToString();
            itemData.count = UnityEngine.Random.Range(1, 10) * 10;
            result.Add(itemData);
        }

        // 정렬하자
        result = result
            .OrderBy(x =>
            {
                if (x.key == ItemType.Stone_Soul)
                    // 새 영웅일 경우 맨 뒤로
                    return DataManager.userInfo.GetHeroInfoData(x.value).isActive ? 2 : 1;
                else
                    return 10;
            })
            .ThenByDescending(x => x.count)
            .ThenByDescending(x => x.key == ItemType.Gold)
            .ToList();


        var keyHero = result.Where(x => x.key == ItemType.Stone_Soul).Select(x => x.value).ToArray();
        var keyItem = result.Where(x => x.key != ItemType.Stone_Soul).Select(x => x.value).ToArray();

        AddressableManager.instance.Load_HeroIconAsync(keyHero).Forget();
        AddressableManager.instance.Load_HeroCharacterAsync(keyHero).Forget();
        await AddressableManager.instance.Load_ItemIconAsync(keyItem);

        i = 0;
        for (; i < result.Count; i++)
        {
            m_itemComps[i].SetItemData(result[i]);
#if UNITY_EDITOR
            m_itemComps[i].name = $"{result[i].value}_x{result[i].count}";
#endif
        }
    }

    async UniTask WaitSkipAsync() => await UniTask.WaitUntil(() => isSkip);
    void AfterNextStep(float _duration) => AfterNextStepAsync(_duration).Forget();
    async UniTask AfterNextStepAsync(float _duration)
    {
        var dt = DateTime.Now.AddSeconds(_duration);

        bool isSkipPush = isSkip;
        while (dt > DateTime.Now && isSkipPush == false)
        {
            await UniTask.WaitForEndOfFrame();
            isSkipPush = isSkip;
        }

        if (isSkipPush == false)
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
            m_itemComps[i].transform.position = m_element.pCenter.position;
    }

    async UniTask ReceiveProduct()
    {
        float duration = 1f;

        for (int i = m_itemComps.Count - 1; i >= 0; i--)
        {
            int idx = i;
            var item = m_itemComps[i].transform;

            item.SetParent(transform);
            if (m_isSkip == true)
            {
                item.position = m_prevPos[m_prevPos.Count - idx - 1];
                m_itemComps[idx].MoveFinished();
            }
            else
            {
                if (m_itemComps[i].data.key == ItemType.Stone_Soul)
                {
                    // 영웅 등장!!
                    await HeroActionAsync(idx);

                    m_itemComps[idx].MoveFinished();
                    await AfterNextStepAsync(3f);
                }

                item.DOMove(m_prevPos[m_prevPos.Count - idx - 1], duration).SetEase(Ease.InCubic)
                    .OnComplete(() =>
                    {
                        m_itemComps[idx].MoveFinished();
                    });

                duration = Math.Max(0.1f, duration * 0.7f);
                await UniTask.WaitForSeconds(duration);
            }
        }
    }

    async UniTask HeroActionAsync(int _idx)
    {
        var itemComp = m_itemComps[_idx];
        string key = itemComp.data.value;

        CharacterComponent hero = null;
        // LOAD HERO
        {
            for (int i = 0; i < m_element.pHero.childCount; i++)
            {
                var obj = m_element.pHero.GetChild(i).gameObject;
                obj.SetActive(obj.name.Equals(key));

                if (obj.activeSelf)
                    hero = obj.GetComponent<CharacterComponent>();
            }

            if (hero == null)
            {
                var obj = await AddressableManager.instance.GetHeroCharacterAsync(key);
                if (obj != null)
                    hero = Instantiate(obj, m_element.pHero).GetComponent<CharacterComponent>();
            }

            if (hero == null)
                return;

            hero.transform.localPosition = Vector3.zero;
        }

        hero.transform.localPosition += new Vector3(UnityEngine.Random.value > 5f ? 5f : -5f, 0, 0); ;
        var prevLocalPos = hero.transform.localPosition;

        if (hero.move.isFlip == hero.transform.localPosition.x > 0)
            hero.move.SetFlip(!hero.move.isFlip);

        hero.anim.AttackMotionFirstFrame();
        await hero.transform.DOLocalMoveX(0, 0.1f).SetEase(Ease.InCubic).AsyncWaitForCompletion();
        hero.anim.AttackMotionEnd();
        hero.attack.ShowSlashEffect(true);

        GradeType grade = GradeType.Normal;

        while (true)
        {
            var soulCount = TableManager.hero.GetNeedSoul(grade);
            itemComp.SetSoulCount(soulCount);

            if (itemComp.data.count >= soulCount)
                break;

            PopupManager.instance.AlertShow("영웅이 승급을 합니다!!");

            if (m_isSkip == false)
                await UniTask.WaitUntil(() => ControllerManager.isClick);

            grade++;

            hero.anim.Play(CharacterAnimType.Attack);
            hero.attack.ShowSlashEffect(true);
        }

        if (m_isSkip == false)
        {
            await UniTask.WaitForSeconds(1f);

            hero.anim.Play(CharacterAnimType.Dash);
            hero.transform.DOLocalMoveX(prevLocalPos.x * -1, 0.3f).SetEase(Ease.OutCubic);
        }

        itemComp.MoveFinished();

        await AfterNextStepAsync(1f);
        // 등장 > 칼질 > 갯수증가 > 끝나면 아이콘 생성 후 날라가자
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

        public Transform pHero;
        public Transform pCenter;

        public List<float> dbRate;

        public NewHeroComponent newHero;

        public void Initialize(Transform _transform)
        {
            baseItem = _transform.GetComponentInChildren<ItemComponent>(true);
            layout = _transform.GetComponent<GridLayoutGroup>();
            pHero = _transform.parent.parent.parent.Find("Back_Hero/Hero");
            pCenter = _transform.parent.Find("Center");

            newHero = pHero.parent.GetComponent<NewHeroComponent>("NewHero");

            SetRateValue();
        }

        void SetRateValue()
        {
            dbRate = new();
            float rate = 1f;
            for (int i = 0; i < 11; i++, rate *= .5f)
                dbRate.Add(rate);
        }
    }
    #endregion VALIDATA
}
