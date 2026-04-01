using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyScreen_Hero : LobbyScreen_Base
{
    PopupHeroFilter m_popupFilter;
    PopupHeroSort m_popupSort;
    PopupHeroInfo m_popupHeroInfo;

    List<HeroIconComponent> m_itemBatch = new();
    List<HeroIconComponent> m_itemList = new();

    List<HeroInfoData> m_myHero = new();

    int m_curIndex_Batch = -1;
    int m_curIndex_List = -1;

    List<string> m_openHeroSkins = new();

    TeamPositionType m_teamPosition;

    protected override void Awake()
    {
        base.Awake();

        m_element.btn_filter.onClick.AddListener(
            async () =>
            {
                if (m_popupFilter == null)
                    m_popupFilter = await PopupManager.instance.OpenPopup<PopupHeroFilter>(PopupType.Hero_Filter);
                else
                    m_popupFilter.OpenPopup();

                await UniTask.WaitUntil(() => m_popupFilter.gameObject.activeSelf == false);

                if (m_popupFilter.isNeedUpdate)
                    SetLayout_List();
            });

        m_element.btn_sort.onClick.AddListener(
            async () =>
            {
                if (m_popupSort == null)
                    m_popupSort = await PopupManager.instance.OpenPopup<PopupHeroSort>(PopupType.Hero_Sort);
                else
                    m_popupSort.OpenPopup();

                await UniTask.WaitUntil(() => m_popupSort.gameObject.activeSelf == false);

                if (m_popupSort.isNeedUpdate)
                    SetLayout_List();
            });

        m_element.btn_mainPosition.onClick.AddListener(OnButton_TeamPosition);

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

    protected override void OnEnable()
    {
        m_openHeroSkins = DataManager.userInfo.myHero.Where(x => x.isBatch).Select(x => x.skin).ToList();

        if (m_isNeedUpdateLayout)
        {
            m_myHero.Clear();
            m_myHero.AddRange(DataManager.userInfo.myHero);
            SetLayout_Batch();
            SetLayout_List();
        }

        m_teamPosition = DataManager.option.mainTeamPosition;

        base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (m_popupFilter != null)
            Destroy(m_popupFilter.gameObject);
        if (m_popupSort != null)
            Destroy(m_popupSort.gameObject);
        if (m_popupHeroInfo != null)
            Destroy(m_popupHeroInfo.gameObject);

        base.OnDisable();
    }

    private void Start()
    {
        m_teamPosition = m_teamPosition == TeamPositionType.Front ? TeamPositionType.Back : TeamPositionType.Front;
        OnButton_TeamPosition();

        m_myHero.AddRange(DataManager.userInfo.myHero);
        SetLayout_Batch();

        // 리스트 아이콘 미리 생성
        {
            var scroll = m_element.list.layout.GetComponent<ScrollRect>();
            var baseItem = scroll.content.GetChild(0).GetComponent<HeroIconComponent>();
            baseItem.transform.SetParent(scroll.viewport);
            while (baseItem.element.icon.childCount > 0)
                DestroyImmediate(baseItem.element.icon.GetChild(0).gameObject);

            var dbHero = TableManager.hero.list;
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
                scroll.enabled = false;
                scroll.verticalScrollbar.gameObject.SetActive(false);

                var e = baseItem.element;
                Destroy(e.txtLevel.gameObject);
                Destroy(e.txtName.gameObject);
                Destroy(e.icon.parent.gameObject);
                Destroy(e.btnAction.gameObject);
                Destroy(e.badge);
                e.btnHero.interactable = false;

                // 20개 미리 생성은 해두자
                for (; i < 20; i++)
                    Instantiate(baseItem, scroll.content);
            }

            scroll.content.ForceRebuildLayout();

            baseItem.gameObject.SetActive(false);
            Destroy(baseItem.gameObject);
        }

        SetLayout_List();
    }

    protected override bool IsCloseScreen()
    {
        if (m_popupFilter?.gameObject.activeSelf == true)
            m_popupFilter.Close();
        else if (m_popupSort?.gameObject.activeSelf == true)
            m_popupSort.Close();
        else if (m_popupHeroInfo?.gameObject.activeSelf == true)
            m_popupHeroInfo.Close();
        else
            return true;

        return false;
    }

    protected override async UniTask CloseAsync()
    {
        ResetActiveButton_List();
        for (int i = 0; i < m_itemBatch.Count; i++)
            m_itemBatch[i].SetActiveButton(false);
        m_curIndex_Batch = -1;

        await base.CloseAsync();
    }

    public override void Close(bool _isTween = true)
    {
        // SaveData
        SaveDataAsync().Forget();
        base.Close(_isTween);
    }

    bool m_isNeedUpdateLayout;
    async UniTask SaveDataAsync()
    {
        List<string> resultSkins = m_itemBatch.Where(x => x.data.isActive).Select(x => x.data.skin).ToList();
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
            MapManager.instance.FadeDimm(true, 0f);

            var heroList = m_itemList.Select(x => x.data).ToList();
            for (int i = 0; i < heroList.Count; i++)
            {
                var data = heroList[i];
                data.isMain = i == 0;
                heroList[i] = data;
            }

            DataManager.userInfo.UpdateAll(heroList);
            Signal.instance.UpdateTeamPosition.Emit();

            EffectWorker.instance.ResetEffect();

            if (TutorialManager.instance.IsComplete(TutorialType.START) == false)
            {
                await TeamManager.instance.SpawnUpdateAsync();

                DataManager.userInfo.SortTeamPosition(TeamManager.instance.members.Select(x => x.Value.info).ToList());

                TeamManager.instance.RepositionToMain(0, true);

                MapManager.instance.FadeDimm(false);
                return;
            }

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
        m_element.txt_mainPosition.text = m_teamPosition == TeamPositionType.Front ? "전열" : "후열";

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

    public async UniTask OpenHeroInfoPopup(HeroInfoData _data)
    {
        if (m_popupHeroInfo == null)
            m_popupHeroInfo = await PopupManager.instance.OpenPopup<PopupHeroInfo>(PopupType.Hero_HeroInfo);

        await m_popupHeroInfo.SetHeroInfoDataAsync(_data);

        await UniTask.WaitUntil(() => m_popupHeroInfo.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

        if (m_popupHeroInfo.isNeedUpdate)
            SetLayout_List(DataManager.userInfo.GetHeroInfoData(_data.key));
    }

    #region BATCH
    void SetLayout_Batch()
    {
        var db = m_myHero.Where(x => x.isBatch).ToList();

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
            if (m_curIndex_Batch > -1)
            {
                OnButton_BatchHeroRemove(_item);
                ResetActiveButton_Batch();
            }
            else
            {
                if (m_itemBatch.Count(x => x.data.isActive) == 1)
                    return;

                ResetActiveButton_Batch();

                OnButton_BatchHeroRemove(_item);
                ResetActiveButton_List();
            }

            return;
        }

        //if (_item.data.isMain == true && StageManager.instance.isClearFirstStage == false)
        //{
        //    PopupManager.instance.AlertShow("일반난이도를_클리어한_후\n주장_교체_가능합니다.");
        //    return;
        //}

        if (m_itemBatch.Count(x => x.data.isActive) == 1)
            return;

        var index = m_itemBatch.FindIndex(x => x == _item);

        // 같은 영웅을 클릭한 거라면, 꺼주자
        if (m_curIndex_Batch == index)
            m_curIndex_Batch = -1;
        else
            m_curIndex_Batch = index;

        // 리스트에서 눌린게 있다면 다 꺼주자
        if (m_curIndex_List > -1)
            ResetActiveButton_List();

        // 내꺼는 삭제, 다른걸 누르면 교체하자
        for (int i = 0; i < m_itemBatch.Count; i++)
            m_itemBatch[i].SetActiveButton(m_curIndex_Batch > -1, i != index);
    }

    void OnButton_BatchHeroRemove(HeroIconComponent _item)
    {
        //if (_item.data.isMain == true && StageManager.instance.isClearFirstStage == false)
        //{
        //    PopupManager.instance.AlertShow("일반난이도를_클리어한_후\n주장_교체_가능합니다.");
        //    return;
        //}

        var index = m_itemBatch.FindIndex(x => x.data.key == _item.data.key);

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

            UpdateHeroData(prevBatchHero, true);

            var data = m_itemList[m_curIndex_List].data;
            data.isBatch = true;
            UpdateHeroData(data, true);

            m_myHero.AddRange(last);

            m_itemList[m_curIndex_List].SetActiveButton(false);
            //m_curIndex_List = -1;

            SetLayout_Batch();
            SetLayout_List();
        }
        else if (m_curIndex_Batch > -1 &&
            m_itemBatch[m_curIndex_Batch] != _item &&
            (m_curIndex_Batch != m_curIndex_List || m_curIndex_List != -1))
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
            var data = m_itemList[index].data;
            data.isBatch = false;
            UpdateHeroData(data, true);

            SetLayout_Batch();
            //m_curIndex_List = m_curIndex_Batch = -1;

            SetLayout_List();
        }

        for (int i = 0; i < m_itemBatch.Count; i++)
            m_itemBatch[i].SetActiveButton(false);

        for (int i = 0; i < m_itemList.Count; i++)
            m_itemList[i].SetActiveButton(false);
        //m_curIndex_Batch = -1;
        m_curIndex_List = m_curIndex_Batch = -1;
    }

    void ResetActiveButton_Batch()
    {
        if (m_curIndex_Batch > -1)
        {
            m_itemList[m_curIndex_Batch].SetActiveButton(false);
            m_curIndex_Batch = -1;
        }
    }
    #endregion BATCH

    #region LIST
    void SetLayout_List(HeroInfoData _updateInfoData = default)
    {
        // 업데이트 정보가 필요하다면
        if (_updateInfoData.isActive)
        {
            var indexList = m_itemList.FindIndex(x => x.data.key == _updateInfoData.key);
            if (indexList > -1)
                m_itemList[indexList].UpdateHeroInfo(_updateInfoData);
        }

        //보유 미보유 전체
        bool isAll = true;
        var db = m_itemList.OrderByDescending(x => x.data.isMine || isAll);

        //정렬

        // 배치된 유저가 앞으로 오기
        var dbNotBatch = db.Where(x => x.data.isBatch == false).ToList();
        var dbBatch = db.Where(x => x.data.isBatch).ToList();
        m_itemList.Clear();
        for (int i = 0; i < m_itemBatch.Count; i++)
        {
            var batchData = m_itemBatch[i];
            if (batchData.data.isActive == false)
                continue;

            m_itemList.Add(dbBatch.Find(x => x.data.key == batchData.data.key));
        }
        m_itemList.AddRange(dbNotBatch);

        for (int i = m_itemList.Count - 1; i > -1; i--)
            m_itemList[i].transform.SetAsFirstSibling();
    }

    void ResetActiveButton_List()
    {
        if (m_curIndex_List > -1)
        {
            m_itemList[m_curIndex_List].SetActiveButton(false);
            m_curIndex_List = -1;
        }
    }

    void OnButton_ListHero(HeroIconComponent _item, bool _isRightClick)
    {
        if (_isRightClick)
        {
            // 이미 출진 중이라면?
            if (_item.data.isBatch == true)
            {
                if (m_itemBatch.Count(x => x.data.isActive) == 1)
                {
                    if (m_curIndex_List > -1)
                    {
                        m_itemList[m_curIndex_List].SetActiveButton(false);
                        m_curIndex_List = -1;
                    }
                    return;
                }

                OnButton_ListHeroRemove(_item);
                return;
            }
            // 빈공간이 있으면?
            else if (m_itemBatch.Any(x => x.data.isActive == false))
            {
                ResetActiveButton_Batch();
                ResetActiveButton_List();

                if (_item.data.isMine == true)
                    OnButton_ListHeroRemove(_item);
                return;
            }
        }

        var index = _item.transform.GetSiblingIndex();

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

        if (m_itemBatch.Count(x => x.data.isActive) > 0)
        {
            for (int i = 0; i < m_itemBatch.Count; i++)
            {
                bool isSelf = _item.data.key.Equals(m_itemBatch[i].data.key);
                m_itemBatch[i].SetActiveButton(m_curIndex_List > -1 && m_itemBatch[i].data.isActive,
                    isSelf == false);

                if (isSelf == true)
                    m_curIndex_Batch = i;
            }
        }
        else
            m_curIndex_Batch = -1;
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
            m_curIndex_List = -1;

            SetLayout_Batch();
            SetLayout_List();
        }
    }
    #endregion LIST

#if UNITY_EDITOR
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_element.Initialize(transform);
    }
#endif

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;
    [Serializable]
    struct ElementData
    {
        public Button btn_filter;
        public Button btn_sort;
        public Button btn_mainPosition;

        public TextMeshProUGUI txt_mainPosition;

        public LayoutData batch;
        public LayoutData list;

        public void Initialize(Transform _transform)
        {
            btn_filter = _transform.GetComponent<Button>("Panel/List/btn_filter");
            btn_sort = _transform.GetComponent<Button>("Panel/List/btn_sort");
            btn_mainPosition = _transform.GetComponent<Button>("Panel/Batch/btn_position");

            txt_mainPosition = btn_mainPosition.GetComponentInChildren<TextMeshProUGUI>();

            batch.Initialize(_transform, "Batch");
            list.Initialize(_transform, "List");
        }
    }

    [Serializable]
    struct LayoutData
    {
        public Transform panel;
        public Transform layout;

        public TextMeshProUGUI title;

        public void Initialize(Transform _transform, string _name)
        {
            panel = _transform.Find("Panel/" + _name);
            title = panel.GetComponent<TextMeshProUGUI>("txt_title");
            layout = panel.Find("Layout");
        }
    }
}
