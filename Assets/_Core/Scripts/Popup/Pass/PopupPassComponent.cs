using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class PopupPassComponent : BasePopupComponent
{
    PopupPassComponent() : base(PopupType.Pass) { }

    TabType m_curTab;
    Dictionary<TabType, PopupPass_ContentBase> m_contents = new();
    Dictionary<TabType, ButtonHelper> m_tabs = new();

    private void Start()
    {
        m_contents.Add(TabType.Reward, m_element.contentReward);
        m_contents.Add(TabType.Mission, m_element.contentMission);

        foreach (var c in m_contents)
            c.Value.gameObject.SetActive(false);

        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
        {
            TabType tab = i;
            int idx = (int)i;

            m_tabs.Add(tab, m_element.btnTap[idx]);
            m_tabs[tab].onClick.AddListener(() => SetTab(tab));
        }

        SetTab(TabType.Reward, true);
    }

    public override void OpenPopup(params object[] _args)
    {
        gameObject.SetActive(true);
        SetTab(TabType.Reward);

        m_element.scroll.content.anchoredPosition = Vector2.zero;
    }

    void SetTab(TabType _tab, bool _isForce = false)
    {
        if (m_curTab == _tab && _isForce == false)
            return;

        m_curTab = _tab;
        m_element.scroll.velocity =
        m_element.scroll.content.anchoredPosition = Vector2.zero;

        bool isReward = _tab == TabType.Reward;
        bool isMission = _tab == TabType.Mission;

        m_contents[TabType.Reward].gameObject.SetActive(isReward);
        m_contents[TabType.Mission].gameObject.SetActive(isMission);

        m_tabs[TabType.Reward].SetDrawSelect(isReward);
        m_tabs[TabType.Mission].SetDrawSelect(isMission);

        m_element.slotStep.gameObject.SetActive(isReward);

        m_element.scroll.content = (RectTransform)m_contents[_tab].transform;
    }

    public override void Close()
    {
        Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtTimer;
        public Transform pHost;
        public ButtonHelper btnPass;
        public TextMeshProUGUI txtDescPass;
        public GaugeHelper gaugeXP;
        public TextMeshProUGUI txtLevelXP;

        public ScrollRect scroll;
        public PopupPass_Reward_Slot slotStep;

        public PopupPass_Reward contentReward;
        public PopupPass_Mission contentMission;

        public ButtonHelper[] btnTap;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            txtTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/Timer/Text");
            pHost = _transform.Find("Panel/Host");
            btnPass = _transform.GetComponent<ButtonHelper>("Panel/Info/btn_pass");
            txtDescPass = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_desc");
            gaugeXP = _transform.GetComponent<GaugeHelper>("Panel/Info/Gauge_XP");
            txtLevelXP = gaugeXP.transform.GetComponent<TextMeshProUGUI>("Level/Text");

            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
            slotStep = scroll.transform.GetComponent<PopupPass_Reward_Slot>("Step");

            contentReward = scroll.viewport.GetComponent<PopupPass_Reward>("Content_Reward");
            contentMission = scroll.viewport.GetComponent<PopupPass_Mission>("Content_Mission");

            btnTap = _transform.Find("Panel/Tab").GetComponentsInChildren<ButtonHelper>();
        }

        public Transform panel => txtTitle.transform.parent;
    }
    #endregion VALIDATE

    enum TabType
    {
        NONE = -1,

        Reward,
        Mission,

        MAX
    }
}
