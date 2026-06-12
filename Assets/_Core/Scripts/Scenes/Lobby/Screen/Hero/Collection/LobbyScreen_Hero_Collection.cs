using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyScreen_Hero_Collection : LobbyScreen_Hero_TabBase, IValidatable
{
    List<TotalStatData> m_totalStat = new();

    bool m_isNeedUpdate = false;

    protected override void Awake()
    {
        TotalStatData baseData = new();
        baseData.Create(m_element.pTotalStat.GetChild(0));
        m_totalStat.Add(baseData);
    }

    private void Start()
    {
        UpdateLayout();

        Signal.instance.UpdateHeroStat.connectLambda = new(this, _ =>
        {
            if (gameObject.activeInHierarchy == false)
                m_isNeedUpdate = true;
            else
                UpdateLayout();
        });
    }

    private void OnEnable()
    {
        if (m_isNeedUpdate == true)
        {
            UpdateLayout();
            m_isNeedUpdate = false;
        }
    }

    public override bool IsCloseScreen()
    {
        if (PopupManager.instance.IsOpenPopup())
            return false;

        return true;
    }

    void UpdateLayout(string _key = "")
    {
        var dbList = DataManager.stat.friendShip.dbFriendShip;
        var content = m_element.scroll.content;

        m_element.scroll.Initialize<LobbyScreen_Hero_Collection_Item>(dbList.Count,
            (_item, _idxData) =>
            {
                _item.SetData(dbList[_idxData]);
#if UNITY_EDITOR
                _item.name = dbList[_idxData].key;
#endif
            });

        //int i = 0;
        //for (; i < dbList.Count; i++)
        //{
        //    var item = i == content.childCount ?
        //        Instantiate(m_baseScrollItem, content) :
        //        content.GetChild(i).GetComponent<LobbyScreen_Hero_Collection_Item>();

        //    item.SetData(dbList[i]);
        //}

        //for (; i < content.childCount; i++)
        //    content.GetChild(i).gameObject.SetActive(false);

        SetTextTotalStat();
    }

    void SetTextTotalStat()
    {
        var dbBonusStat = DataManager.stat.friendShip.bonusStatBonus;
        var pTotalStat = m_element.pTotalStat;

        //baseTotalRelic.txtName = panel.Find("Total_Relic/Text").GetComponent<TextMeshProUGUI>();
        int i = 0;
        foreach (var d in dbBonusStat)
        {
            if (i == m_totalStat.Count)
            {
                TotalStatData newData = new();
                newData.Create(Instantiate(m_totalStat[0].txtTitle, pTotalStat).transform);
                m_totalStat.Add(newData);
            }

            TotalStatData data = m_totalStat[i];
            data.txtTitle.text = d.Value.statName;
            data.txtValue.text = $"{d.Value.stringPercent}";
            m_totalStat[i].SetActive(true);
            i++;
        }

        for (; i < m_totalStat.Count; i++)
            m_totalStat[i].SetActive(false);

        pTotalStat.ForceRebuildLayout();
        RebuildLayout();
    }

    void RebuildLayout()
    {
        m_element.scroll.content.anchoredPosition = Vector2.zero;

        var rtPanel = m_element.rtPanel;
        var rtLayout = m_element.rtLayout;

        rtPanel.ForceRebuildLayout();

        var heightPanel = rtPanel.rect.height;          //1407
        var posY_Layout = rtLayout.anchoredPosition.y;  //-110

        var sizeLayout = rtLayout.sizeDelta;            //1296
        sizeLayout.y = heightPanel + posY_Layout;
        rtLayout.sizeDelta = sizeLayout;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform pTotalStat;

        public LoopScrollHelper scroll;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");

            pTotalStat = panel.Find("Total_Stat");

            scroll = panel.Find("List/Scroll").GetComponent<LoopScrollHelper>();
        }

        public RectTransform rtPanel => (RectTransform)pTotalStat.parent;
        public RectTransform rtLayout => (RectTransform)scroll.transform.parent;
    }
    struct TotalStatData
    {
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtValue;

        public void Create(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("");
            txtValue = _transform.GetComponent<TextMeshProUGUI>("Text");
        }

        public void SetActive(bool _isActive) => txtTitle.gameObject.SetActive(_isActive);
    }
    #endregion VALIDATE
}
