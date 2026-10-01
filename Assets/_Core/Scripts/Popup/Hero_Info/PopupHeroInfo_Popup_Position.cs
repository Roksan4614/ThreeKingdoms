using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Popup_Position : MonoBehaviour, IValidatable
{
    string m_heroKey;

    Dictionary<PositionCategory, PopupHeroInfo_Popup_Position_Group> m_group = new();

    private void Start()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);

        for (var i = PositionCategory.None + 1; i < PositionCategory.Max; i++)
        {
            PositionCategory type = i;
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
    }

    public bool isNeedUpdate { get; private set; }

    public async UniTask<bool> OpenPopupAsync(string _heroKey)
    {
        isNeedUpdate = false;
        gameObject.SetActive(true);
        await UniTask.WaitForEndOfFrame();
        m_heroKey = _heroKey;

        m_element.scroll.velocity =
        m_element.scroll.content.anchoredPosition = Vector2.zero;

        RefreshData();

        var posData = DataManager.heroPosition.GetHeroPosition(_heroKey);
        if (posData != null)
        {
            var pos = m_element.scroll.content.anchoredPosition;
            pos.y = m_group[posData.positionData.position_category].GetPositionY(posData.type) * -1 - 200;
            m_element.scroll.content.anchoredPosition = pos;
        }

        await UniTask.WaitUntil(() => gameObject.activeSelf == false);

        return isNeedUpdate;
    }

    void RefreshData(PositionCategory _category = PositionCategory.None, PositionType _heroPositionType = PositionType.None)
    {
        if (_heroPositionType > PositionType.None)
            m_group[_category].RefreshData(_heroPositionType);
        else
        {
            foreach (var g in m_group)
                g.Value.RefreshData();
        }
    }

    bool m_isDoing = false;
    async UniTask OnButtonAsync(PositionCategory _category, PositionType _heroPositionType)
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
                var prevCategory = prevData.positionData.position_category;
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

