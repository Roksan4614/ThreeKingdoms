using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Summon_Result : MonoBehaviour, IValidatable
{
    public enum ResultStepType
    {
        NONE,

        ReceiveEnd,

        MAX
    }

    bool m_isSkip = false;
    bool m_isNextStep = false;

    public ResultStepType step { get; private set; }
    public void AllSkip() => m_isSkip = true;
    public bool isSkip
    {
        get
        {
            if (m_isSkip)
                return true;
            else if (m_isNextStep || ControllerManager.isClick)
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
        step = ResultStepType.NONE;
        transform.localScale = Vector3.one;

        await UniTask.WaitForEndOfFrame();

        m_isNextStep = m_isSkip = _isSkip;
        gameObject.SetActive(true);

        await Request_Summon(_regionType, _hostKey);

        InitializePos();

        await ReceiveActionAsync();

        await SetResultDataAsync();

        m_element.SetText_btnStart("_마무리_");

        m_isNextStep = false;
        await UniTask.WaitUntil(() => m_isNextStep || ControllerManager.isClick);

        await Utils.SetActivePunchAsync(transform, false);

        gameObject.SetActive(false);
        for (int i = 0; i < 10; i++)
            m_itemComps[i].gameObject.SetActive(false);

        await UniTask.WaitUntil(() => ControllerManager.isClick == false);
        await UniTask.WaitForEndOfFrame();
        step = ResultStepType.NONE;
    }

    async UniTask Request_Summon(RegionType _regionType, string _hostKey)
    {
        List<TableItemData> result = new();

        #region 영웅 불러오기
        {
            await UniTask.WaitForEndOfFrame();
            List<TableHeroData> dbHeros = new();
            dbHeros.AddRange(TableManager.hero.list);

            // 특정 국가면 하나 더 넣자
            if (_regionType > RegionType.NONE)
                dbHeros.AddRange(TableManager.hero.list
                    .Where(x => x.regionType == _regionType && x.key.Equals(_hostKey) == false).ToList());

            int i = 0;

            if (TutorialManager.instance.IsComplete(TutorialType.START) == false)
            {
                i++;
                result.Add(new()
                {
                    key = ItemType.Stone_Soul,
                    value = _hostKey,
                    count = TableManager.hero.GetNeedSoul(GradeType.Normal)
                });

                var startHero = TableManager.region.Get(
                    TableManager.hero.Get(_hostKey).regionType).startHeroKey;

                for (; i < startHero.Length; i++)
                {
                    result.Add(new()
                    {
                        key = ItemType.Stone_Soul,
                        value = startHero[i],
                        count = 10,
                    });
                }
            }
            else
            {
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
                    while (UnityEngine.Random.value <= m_element.dbRate[i + 1] && grade < GradeType.MAX - 1)
                        grade++;

                    itemData.count = TableManager.hero.GetNeedSoul(grade);
                    result.Add(itemData);
                }
            }

            for (; i < 10; i++)
            {
                TableItemData itemData = new();
                itemData.key = UnityEngine.Random.value > 0.5f ? ItemType.Gold : ItemType.Rice;
                itemData.value = itemData.key.ToString();
                itemData.count = UnityEngine.Random.Range(1, 10) * 10;
                result.Add(itemData);
            }
        }
        #endregion 영웅 불러오기

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

        AddressableManager.instance.Load_HeroCharacterAsync(keyHero).Forget();
        await AddressableManager.instance.Load_HeroIconAsync(keyHero);
        SetItemDataAsync(result).Forget();
    }
    async UniTask SetItemDataAsync(List<TableItemData> _result)
    {
        long totalGold = 0, totalRice = 0;

        var keyItem = _result.Where(x => x.key != ItemType.Stone_Soul).Select(x => x.value).ToArray();
        await AddressableManager.instance.Load_ItemIconAsync(keyItem);

        Dictionary<string, long> resultSoul = new();
        for (int i = 0; i < _result.Count; i++)
        {
            var data = _result[i];

            if (data.key == ItemType.Gold)
                totalGold += data.count;
            else if (data.key == ItemType.Rice)
                totalRice += data.count;
            else if (data.key == ItemType.Stone_Soul)
            {
                if (resultSoul.ContainsKey(data.value))
                    resultSoul[data.value] += data.count;
                else
                {
                    data.isNew = DataManager.userInfo.GetHeroInfoData(data.value).isActive == false;
                    resultSoul.Add(data.value, data.count);
                }
            }

            m_itemComps[i].SetItemData(data);
#if UNITY_EDITOR
            m_itemComps[i].name = $"{data.value}_x{data.count}";
#endif
        }

        // 재화 데이타 저장
        DataManager.userInfo.AddAsset(totalGold, totalRice, false, false);
        foreach (var soul in resultSoul)
            DataManager.userInfo.AddHeroSoul(soul.Key, (int)soul.Value);
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
                m_prevPos.Add(m_itemComps[i].transform.localPosition);

            m_element.layout.enabled = false;
        }

        for (int i = 0; i < m_itemComps.Count; i++)
            m_itemComps[i].transform.position = m_element.pCenter.position;
    }
    async UniTask ReceiveActionAsync()
    {
        float duration = 1f;

        for (int i = m_itemComps.Count - 1; i >= 0; i--)
        {
            int idx = i;
            var item = m_itemComps[i].transform;

            item.SetParent(transform);
            if (m_isSkip == true)
            {
                item.localPosition = m_prevPos[m_prevPos.Count - idx - 1];

                var itemData = m_itemComps[idx].data;
                if (itemData.key == ItemType.Stone_Soul)
                {
                    if (DataManager.userInfo.GetHeroInfoData(itemData.value).isActive == false)
                        m_itemComps[idx].SetSoulCount(0);
                }

                m_itemComps[idx].MoveFinished();
            }
            else
            {
                if (m_itemComps[i].data.key == ItemType.Stone_Soul)
                    // 영웅 등장!!
                    await HeroActionAsync(idx);

                item.DOLocalMove(m_prevPos[m_prevPos.Count - idx - 1], duration).SetEase(Ease.InCubic)
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
        var dbHeroData = TableManager.hero.GetHeroData(itemComp.data.value);
        string key = itemComp.data.value;

        #region NEW HERO!!
        if (itemComp.data.isNew)
        {
            PopupManager.instance.AlertShow("새로운_영웅이_방문하였습니다");

            m_element.newHero.Show();

            m_element.SetText_btnStart("획득하기_");
            await AfterNextStepAsync(3f);
            m_element.SetText_btnStart("진행중_");

            PopupManager.instance.AlertDisable();

            await m_element.newHero.OutAsync();

            if (PopupManager.instance.isAlerting)
                await UniTask.WaitUntil(() => PopupManager.instance.isAlerting == false);

            await AfterNextStepAsync(.5f);
            await PopupManager.instance.AlertShowAsync($"{dbHeroData.talk}\n- {dbHeroData.name} -", -300, true, 2f);
        }
        #endregion NEW HERO!!!

        if (m_isSkip)
            return;

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

        hero.transform.localPosition += new Vector3(UnityEngine.Random.value > .5f ? 5f : -5f, 0, 0); ;
        var prevLocalPos = hero.transform.localPosition;

        if (hero.move.isFlip != hero.transform.localPosition.x < 0)
            hero.move.SetFlip(!hero.move.isFlip);

        hero.anim.AttackMotionFirstFrame();
        await hero.transform.DOLocalMoveX(0, 0.1f).SetEase(Ease.InCubic).AsyncWaitForCompletion();
        hero.anim.AttackMotionEnd();
        hero.attack.ShowSlashEffect(true);

        if (itemComp.data.isNew)
        {
            itemComp.SetSoulCount(0);

            GradeType grade = GradeType.Normal;
            var soulCount = TableManager.hero.GetNeedSoul(grade);

            m_element.SetText_btnStart("확인_하기");

            while (true)
            {
                var stringGrade = TableManager.stringHero.GetString($"GRADE_" + grade.ToString().ToUpper());

                if (grade == GradeType.Normal)
                    PopupManager.instance.AlertShow($"영웅의_등급을_확인합니다.");
                else
                    PopupManager.instance.AlertShow($"[{stringGrade}] 등급을 확인했습니다.\n한번 더 확인해 주세요.");

                grade++;
                soulCount = TableManager.hero.GetNeedSoul(grade);

                await AfterNextStepAsync(1f);

                if (itemComp.data.count <= soulCount)
                {
                    m_element.SetText_btnStart("진행_중");
                    PopupManager.instance.AlertShow($"[{stringGrade}] {dbHeroData.name.WithJosa()} 진영에 합류합니다.");
                    break;
                }

                hero.anim.Play(CharacterAnimType.Attack);
                hero.attack.ShowSlashEffect(true);

                await PopupManager.instance.AlertDisableAsync();
            }
        }
        else
            itemComp.SetSoulCount(itemComp.data.count);

        if (m_isSkip == false)
            await UniTask.WaitForSeconds(1f);

        hero.anim.Play(CharacterAnimType.Dash);
        hero.transform.DOLocalMoveX(prevLocalPos.x * -1, 0.3f).SetEase(Ease.OutCubic);
    }

    async UniTask SetResultDataAsync()
    {
        step = ResultStepType.ReceiveEnd;

        // 밖에서 호스트가 날라와서 칼질하는 시간을 벌자
        await UniTask.WaitForSeconds(.2f);

        Dictionary<ItemType, TableItemData> result = new();
        Dictionary<string, int> resultSoul = new();

        for (int i = 0; i < 10; i++)
        {
            var comp = m_itemComps[i];
            var itemData = comp.data;

            if (result.ContainsKey(comp.data.key))
            {
                var data = result[comp.data.key];
                data.count += itemData.count;
                result[itemData.key] = data;
            }
            else
                result.Add(itemData.key, itemData);

        }

        int idx = 0;
        foreach (var i in result)
        {
            RewardWorker.instance.Run(
                m_element.pHost.position + new Vector3(2f, 1f)
                , i.Key, i.Value.count, true, false, 0.5f, true
                , _isTargetPunch: true,
                _posTargetPunch: transform.position + new Vector3(
                    UnityEngine.Random.Range(0.5f, 2f) * (idx++ % 2 == 0 ? 1 : -1),
                    UnityEngine.Random.Range(4f, 6f)));

            await UniTask.WaitForSeconds(UnityEngine.Random.Range(.05f, .1f));
        }

        await UniTask.WaitForSeconds(.5f);
    }
    async UniTask WaitSkipAsync()
    {
        m_isNextStep = false;
        await UniTask.WaitUntil(() => isSkip);
    }
    void AfterNextStep(float _duration) => AfterNextStepAsync(_duration).Forget();
    async UniTask AfterNextStepAsync(float _duration)
    {
        var dt = DateTime.Now.AddSeconds(_duration);

        m_isNextStep = false;
        bool isSkipPush = isSkip;
        while (dt > DateTime.Now && isSkipPush == false)
        {
            await UniTask.WaitForEndOfFrame();
            isSkipPush = isSkip;
        }

        if (isSkipPush == false)
            m_isNextStep = true;
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
        public Transform pHost;
        public Transform pCenter;

        public List<float> dbRate;

        public NewHeroComponent newHero;

        [SerializeField] ButtonHelper btnStart;

        public void Initialize(Transform _transform)
        {
            var panelSummon = _transform.parent.parent.parent;

            baseItem = _transform.GetComponentInChildren<ItemComponent>(true);
            layout = _transform.GetComponent<GridLayoutGroup>();
            pHero = panelSummon.Find("Back_Hero/Hero");
            pCenter = _transform.parent.Find("Center");
            pHost = panelSummon.Find("Host");

            newHero = pHero.parent.GetComponent<NewHeroComponent>("NewHero");
            btnStart = panelSummon.GetComponent<ButtonHelper>("btn_start");

            SetRateValue();
        }

        public void SetText_btnStart(string _text)
            => btnStart.text = _text;

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
