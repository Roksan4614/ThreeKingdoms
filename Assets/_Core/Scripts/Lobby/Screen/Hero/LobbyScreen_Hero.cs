using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        SetHero_Batch();

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
                m_itemList[i].SetHeroData(heroInfo, OnButton_ListHero, OnButton_ListHeroRemove).Forget();
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
    }

    protected override async UniTask CloseAsync()
    {
        ResetActiveButton_List();
        for (int i = 0; i < m_itemBatch.Count; i++)
            m_itemBatch[i].SetActiveButton(false);
        m_curIndex_Batch = -1;

        await base.CloseAsync();
    }

    #region BATCH
    void SetHero_Batch()
    {
        int i = 0;
        for (; i < m_myHero.Count; i++)
            m_itemBatch[i]
                .SetHeroData(m_myHero[i], OnButton_BatchHero, OnButton_BatchHeroRemove).Forget();

        for (; i < m_itemBatch.Count; i++)
            m_itemBatch[i].Disable();
    }

    void OnButton_BatchHero(HeroIconComponent _item)
    {
        if (m_myHero.Count == 1)
            return;

        var index = m_myHero.FindIndex(x => x.key == _item.data.key);

        if (m_curIndex_Batch == index)
        {
            m_itemBatch[m_curIndex_Batch].SetActiveButton(false);
            m_curIndex_Batch = -1;
            return;
        }

        if (m_curIndex_Batch != index && m_curIndex_Batch > -1)
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
        var index = m_myHero.FindIndex(x => x.key == _item.data.key);

        m_myHero.RemoveAt(index);
        SetHero_Batch();
        m_curIndex_Batch = -1;

        var heroInfo = m_itemList[index].data;
        heroInfo.isBatch = false;
        m_itemList[index].UpdateHeroInfo(heroInfo);

        ResetActiveButton_List();
        SetHero_List();
    }
    #endregion BATCH

    void SetHero_List()
    {
        m_itemList = m_itemList
            .OrderByDescending(x => x.data.isMine)
            .ThenByDescending(x => x.data.isBatch).ToList();

        for (int i = m_itemList.Count - 1; i > -1; i--)
        {
            m_itemList[i].transform.SetAsFirstSibling();
        }
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

            m_itemList[index].SetActiveButton(true);
            m_curIndex_List = index;

            m_itemList[index].element.btnAction.transform.SetText("Text",
                _item.data.isBatch ? "<size=150><color=#9A0A00>-</color></size>" : "+");
        }
        else if (m_curIndex_List == index)
        {
            m_itemList[index].SetActiveButton(false);
            m_curIndex_List = -1;
        }

        for (int i = 0; i < m_myHero.Count; i++)
            m_itemBatch[i].SetActiveButton(m_curIndex_List > -1 && _item.data.isBatch == false);

        m_curIndex_Batch = -1;
    }

    void OnButton_ListHeroRemove(HeroIconComponent _item)
    {
        var index = _item.transform.GetSiblingIndex();

        if (_item.data.isBatch)
        {
            OnButton_BatchHeroRemove(_item);
        }
    }

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
