using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroFilter : BasePopupComponent, IValidatable
{
    PopupHeroFilter() : base(PopupType.Hero_Filter) { }

    string m_stringAll;
    public bool isNeedUpdate { get; private set; }

    HeroSortType m_sortType = HeroSortType.NONE;
    Dictionary<HeroSortType, ButtonHelper> m_dicSort = new();

    Dictionary<RegionType, ButtonHelper> m_dicRegion = new();
    List<RegionType> m_filterRegion = new();

    Dictionary<HeroClassType, ButtonHelper> m_dicClass = new();
    List<HeroClassType> m_filterClass = new();

    Dictionary<GradeType, ButtonHelper> m_dicGrade = new();
    List<GradeType> m_filterGrade = new();

    private void Start()
    {
        var content = m_element.scroll.content;
        m_stringAll = TableManager.stringTable.GetString("TAB_ALL");

        #region 정렬
        {
            var sort = content.Find("Sort/Content");
            sort.parent.GetComponent<TextMeshProUGUI>("txt_title").text = "정렬_";
            int max = (int)HeroSortType.MAX;
            for (int i = 0; i < max; i++)
            {
                var type = (HeroSortType)i;
                var btn = (i == sort.childCount ? Instantiate(sort.GetChild(0), sort) : sort.GetChild(i)).GetComponent<ButtonHelper>();
                btn.text = TableManager.stringTable.GetString($"SORTTYPE_{type}");
                btn.onClick.AddListener(() => OnButton_Sort(type));

                m_dicSort.Add(type, btn);
            }

            sort.ForceRebuildLayout();
            OnButton_Sort(DataManager.userInfo.sortData.sortType, true);
        }
        #endregion 정렬

        #region 국가
        {
            var panel = content.Find("Region/Content");
            panel.parent.GetComponent<TextMeshProUGUI>("txt_title").text = "국가_";
            int max = (int)RegionType.MAX;
            int idx = 0;
            for (int i = -1; i < max; i++, idx++)
            {
                var type = (RegionType)i;
                var btn = (idx == panel.childCount ? Instantiate(panel.GetChild(0), panel) : panel.GetChild(idx)).GetComponent<ButtonHelper>();
                btn.text = TableManager.stringTable.GetString(type == RegionType.NONE ? "TAB_ALL" : $"REGION_NAME_{type}");
                btn.onClick.AddListener(() => OnButton_Region(type));

                m_dicRegion.Add(type, btn);
            }

            panel.ForceRebuildLayout();
            m_filterRegion.AddRange(DataManager.userInfo.sortData.filter_region);
            SetFilterUpdate_Region();
        }
        #endregion 국가

        #region 역할
        {
            var panel = content.Find("Class/Content");
            panel.parent.GetComponent<TextMeshProUGUI>("txt_title").text = "역할_";
            int max = (int)HeroClassType.MAX;
            int idx = 0;
            for (int i = -1; i < max; i++, idx++)
            {
                var type = (HeroClassType)i;
                var btn = (idx == panel.childCount ? Instantiate(panel.GetChild(0), panel) : panel.GetChild(idx)).GetComponent<ButtonHelper>();
                btn.text = type == HeroClassType.NONE ? m_stringAll : TableManager.stringHero.GetString($"CLASSTYPE_{type.ToString().ToUpper()}");
                btn.onClick.AddListener(() => OnButton_Class(type));

                m_dicClass.Add(type, btn);
            }

            panel.ForceRebuildLayout();
            m_filterClass.AddRange(DataManager.userInfo.sortData.filter_class);
            SetFilterUpdate_Class();
        }
        #endregion 역할

        #region 등급
        {
            var panel = content.Find("Grade/Content");
            panel.parent.GetComponent<TextMeshProUGUI>("txt_title").text = "등급_";
            int max = (int)GradeType.MAX;
            int idx = 0;
            for (int i = -1; i < max; i++, idx++)
            {
                var type = (GradeType)i;
                var btn = (idx == panel.childCount ? Instantiate(panel.GetChild(0), panel) : panel.GetChild(idx)).GetComponent<ButtonHelper>();
                btn.text = type == GradeType.NONE ? m_stringAll : TableManager.stringTable.GetString($"GRADE_{type.ToString().ToUpper()}");
                btn.onClick.AddListener(() => OnButton_Grade(type));

                m_dicGrade.Add(type, btn);
            }

            panel.ForceRebuildLayout();
            m_filterGrade.AddRange(DataManager.userInfo.sortData.filter_grade);
            SetFilterUpdate_Grade();
        }
        #endregion 국가

        content.ForceRebuildLayout();

        Utils.WaitEscape(this, () => Utils.AfterSecond(Close));
    }

    public override void OpenPopup(params object[] _args)
    {
        isNeedUpdate = false;
        gameObject.SetActive(true);
        m_element.scroll.content.anchoredPosition = Vector2.zero;
    }

    public override void Close()
    {
        gameObject.SetActive(false);

        if (isNeedUpdate == true)
        {
            DataManager.userInfo.SetSortingData(m_sortType, DataManager.userInfo.sortData.isDescending);

            DataManager.userInfo.SetFilterData(m_filterRegion, m_filterClass, m_filterGrade);
        }
    }

    void OnButton_Sort(HeroSortType _sortType, bool _isForce = false)
    {
        if (m_dicSort.ContainsKey(m_sortType))
            m_dicSort[m_sortType].SetDrawSelect(false);

        m_sortType = _sortType;
        m_dicSort[m_sortType].SetDrawSelect(true);

        if (_isForce == false)
            isNeedUpdate = true;
    }

    void OnButton_Region(RegionType _region)
    {
        if (_region == RegionType.NONE)
        {
            bool isAll = isAll_Region == false;

            m_filterRegion.Clear();
            foreach (var btn in m_dicRegion)
            {
                btn.Value.isCheck = isAll;
                if (isAll)
                    m_filterRegion.Add(btn.Key);
            }
        }
        else
        {
            if (isAll_Region == true)
            {
                m_filterRegion.Clear();
                m_filterRegion.Add(_region);
            }
            else if (m_filterRegion.Remove(_region) == false)
                m_filterRegion.Add(_region);

            SetFilterUpdate_Region();
        }

        isNeedUpdate = true;
    }

    void OnButton_Class(HeroClassType _class)
    {
        if (_class == HeroClassType.NONE)
        {
            bool isAll = isAll_Class == false;

            m_filterClass.Clear();
            foreach (var btn in m_dicClass)
            {
                btn.Value.isCheck = isAll;
                if (isAll)
                    m_filterClass.Add(btn.Key);
            }
        }
        else
        {
            if (isAll_Class == true)
            {
                m_filterClass.Clear();
                m_filterClass.Add(_class);
            }
            else if (m_filterClass.Remove(_class) == false)
                m_filterClass.Add(_class);

            SetFilterUpdate_Class();
        }

        isNeedUpdate = true;
    }

    void OnButton_Grade(GradeType _grade)
    {
        if (_grade == GradeType.NONE)
        {
            bool isAll = isAll_Grade == false;

            m_filterGrade.Clear();
            foreach (var btn in m_dicGrade)
            {
                btn.Value.isCheck = isAll;
                if (isAll)
                    m_filterGrade.Add(btn.Key);
            }
        }
        else
        {
            if (isAll_Grade == true)
            {
                m_filterGrade.Clear();
                m_filterGrade.Add(_grade);
            }
            else if (m_filterGrade.Remove(_grade) == false)
                m_filterGrade.Add(_grade);

            SetFilterUpdate_Grade();
        }

        isNeedUpdate = true;
    }

    void SetFilterUpdate_Region()
    {
        foreach (var btn in m_dicRegion)
            btn.Value.isCheck = m_filterRegion.Contains(btn.Key);
    }

    void SetFilterUpdate_Class()
    {
        foreach (var btn in m_dicClass)
            btn.Value.isCheck = m_filterClass.Contains(btn.Key);
    }

    void SetFilterUpdate_Grade()
    {
        foreach (var btn in m_dicGrade)
            btn.Value.isCheck = m_filterGrade.Contains(btn.Key);
    }

    bool isAll_Region => m_filterRegion.Contains(RegionType.NONE);
    bool isAll_Class => m_filterClass.Contains(HeroClassType.NONE);
    bool isAll_Grade => m_filterGrade.Contains(GradeType.NONE);

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ScrollRect scroll;

        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
        }
    }
    #endregion VALIDATE
}
