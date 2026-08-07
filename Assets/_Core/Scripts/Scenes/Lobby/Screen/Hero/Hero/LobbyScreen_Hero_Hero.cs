using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyScreen_Hero_Hero : LobbyScreen_Hero_TabBase, IValidatable
{
    protected PopupHeroFilter m_popupFilter;
    PopupHeroInfo m_popupHeroInfo;

    List<HeroIconComponent> m_itemBatch = new();
    List<HeroIconComponent> m_itemList = new();

    protected List<HeroInfoData> m_myHero = new();

    int m_curIndex_Batch = -1;
    int m_curIndex_List = -1;

    List<string> m_openHeroSkins = new();

    TeamPositionType m_teamPosition;

    bool m_isNeedUpdateLayout;

    protected override void Awake()
    {
        tabType = LobbyScreen_Hero.HeroTabType.Hero;

        m_element.btnFilter.onClick.AddListener(
            async () =>
            {
                if (m_popupFilter == null)
                    m_popupFilter = await PopupManager.instance.OpenPopupAsync<PopupHeroFilter>(PopupType.Hero_Filter);
                else
                    m_popupFilter.OpenPopup();

                SetFilterSize();

                await UniTask.WaitUntil(() => m_popupFilter.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

                if (m_popupFilter.isNeedUpdate)
                {
                    SetLayout_List();
                    m_element.scroll.content.anchoredPosition = Vector2.zero;
                }
            });


        UnityAction action = () =>
        {
            var scale = m_element.imgSort.rectTransform.localScale;
            scale.y = DataManager.userInfo.sortData.isDescending ? -1 : 1;
            m_element.imgSort.rectTransform.localScale = scale;
        };
        action();

        m_element.btnSort.onClick.AddListener(
            () =>
            {
                var sortData = DataManager.userInfo.sortData;
                DataManager.userInfo.SetSortingData(sortData.sortType, sortData.isDescending == false);
                SetLayout_List();

                action();
            });

        if (m_element.btnMainPosition != null)
        {
            m_element.btnMainPosition.onClick.AddListener(OnButton_TeamPosition);

            // 출정 중 히어로 세팅
            {
                var panel = m_element.batch.layout;
                for (int i = 0; i < panel.childCount; i++)
                {
                    var hero = panel.GetChild(i).GetComponent<HeroIconComponent>();
                    if (hero != null)
                        m_itemBatch.Add(hero);
                }
            }
        }

        Signal.instance.UpdateHeroStat.connectLambda = new(this, _
            => m_isNeedUpdateLayout = true);
    }

    protected virtual void Start()
    {
        m_myHero.Clear();
        m_myHero.AddRange(DataManager.userInfo.myHero);

        m_teamPosition = m_teamPosition == TeamPositionType.Front ? TeamPositionType.Back : TeamPositionType.Front;
        OnButton_TeamPosition();

        SetLayout_Batch();

        // 리스트 아이콘 미리 생성
        InstantiateList();

        m_isStarted = true;
        SetLayout_List();
    }

    protected void InstantiateList()
    {
        var scroll = m_element.scroll;
        var baseItem = scroll.content.GetChild(0).GetComponent<HeroIconComponent>();
        baseItem.transform.SetParent(scroll.viewport);
        while (baseItem.element.icon.childCount > 0)
            DestroyImmediate(baseItem.element.icon.GetChild(0).gameObject);

        var dbHero = TableManager.hero.GetHeroList();
        int i = 0;
        for (; i < dbHero.Count; i++)
        {
            m_itemList.Add(Instantiate(baseItem, scroll.content));
            var heroInfo = DataManager.userInfo.GetHeroInfoData(dbHero[i].key);

            if (heroInfo.isActive == false)
                heroInfo = new(dbHero[i].key, _isMine: false);

            m_itemList[i].name = dbHero[i].key;
            m_itemList[i].SetHeroData(heroInfo, OnButton_ListHero, OnButton_ListHeroRemove);
        }

        if (i < 20)
        {
            scroll.verticalScrollbar.gameObject.SetActive(false);

            baseItem.element.panel.gameObject.SetActive(false);
            baseItem.element.btnHero.interactable = false;

            // 20개 미리 생성은 해두자
            for (; i < 20; i++)
                Instantiate(baseItem, scroll.content);
        }

        scroll.content.ForceRebuildLayout();

        baseItem.gameObject.SetActive(false);
        Destroy(baseItem.gameObject);
    }

    protected bool m_isStarted = false;

    protected virtual void OnEnable()
    {
        m_openHeroSkins = DataManager.userInfo.myHero.Where(x => x.isBatch).Select(x => x.skin).ToList();

        if (m_isNeedUpdateLayout && m_isStarted == true)
        {
            List<HeroInfoData> myHero = new();
            myHero.AddRange(m_myHero);

            // 이 창에서 없는 영웅들을 추가해줘야.
            // 열때마다 계산하면 편하지만, 연산을 줄이기 위함
            m_myHero.Clear();
            for (int i = 0; i < myHero.Count; i++)
                m_myHero.Add(DataManager.userInfo.GetHeroInfoData(myHero[i].key));
            for (int i = 0; i < DataManager.userInfo.myHero.Count; i++)
            {
                var hero = DataManager.userInfo.myHero[i];
                if (m_myHero.FindIndex(x => x.key == hero.key) == -1)
                    m_myHero.Add(hero);
            }

            for (int i = 0; i < m_myHero.Count; i++)
            {
                var itemBatch = m_itemBatch.Find(x => x.data.key == m_myHero[i].key);
                itemBatch?.UpdateHeroInfo(m_myHero[i]);

                var itemList = m_itemList.Find(x => x.data.key == m_myHero[i].key);
                itemList.UpdateHeroInfo(m_myHero[i]);
            }

            SetLayout_Batch();
            SetLayout_List();
        }

        m_teamPosition = DataManager.option.mainTeamPosition;
    }

    protected void OnDisable()
    {
        if (m_popupFilter != null)
            Destroy(m_popupFilter.gameObject);
        if (m_popupHeroInfo != null)
            Destroy(m_popupHeroInfo.gameObject);

        m_popupHeroInfo = null; m_popupFilter = null;
    }

    public override bool IsCloseScreen()
    {
        if (gameObject.activeSelf == true)
        {
            if (m_popupFilter?.gameObject.activeSelf == true ||
                m_popupHeroInfo?.gameObject.activeSelf == true)
                return false;
            else
                return true;
        }

        return true;
    }

    public override async UniTask CloseAsync()
    {
        if (gameObject.activeSelf == false)
            return;

        ResetActiveButton_List();
        for (int i = 0; i < m_itemBatch.Count; i++)
            m_itemBatch[i].SetActiveButton(false);
        m_curIndex_Batch = -1;

        await UniTask.Yield();
    }

    public async UniTask SaveDataAsync()
    {
        List<string> resultSkins = m_itemBatch.FindAll(x => x.data.isActive).Select(x => x.data.skin).ToList();
        m_isNeedUpdateLayout = m_openHeroSkins.Count != resultSkins.Count;

        if (m_isNeedUpdateLayout == false)
        {
            for (int i = 0; i < m_openHeroSkins.Count; i++)
            {
                if (m_openHeroSkins[i] != resultSkins[i])
                {
                    m_isNeedUpdateLayout = true;
                    break;
                }
            }

            m_isNeedUpdateLayout = m_isNeedUpdateLayout || m_teamPosition != DataManager.option.mainTeamPosition;
        }

        if (m_isNeedUpdateLayout)
        {
            m_isNeedUpdateLayout = false;
            MapManager.instance.FadeDimm(true, 0f);

            //var heroList = m_itemBatch.FindAll(x => x.data.isActive == true).Select(x => x.data).ToList();
            var heroList = m_itemList.FindAll(x => x.data.isMine == true).Select(x => x.data).ToList();
            for (int i = 0; i < heroList.Count; i++)
            {
                var data = heroList[i];
                data.isMain = data.key == m_itemBatch[0].data.key;
                heroList[i] = data;
            }

            DataManager.userInfo.UpdateAll(heroList);
            Signal.instance.UpdateTeamPosition.Emit();

            EffectWorker.instance.ResetEffect();

            //if (TutorialManager.instance.IsComplete(GuideQuestType.START) == false)
            //{
            //    await TeamManager.instance.SpawnUpdateAsync();

            //    DataManager.userInfo.SortTeamPosition(TeamManager.instance.members.Select(x => x.Value.info).ToList());

            //    TeamManager.instance.RepositionToMain(0, true);

            //    MapManager.instance.FadeDimm(false);
            //    return;
            //}

            TeamManager.instance.SetState(CharacterStateType.None);
            StageManager.instance.SetState(CharacterStateType.None);

            DataManager.option.mainTeamPosition = m_teamPosition;

            await TeamManager.instance.SpawnUpdateAsync();

            DataManager.userInfo.SortTeamPosition(TeamManager.instance.members.Select(x => x.Value.info).ToList());

            TeamManager.instance.RepositionToMain(0, true);
            StageManager.instance.RestartStage();
        }
    }

    void OnButton_TeamPosition()
    {
        m_teamPosition = m_teamPosition == TeamPositionType.Front ? TeamPositionType.Back : TeamPositionType.Front;
        m_element.txtMainPosition.text = m_teamPosition == TeamPositionType.Front ? "전열" : "후열";

        var line = m_itemBatch[0].transform.parent.Find("Line");
        if (m_teamPosition == TeamPositionType.Front)
        {
            line.SetAsFirstSibling();
            m_itemBatch[0].transform.SetAsFirstSibling();
        }
        else
        {
            line.SetAsLastSibling();
            m_itemBatch[0].transform.SetAsLastSibling();
        }

        SetLayout_List();
    }

    void UpdateHeroData(HeroInfoData _data, bool _isLast = false)
    {
        var indexDB = m_myHero.FindIndex(x => x.key == _data.key);
        bool isNeedUpdate = m_myHero[indexDB].isBatch == _data.isBatch;

        //배치면 뒤로 보내주자
        if (_isLast && indexDB < m_myHero.Count - 1)
        {
            m_myHero.RemoveAt(indexDB);
            m_myHero.Add(_data);
        }
        else
            m_myHero[indexDB] = _data;

        m_itemList.Find(x => x.data.key == _data.key).UpdateHeroInfo(_data);
    }

    public async UniTask OpenHeroInfoPopupAsync(HeroInfoData _data)
    {
        if (m_popupHeroInfo == null)
        {
            m_popupHeroInfo = await PopupManager.instance.OpenPopupAsync<PopupHeroInfo>(PopupType.Hero_HeroInfo, _data);
            m_popupHeroInfo.isDontDestroy = true;
        }
        else
            await m_popupHeroInfo.SetHeroInfoDataAsync(_data);

        await UniTask.WaitUntil(() => m_popupHeroInfo.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

        ResetActiveButton_Batch();
        ResetActiveButton_List();

        if (m_popupHeroInfo.isNeedUpdate)
            OnEnable();
        //SetLayout_List(DataManager.userInfo.GetHeroInfoData(_data.key));
    }

    #region BATCH
    void SetLayout_Batch()
    {
        var db = m_myHero.FindAll(x => x.isBatch).ToList();

        int i = 0;
        for (; i < db.Count; i++)
            m_itemBatch[i]
                .SetHeroData(db[i], OnButton_BatchHero, OnButton_BatchHeroRemove);

        for (; i < m_itemBatch.Count; i++)
            m_itemBatch[i].Disable();
    }

    void OnButton_BatchHero(HeroIconComponent _item, bool _isRightClick)
    {
        if (_isRightClick)
        {
            ResetActiveButton_Batch();
            ResetActiveButton_List();

            OnButton_BatchHeroRemove(_item);

            //// 다른거 선택한게 없다면
            //if (m_curIndex_Batch > -1)
            //{
            //    OnButton_BatchHeroRemove(_item);
            //    ResetActiveButton_Batch();
            //}
            //else
            //{
            //    //하나 밖에 없는데 리스트도 선택 없었다면 해제인데, 한명은 무조건 있어야 해
            //    if (m_itemBatch.Count(x => x.data.isActive) == 1 && m_curIndex_List <= 0)
            //        return;

            //    m_curIndex_Batch = m_curIndex_List;

            //    OnButton_BatchHeroRemove(_item);

            //    ResetActiveButton_Batch();
            //    ResetActiveButton_List();
            //}

            return;
        }

        // 리스트에서 눌린게 있다면 다 꺼주자
        ResetActiveButton_List();

        //if (_item.data.isMain == true && StageManager.instance.isClearFirstStage == false)
        //{
        //    PopupManager.instance.AlertShow("일반난이도를_클리어한_후\n주장_교체_가능합니다.");
        //    return;
        //}

        if (m_itemBatch.Count(x => x.data.isActive) > 1)
        {
            var index = m_itemBatch.FindIndex(x => x == _item);

            // 같은 영웅을 클릭한 거라면, 꺼주자
            if (m_curIndex_Batch == index)
                m_curIndex_Batch = -1;
            else
                m_curIndex_Batch = index;

            // 내꺼는 삭제, 다른걸 누르면 교체하자
            for (int i = 0; i < m_itemBatch.Count; i++)
                m_itemBatch[i].SetActiveButton(m_curIndex_Batch > -1, i != index);
        }
        else
        {
            m_itemBatch[0].SetActiveButton(false);
        }
    }

    void OnButton_BatchHeroRemove(HeroIconComponent _item)
    {
        // 리스트와 교환하는 경우
        if (m_curIndex_List > -1 && m_itemList[m_curIndex_List].data.isBatch == false)
        {
            var prevBatchHero = _item.data;
            prevBatchHero.isBatch = false;

            //뒤에 있는배치 영웅을 뒤로 가게 해야 해
            List<HeroInfoData> last = new();
            var indexDbPrev = m_myHero.FindIndex(x => x.key == _item.data.key);
            for (int i = indexDbPrev + 1; i < m_myHero.Count; i++)
            {
                if (m_myHero[i].isBatch == true)
                {
                    last.Add(m_myHero[i]);
                    m_myHero.RemoveAt(i);
                    i--;
                }
            }

            if (prevBatchHero.isActive == true)
                UpdateHeroData(prevBatchHero, true);

            var data = m_itemList[m_curIndex_List].data;
            data.isBatch = true;
            UpdateHeroData(data, true);

            m_myHero.AddRange(last);

            m_itemList[m_curIndex_List].SetActiveButton(false);

            SetLayout_Batch();
            SetLayout_List();
        }
        else
        {
            int idxBatchFromList = m_curIndex_List == -1 ? -1
                : m_itemBatch.FindIndex(x => x.data.key.Equals(m_itemList[m_curIndex_List].data.key));

            if (m_curIndex_Batch > -1 &&
                m_itemBatch[m_curIndex_Batch].data.key != _item.data.key &&
                (m_curIndex_Batch != idxBatchFromList || idxBatchFromList != -1))
            {
                int prevIndex = m_myHero.FindIndex(x => x.key == m_itemBatch[m_curIndex_Batch].data.key);
                int nowIndex = m_myHero.FindIndex(x => x.key == _item.data.key);

                var temp = m_myHero[prevIndex];
                m_myHero[prevIndex] = m_myHero[nowIndex];
                m_myHero[nowIndex] = temp;

                SetLayout_Batch();
                SetLayout_List();
            }
            else
            {
                if (m_itemBatch.Count(x => x.data.isMine) > 1)
                {
                    var index = m_itemBatch.FindIndex(x => x.data.key == _item.data.key);
                    var data = m_itemBatch[index].data;
                    data.isBatch = false;
                    UpdateHeroData(data, true);

                    SetLayout_Batch();
                    SetLayout_List();
                }
            }
        }

        ResetActiveButton_Batch();
        ResetActiveButton_List();
    }

    void ResetActiveButton_Batch()
    {
        for (int i = 0; i < m_itemBatch.Count; i++)
            m_itemBatch[i].SetActiveButton(false);
        m_curIndex_Batch = -1;
    }
    #endregion BATCH

    #region LIST
    protected void SetLayout_List(HeroInfoData _updateInfoData = default)
    {
        if (m_isStarted == false)
            return;

        // 업데이트 정보가 필요하다면
        if (_updateInfoData.isActive)
        {
            var indexList = m_itemList.FindIndex(x => x.data.key == _updateInfoData.key);
            if (indexList > -1)
                m_itemList[indexList].UpdateHeroInfo(_updateInfoData);
        }

        var mainTeamPos = DataManager.option.mainTeamPosition;
        if (mainTeamPos != m_teamPosition)
            DataManager.option.mainTeamPosition = m_teamPosition;

        var sortData = DataManager.userInfo.GetHeroSortData(m_myHero);

        if (mainTeamPos != m_teamPosition)
            DataManager.option.mainTeamPosition = mainTeamPos;

        int i = 0;
        for (; i < sortData.Count; i++)
        {
            int idx = m_itemList.FindIndex(x => x.data.key == sortData[i].key);
            if (idx == -1)
                continue;

            m_itemList[idx].transform.SetSiblingIndex(i);
            m_itemList[idx].element.panel.gameObject.SetActive(true);
        }

        var parent = m_element.scroll.content;
        for (; i < m_itemList.Count; i++)
        {
            var idx = sortData.FindIndex(x => x.key == m_itemList[i].data.key);
            if (idx == -1)
                m_itemList[i].element.panel.gameObject.SetActive(false);
        }

        //var orderMap = sortData
        //    .Select((_data, _index) => new { _data, _index })
        //    .ToDictionary(x => x._data.key, x => x._index);

        //m_itemList = m_itemList.SortBy(x =>
        //    {
        //        string key = x.data.key;
        //        int result = orderMap.ContainsKey(key) ? orderMap[key] : int.MaxValue;
        //        return result;
        //    });

        //for (int i = m_itemList.Count - 1; i > -1; i--)
        //    m_itemList[i].transform.SetAsFirstSibling();

        ////보유 미보유 전체
        //bool isAll = true;
        //var db = m_itemList.OrderByDescending(x => x.data.isMine || isAll);

        ////정렬
        //{
        //    List<HeroInfoData> dataList = m_itemList.Select(item => item.data).ToList();

        //    dataList = DataManager.userInfo.GetHeroSortData(dataList);

        //    var dd = dataList.Select(x => x.key).ToList();
        //}

        //// 배치된 유저가 앞으로 오기
        //var dbNotBatch = db.Where(x => x.data.isBatch == false).ToList();
        //var dbBatch = db.Where(x => x.data.isBatch).ToList();
        //m_itemList.Clear();
        //for (int i = 0; i < m_itemBatch.Count; i++)
        //{
        //    var batchData = m_itemBatch[i];
        //    if (batchData.data.isActive == false)
        //        continue;

        //    m_itemList.Add(dbBatch.Find(x => x.data.key == batchData.data.key));
        //}
        //m_itemList.AddRange(dbNotBatch);

        //{
        //    List<HeroInfoData> dataList = m_itemList.Select(item => item.data).ToList();

        //    var dd = dataList.Select(x => x.key).ToList();
        //}

        //for (int i = m_itemList.Count - 1; i > -1; i--)
        //    m_itemList[i].transform.SetAsFirstSibling();
    }

    void ResetActiveButton_List()
    {
        for (int i = 0; i < m_itemList.Count; i++)
        {
            if (m_itemList[i].data.isMine == false)
                break;

            m_itemList[i].SetActiveButton(false);
        }
        m_curIndex_List = -1;
    }

    void OnButton_ListHero(HeroIconComponent _item, bool _isRightClick)
    {
        int countBatch = m_itemBatch.Count(x => x.data.isActive);

        if (_isRightClick)
        {
            ResetActiveButton_Batch();
            ResetActiveButton_List();

            int idxSbling = _item.transform.GetSiblingIndex();

            // 이미 출진 중이라면?
            if (_item.data.isBatch == true)
                OnButton_BatchHeroRemove(m_itemBatch.Find(x => x.data.isBatch == true && x.data.key == _item.data.key));
            // 빈공간이 있으면?
            else if (m_itemBatch.Any(x => x.data.isActive == false))
            {
                if (_item.data.isMine == true)
                    OnButton_ListHeroRemove(_item);
            }
            return;
        }

        ResetActiveButton_Batch();

        //var index = _item.transform.GetSiblingIndex();
        var index = m_itemList.FindIndex(x => x.data.key == _item.data.key);// _item.transform.GetSiblingIndex();

        if (m_curIndex_List != index && _item.data.isMine == true)
        {
            if (m_curIndex_List != index && m_curIndex_List > -1)
                m_itemList[m_curIndex_List].SetActiveButton(false);

            m_curIndex_List = index;

            if (_item.data.isBatch == false || m_myHero.Count(x => x.isBatch) > 1)
            {
                m_itemList[index].SetActiveButton(true);

                m_itemList[index].element.btnAction.transform.SetText("Text",
                    _item.data.isBatch ? "<size=150><color=#9A0A00>-</color></size>" : "+");
            }
        }
        else if (m_curIndex_List == index)
        {
            m_itemList[index].SetActiveButton(false);
            m_curIndex_List = -1;
        }

        //if (_item.data.isMain && StageManager.instance.isClearFirstStage == false)
        //    PopupManager.instance.AlertShow("일반난이도를_클리어한_후\n주장_교체_가능합니다.");

        if (countBatch > 1 || m_itemBatch[0].data.key != _item.data.key)
        {
            for (int i = 0; i < m_itemBatch.Count; i++)
            {
                bool isSelf = _item.data.key.Equals(m_itemBatch[i].data.key);
                m_itemBatch[i].SetActiveButton(m_curIndex_List > -1 && m_itemBatch[i].data.isActive,
                    isSelf == false);

                if (isSelf)
                    m_curIndex_Batch = i;
            }
        }
    }

    void OnButton_ListHeroRemove(HeroIconComponent _item)
    {
        var index = _item.transform.GetSiblingIndex();

        if (_item.data.isBatch)
        {
            OnButton_BatchHeroRemove(_item);
            _item.SetActiveButton(false);
            m_curIndex_List = -1;
        }
        else if (m_itemBatch.Count(x => x.data.isBatch) == m_itemBatch.Count)
        {

        }
        else
        {
            for (int i = 0; i < m_itemBatch.Count; i++)
                m_itemBatch[i].SetActiveButton(false);

            var data = _item.data;
            data.isBatch = true;

            UpdateHeroData(data, true);
            _item.SetActiveButton(false);
            ResetActiveButton_List();

            SetLayout_Batch();
            SetLayout_List();
        }
    }
    #endregion LIST

    protected virtual void SetFilterSize() { }

    public virtual void OnManualValidate()
    {
        m_element.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    //[SerializeField]
    protected ElementData m_element;
    [Serializable]
    protected struct ElementData
    {
        public Button btnFilter;
        public Button btnSort;
        public Image imgSort;
        public Button btnMainPosition;

        public TextMeshProUGUI txtMainPosition;

        public LayoutData batch;
        public LayoutData list;

        public ScrollRect scroll;

        public void Initialize(Transform _transform)
        {
            btnFilter = _transform.GetComponent<Button>("List/btn_filter");
            btnSort = _transform.GetComponent<Button>("List/btn_sort");
            imgSort = _transform.GetComponent<Image>("List/btn_sort/Image");
            btnMainPosition = _transform.GetComponent<Button>("Batch/btn_position");

            txtMainPosition = btnMainPosition?.GetComponentInChildren<TextMeshProUGUI>();

            batch.Initialize(_transform, "Batch");
            list.Initialize(_transform, "List");

            scroll = list.layout.GetComponent<ScrollRect>();
        }
    }

    [Serializable]
    protected struct LayoutData
    {
        public Transform panel;
        public Transform layout;

        public TextMeshProUGUI title;

        public void Initialize(Transform _transform, string _name)
        {
            panel = _transform.Find(_name);
            title = panel.GetComponent<TextMeshProUGUI>("txt_title");
            layout = panel.Find("Layout");
        }
    }
}
