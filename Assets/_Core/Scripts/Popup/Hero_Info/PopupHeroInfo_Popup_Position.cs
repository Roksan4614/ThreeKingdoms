using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Popup_Position : MonoBehaviour, IValidatable
{
    string m_heroKey;

    Dictionary<CategoryType_HeroPositon, PopupHeroInfo_Popup_Position_Group> m_group = new();

    private void Start()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);

        for (var i = CategoryType_HeroPositon.NONE + 1; i < CategoryType_HeroPositon.MAX; i++)
        {
            CategoryType_HeroPositon type = i;
            int idx = (int)type;

            var data = TableManager.heroPosition.GetPositionds(type);
            if (data.Count == 0)
                continue;

            var group = Instantiate(m_element.baseGroup, m_element.scroll.content);
            group.Initialize(type, data, (_category, _type) => OnButtonAsync(_category, _type).Forget());

            m_group.Add(type, group);
        }

        DestroyImmediate(m_element.baseGroup.gameObject);
        m_element.scroll.transform.ForceRebuildLayout();

        RefreshData();
    }

    public bool isNeedUpdate { get; private set; }

    public async UniTask<bool> OpenPopupAsync(string _heroKey)
    {
        isNeedUpdate = false;
        gameObject.SetActive(true);
        m_heroKey = _heroKey;

        m_element.scroll.content.anchoredPosition = Vector2.zero;
        RefreshData();

        await UniTask.WaitUntil(() => gameObject.activeSelf == false);

        return isNeedUpdate;
    }

    void RefreshData(CategoryType_HeroPositon _category = CategoryType_HeroPositon.NONE, HeroPositionType _heroPositionType = HeroPositionType.NONE)
    {
        if (_heroPositionType > HeroPositionType.NONE)
            m_group[_category].RefreshData(_heroPositionType);
        else
        {
            foreach (var g in m_group)
                g.Value.RefreshData();
        }
    }

    bool m_isDoing = false;
    async UniTask OnButtonAsync(CategoryType_HeroPositon _category, HeroPositionType _heroPositionType)
    {
        if (m_isDoing == true)
            return;

        m_isDoing = true;

        HeroPositionData prevData = DataManager.heroPosition.GetHeroPosition(m_heroKey);

        bool result = await DataManager.heroPosition.API_BindPosition(m_heroKey, _heroPositionType);

        if (result == true)
        {
            RefreshData(_category, _heroPositionType);

            if (prevData != null)
            {
                var prevCategory = prevData.positionData.category;
                RefreshData(prevCategory, prevData.type);
            }

            isNeedUpdate = true;
        }

        m_isDoing = false;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public ScrollRect scroll;

        public PopupHeroInfo_Popup_Position_Group baseGroup;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");

            scroll = panel.GetComponent<ScrollRect>();
            baseGroup = scroll.content.GetChild(0).GetComponent<PopupHeroInfo_Popup_Position_Group>();
        }
    }
    #endregion VALIDATA
}

