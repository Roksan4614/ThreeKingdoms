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

    List<CharacterComponent> m_member = new();
    List<HeroInfoData> m_myHero = new();

    int m_curBatchIndex = -1;

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
        m_member.AddRange(TeamManager.instance.members.Values);
        SetHero_Batch();

        // 리스트 아이콘 미리 생성
        {
            m_myHero.AddRange(DataManager.userInfo.myHero);
            var scroll = m_element.list.layout.GetComponent<ScrollRect>();
            var baseItem = scroll.content.GetChild(0).GetComponent<HeroIconComponent>();
            baseItem.transform.SetParent(scroll.viewport);
            while (baseItem.element.icon.childCount > 0)
                DestroyImmediate(baseItem.element.icon.GetChild(0).gameObject);

            var dbList = TableManager.hero.list;
            int i = 0;
            for (; i < dbList.Count; i++)
                m_itemList.Add(Instantiate(baseItem, scroll.content));

            if (i < 20)
            {
                scroll.enabled = false;
                scroll.verticalScrollbar.gameObject.SetActive(false);

                var e = baseItem.element;
                DestroyImmediate(e.txtLevel.gameObject);
                DestroyImmediate(e.txtName.gameObject);
                DestroyImmediate(e.icon.parent.gameObject);
                DestroyImmediate(e.btnAction.gameObject);
                e.btnHero.interactable = false;

                // 20개 미리 생성은 해두자
                for (; i < 20; i++)
                    Instantiate(baseItem, scroll.content);
            }

            scroll.content.ForceRebuildLayout();

            baseItem.gameObject.SetActive(false);
            Destroy(baseItem.gameObject);

            SetHero_List();
        }
    }

    protected override async UniTask CloseAsync()
    {
        await base.CloseAsync();
    }

    #region BATCH
    void SetHero_Batch()
    {
        int i = 0;
        for (; i < m_member.Count; i++)
            m_itemBatch[i]
                .SetHeroData(m_member[i].info, OnButton_BatchHero, OnButton_BatchHeroRemove).Forget();

        for (; i < m_itemBatch.Count; i++)
            m_itemBatch[i].Disable();
    }

    void OnButton_BatchHero(HeroIconComponent _hero)
    {
        var index = m_member.FindIndex(x => x.data.key == _hero.data.key);

        if (m_curBatchIndex == index)
            return;

        if (m_curBatchIndex != index && m_curBatchIndex > -1)
            m_itemBatch[m_curBatchIndex].SetActiveButton(false);

        m_itemBatch[index].SetActiveButton(true);
        m_curBatchIndex = index;
    }

    void OnButton_BatchHeroRemove(HeroIconComponent _hero)
    {
        var index = m_member.FindIndex(x => x.data.key == _hero.data.key);

        m_member.RemoveAt(index);
        SetHero_Batch();
        m_curBatchIndex = -1;

        index = m_myHero.FindIndex(x => x.key == _hero.data.key);
        var db = m_myHero[index];
        db.isBatch = false;
        m_myHero[index] = db;
    }
    #endregion BATCH

    void SetHero_List(bool _isAll = true)
    {
        var dbHero = TableManager.hero.list;

        int i = 0;
        for (; i < dbHero.Count; i++)
        {
            var heroInfo = DataManager.userInfo.GetHeroInfoData(dbHero[i].key);

            if (heroInfo.isActive == false)
                heroInfo = new(dbHero[i].key, _isMine: false);

            m_itemList[i].SetHeroData(heroInfo, OnButton_BatchHero, OnButton_BatchHeroRemove).Forget();
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
