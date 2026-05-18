using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CastleMissionData = Data_Castle_Mission.CastleMissionData;

public class PopupCastleMissionComponent : BasePopupComponent
{
    public enum TabType
    {
        NONE = -1,
        MissionList,
        RunningList,
    }

    PopupCastleMissionComponent() : base(PopupType.Castle_Mission) { }

    CancellationTokenSource m_cts;

    TabType m_curTab = TabType.NONE;

    protected override void Awake()
    {
        base.Awake();

        m_element.baseItem.transform.SetParent(m_element.scroll.viewport);
        m_element.baseItem.gameObject.SetActive(false);

        var popup = transform.Find("Popup");
        for (int i = 0; i < popup.childCount; i++)
            popup.GetChild(i).gameObject.SetActive(false);
    }

    private void Start()
    {
        m_cts = new();
        Utils.WaitEscape(this, () => Close(), _token: m_cts.Token);

        foreach (var tab in m_element.dbTab)
            tab.Value.onClick.AddListener(() => OnButton_Tab(tab.Key));

        m_element.btnRefresh.onClick.AddListener(() =>
        {
            DataManager.castle.mission.RefreshMission();
            SetMissionList(false);
        });

        m_element.btnAll.onClick.AddListener(() =>
        {
            DataManager.castle.mission.CompleteMission();
            SetMissionList(true);
        });
    }

    public override void OpenPopup(params object[] _args)
    {
        OnButton_Tab(TabType.MissionList);
        RefreshRemainCount();
    }

    void RefreshRemainCount()
        => m_element.txtRemainCount.text = $"남은_횟수 : {DataManager.castle.mission.remainCount}";

    void OnButton_Tab(TabType _tabType)
    {
        if (m_curTab == _tabType)
            return;

        if (m_curTab > TabType.NONE)
            m_element.dbTab[m_curTab].SetDrawSelect(false);

        m_element.dbTab[_tabType].SetDrawSelect(true);

        m_curTab = _tabType;
        SetMissionList(_tabType == TabType.RunningList);

        m_element.btnRefresh.gameObject.SetActive(_tabType == TabType.MissionList);
        m_element.btnAll.gameObject.SetActive(_tabType == TabType.RunningList);
    }

    void SetMissionList(bool _isRunning)
    {
        var missionList = DataManager.castle.mission.data.Where(x => (x.tickStart == 0) == (_isRunning == false)).ToArray();

        if (_isRunning)
            missionList = missionList.OrderByDescending(x => x.tickEnd < Utils.GetUTC().Ticks).ToArray();

        int i = 0;
        for (; i < missionList.Length; i++)
        {
            var missionData = missionList[i];

            bool isNew = i == m_element.scroll.content.childCount;
            var item = isNew ?
                Instantiate(m_element.baseItem, m_element.scroll.content) :
                m_element.scroll.content.GetChild(i).GetComponent<PopupCastleMission_Item>();

            if (isNew == true)
                item.Initalize(
                    _data => OnButtonAsync_Batch(_data).Forget());

            item.gameObject.SetActive(true);
            item.name = missionData.idx.ToString();
            item.SetMissionInfo(missionData);
        }

        for (; i < m_element.scroll.content.childCount; i++)
            m_element.scroll.content.GetChild(i).gameObject.SetActive(false);

        m_element.scroll.content.ForceRebuildLayout();
    }

    async UniTask OnButtonAsync_Batch(CastleMissionData _missionData)
    {
        // 진행중이라면
        if (_missionData.tickStart > 0)
        {
            // 완료면 보상받기
            if (_missionData.tickEnd <= Utils.GetUTC().Ticks)
            {
                PopupManager.instance.AlertShow("TODO: 보상받기");
                DataManager.castle.mission.CompleteMission(_missionData);

                SetMissionList(true);
            }
            // 아니면 시간단축 팝업 띄우기
            else
            {
                PopupManager.instance.AlertShow("TODO: 시간단축 팝업 띄우기");
            }
        }
        // 아니면 상세페이지를 띄워주자
        else
        {
            await Utils.SetActivePunchAsync(m_element.panel, false);

            m_element.info.Open(_missionData);

            //await UniTask.WaitUntil(() => m_element.info.resultType != StatusType.Wait, cancellationToken: destroyCancellationToken);
            await UniTask.WaitUntil(() => m_element.info.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

            Utils.SetActivePunch(m_element.panel, true);

            if (m_element.info.resultType == StatusType.Success)
            {
                DataManager.castle.mission.StartMission(_missionData);
                RefreshRemainCount();
                SetMissionList(false);
            }
        }
    }

    protected override void OnClosePopup()
    {
        ReleaseCTS();
    }

    void ReleaseCTS()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public PopupCastleMission_Popup_Info info;
        public PopupCastleMission_Item baseItem;

        public ScrollRect scroll;

        [SerializeField] ButtonHelper[] m_tab;
        public TextMeshProUGUI txtRemainCount;

        Dictionary<TabType, ButtonHelper> m_dbTab;

        public ButtonHelper btnRefresh;
        public ButtonHelper btnAll;

        public Dictionary<TabType, ButtonHelper> dbTab
        {
            get
            {
                if (m_dbTab == null)
                {
                    int idx = 1;
                    m_dbTab = m_tab.ToDictionary(x => TabType.NONE + idx++, x => x);
                }
                return m_dbTab;
            }
        }

        public void Initialize(Transform _transform)
        {
            info = _transform.GetComponent<PopupCastleMission_Popup_Info>("Popup/MissionInfo");

            scroll = _transform.GetComponent<ScrollRect>("Panel/List/Scroll");
            baseItem = scroll.content.GetChild(0).GetComponent<PopupCastleMission_Item>();

            txtRemainCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_remain_count");
            m_tab = _transform.Find("Panel/Tab").GetComponentsInChildren<ButtonHelper>();

            btnRefresh = _transform.GetComponent<ButtonHelper>("Panel/btn_refresh");
            btnAll = _transform.GetComponent<ButtonHelper>("Panel/btn_all");
        }

        public Transform panel => btnAll.transform.parent;
    }
    #endregion VALIDATE
}
