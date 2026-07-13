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
        if (OpenIFMode() == false)
        {
            OnButton_Tab(RegionType.NONE);

            m_element.gauge.textTitle = "달성도_";

            var historyCount = DataManager.storyMode.historyCount;
            var totalNode = TableManager.storyNode.list.Count(x => x.chapter_key > 0);
            m_element.gauge.fillAmount = historyCount / (float)totalNode;
            m_element.gauge.textAmount = $"{(m_element.gauge.fillAmount * 100):0.00}%<size=90%> ({historyCount}/{totalNode})</size>";
        }

        Signal.instance.UnlockStoryMode.connectLambda = new(this, () =>
        {
            if (OpenIFMode() == false)
            {
                OnButton_Tab(RegionType.NONE, true);
            }
        });
    }

    bool OpenIFMode()
    {
        var ifNodeData = DataManager.storyMode.GetOpenIFMode();

        bool notIfMode = ifNodeData.isActive == false;

        m_element.bg.SetActive(notIfMode);
        m_element.pTab.gameObject.SetActive(notIfMode);
        m_element.scroll.gameObject.SetActive(notIfMode);
        m_element.gauge.gameObject.SetActive(notIfMode);

        return m_element.storyIF.Open(ifNodeData);
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

    void OnButton_Tab(RegionType _region, bool _isForce = false)
    {
        if (m_curRegion == _region && _isForce == false)
            return;

        if (m_dicButton.ContainsKey(m_curRegion))
            m_dicButton[m_curRegion].SetDrawSelect(false);

        m_dicButton[_region].SetDrawSelect(true);
        m_curRegion = _region;

        SetNodeData();

        var scroll = m_element.scroll;
        var content = scroll.content;
        scroll.velocity = Vector2.zero;

        // POSITION
        {
            // 일단 slot 위치
            var layout = content.GetComponent<VerticalLayoutGroup>();
            float posY = layout.padding.top;

            for (int i = 0; i < DataManager.storyMode.siblingIndexSlot; i++)
            {
                var rt = (RectTransform)content.GetChild(i);
                posY += rt.rect.height;
            }

            // 거기에서 node위치만큼 내리자.
            var slot = content.GetChild(DataManager.storyMode.siblingIndexSlot);
            layout = slot.GetComponent<VerticalLayoutGroup>();

            float heightNode = 0;
            for (int i = 0; i <= DataManager.storyMode.siblingIndexNode; i++)
            {
                //+1한 이유는 연도가 있어서
                var rt = (RectTransform)slot.GetChild(i + 1);
                posY += rt.rect.height + layout.spacing;

                heightNode = rt.rect.height;
            }

            posY = Mathf.Min(
                    content.rect.height - scroll.viewport.rect.height,
                    Mathf.Max(0, posY - scroll.viewport.rect.height * .5f + heightNode * .5f));

            content.SetAnchoredPositionY(posY);
        }
    }

    void SetNodeData()
    {
        DataManager.storyMode.SetPopupSiblingIndex(0, 0);

        var group = TableManager.storyNode.group
            .Select(x =>
                x.Where(y =>
                    y[0].region_type == m_curRegion ||
                    m_curRegion == RegionType.NONE ||
                    y[0].region_type == RegionType.NONE ||
                    y[0].order_num == DataManager.storyMode.nextOpenOrderNumber)
                .ToList())
            .Where(x => x.Count > 0).ToList();

        var content = m_element.scroll.content;
        int i = 0;
        for (; i < group.Count; i++)
        {
            var slot = (i == content.childCount ? Instantiate(content.GetChild(0), content) : content.GetChild(i))
                .GetComponent<PopupLobbyStoryMode_Slot>();

            slot.gameObject.SetActive(true);
            slot.name = group[i][0][0].year.ToString();
            if (slot.SetNodeData(group[i]) == false)
            {
                i++;
                break;
            }
        }

        m_element.txtEmpty.gameObject.SetActive(i == 0);

        for (; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);

        m_element.scroll.content.ForceRebuildLayout();
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
        public GameObject bg;

        public TextMeshProUGUI txtTitle;

        public Transform pTab;
        public ScrollRect scroll;
        public GaugeHelper gauge;

        public TextMeshProUGUI txtEmpty;

        public PopupLobbyStoryMode_IF storyIF;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");

            bg = _transform.Find("Panel/BG").gameObject;
            pTab = _transform.Find("Panel/Tab");
            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
            gauge = _transform.GetComponent<GaugeHelper>("Panel/GaugeHelper");

            txtEmpty = scroll.viewport.GetComponent<TextMeshProUGUI>("txt_empty");
            storyIF = _transform.GetComponent<PopupLobbyStoryMode_IF>("Panel/IF");
        }

        public Transform panel => txtTitle.transform.parent;
    }
    #endregion VALIDATE

}
