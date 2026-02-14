using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyScreen_Hero : LobbyScreen_Base
{
    PopupHeroFilter m_popup;

    List<HeroIconComponent> m_itemBatch = new();
    List<HeroIconComponent> m_itemList = new();

    List<HeroInfoData> m_myHero = new();

    int m_curIndex_Batch = -1;
    int m_curIndex_List = -1;

    protected override void Awake()
    {
        base.Awake();

        m_element.btn_filter.onClick.AddListener(
            async () =>
            {
                m_popup = await PopupManager.instance.OpenPopupAndWait<PopupHeroFilter>(PopupType.Hero_Filter);
            });

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

    private void Start()
    {
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
                DestroyImmediate(e.txtLevel.gameObject);
                DestroyImmediate(e.txtName.gameObject);
                DestroyImmediate(e.icon.parent.gameObject);
                DestroyImmediate(e.btnAction.gameObject);
                DestroyImmediate(e.batch);
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

    protected override async UniTask CloseAsync()
    {
        ResetActiveButton_List();
        for (int i = 0; i < m_itemBatch.Count; i++)
            m_itemBatch[i].SetActiveButton(false);
        m_curIndex_Batch = -1;

        await base.CloseAsync();
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

    void OnButton_BatchHero(HeroIconComponent _item)
    {
        if (m_itemBatch.Count(x => x.data.isActive) == 1)
            return;

        var index = m_itemBatch.FindIndex(x => x == _item);

        if (m_curIndex_Batch == index)
        {
            m_itemBatch[m_curIndex_Batch].SetActiveButton(false);
            m_curIndex_Batch = -1;
            return;
        }
        else if (m_curIndex_Batch > -1)
            m_itemBatch[m_curIndex_Batch].SetActiveButton(false);

        m_curIndex_Batch = index;

        if (m_curIndex_List > -1)
        {
            for (int i = 0; i < m_itemBatch.Count; i++)
                m_itemBatch[i].SetActiveButton(index == i);

            ResetActiveButton_List();
        }
        else
            m_itemBatch[index].SetActiveButton(true);
    }

    void OnButton_BatchHeroRemove(HeroIconComponent _item)
    {
        var index = m_itemBatch.FindIndex(x => x.data.key == _item.data.key);

        //교환하는 경우
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

            SetLayout_Batch();
            SetLayout_List();

            for (int i = 0; i < m_itemBatch.Count; i++)
                m_itemBatch[i].SetActiveButton(false);
            m_curIndex_List = -1;
        }
        else
        {
            var data = m_itemList[index].data;
            data.isBatch = false;
            UpdateHeroData(data, true);

            SetLayout_Batch();
            m_curIndex_Batch = -1;

            ResetActiveButton_List();
            SetLayout_List();
        }
    }
    #endregion BATCH

    #region LIST
    void SetLayout_List()
    {
        //보유 미보유 전체
        bool isAll = true;
        var db = m_itemList.OrderByDescending(x => x.data.isMine || isAll);

        //정렬


        // 배치
        bool isBatch = true;
        if (isBatch)
        {
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
        }
        else
            m_itemList = db.ToList();

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

    void OnButton_ListHero(HeroIconComponent _item)
    {
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

        for (int i = 0; i < m_itemBatch.Count; i++)
            m_itemBatch[i].SetActiveButton(m_curIndex_List > -1 && m_itemBatch[i].data.isActive && _item.data.isBatch == false);

        m_curIndex_Batch = -1;
    }

    void OnButton_ListHeroRemove(HeroIconComponent _item)
    {
        var index = _item.transform.GetSiblingIndex();

        if (_item.data.isBatch)
        {
            OnButton_BatchHeroRemove(_item);
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

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;
    [Serializable]
    struct ElementData
    {
        public Button btn_filter;
        public Button btn_mainPosition;

        public LayoutData batch;
        public LayoutData list;

        public void Initialize(Transform _transform)
        {
            btn_filter = _transform.GetComponent<Button>("Panel/List/btn_filter");
            btn_mainPosition = _transform.GetComponent<Button>("Panel/Batch/btn_position");

            batch.Initialize(_transform, "Batch");
            list.Initialize(_transform, "List");
        }
    }

    [Serializable]
    struct LayoutData
    {
        public Transform panel;
        public Transform layout;

        public Text title;

        public void Initialize(Transform _transform, string _name)
        {
            panel = _transform.Find("Panel/" + _name);
            title = panel.GetComponent<Text>("txt_title");
            layout = panel.Find("Layout");
        }
    }
}
