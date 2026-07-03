using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyStoryModeComponent : BasePopupComponent
{
    PopupLobbyStoryModeComponent() : base(PopupType.LobbyStoryMode) { }

    Dictionary<RegionType, ButtonHelper> m_dicButton = new();
    RegionType m_curRegion = RegionType.MAX;

    private void Start()
    {
        Utils.SetActivePunch(m_element.panel, true);

        Utils.WaitEscape(this, () =>
        {
            Close();
        }, _token: destroyCancellationToken);

        InitializeTab();
        OnButton_Tab(RegionType.NONE);
    }

    void InitializeTab()
    {
        int i = 0;
        for (var region = RegionType.NONE; region <= RegionType.Etc; region++, i++)
        {
            var slot = i == m_element.pTab.childCount ? Instantiate(m_element.pTab.GetChild(0), m_element.pTab) : m_element.pTab.GetChild(i);

            var r = region;
            var button = slot.GetComponent<ButtonHelper>();

            m_dicButton.Add(r, button);
            button.onClick.AddListener(() => OnButton_Tab(r));

            button.text = TableManager.stringTable.GetRegionType(region, true);
            button.SetDrawSelect(false);
        }

        m_element.pTab.ForceRebuildLayout();
    }

    void OnButton_Tab(RegionType _region)
    {
        if (m_curRegion == _region)
            return;

        if (m_dicButton.ContainsKey(m_curRegion))
            m_dicButton[m_curRegion].SetDrawSelect(false);

        m_dicButton[_region].SetDrawSelect(true);
        m_curRegion = _region;

        SetNodeData();
    }

    void SetNodeData()
    {
        var group = TableManager.storyNode.group
            .Select(x => x.Where(x => x.region_type == m_curRegion || m_curRegion == RegionType.NONE || x.region_type == RegionType.NONE).ToList())
            .Where(x => x.Count > 0).ToList();

        var content = m_element.scroll.content;
        int i = 0;
        for (; i < group.Count; i++)
        {
            var slot = (i == content.childCount ? Instantiate(content.GetChild(0), content) : content.GetChild(i))
                .GetComponent<PopupLobbyStoryMode_Slot>();

            slot.SetNodeData(group[i]);
            slot.gameObject.SetActive(true);
        }

        m_element.txtEmpty.gameObject.SetActive(i == 0);

        for (; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);

        content.ForceRebuildLayout();
    }


    public override void Close()
        => Utils.SetActivePunch(m_element.panel, false, _callback: base.Close);

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;

        public Transform pTab;
        public ScrollRect scroll;
        public GaugeHelper gauge;

        public TextMeshProUGUI txtEmpty;


        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");

            pTab = _transform.Find("Panel/Tab");
            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
            gauge = _transform.GetComponent<GaugeHelper>("Panel/GaugeHelper");

            txtEmpty = scroll.viewport.GetComponent<TextMeshProUGUI>("txt_empty");
        }

        public Transform panel => txtTitle.transform.parent;
    }
    #endregion VALIDATE

}
