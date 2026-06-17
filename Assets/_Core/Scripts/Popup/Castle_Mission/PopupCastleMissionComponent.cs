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
    PopupUseTimeStoneComponent m_popupTimeStone;

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
        Utils.WaitEscape(this, CloseEscape, _token: m_cts.Token);

        foreach (var tab in m_element.dbTab)
            tab.Value.onClick.AddListener(() => OnButton_Tab(tab.Key));

        m_element.btnRefresh.onClick.AddListener(() =>
        {
            DataManager.castle.mission.RefreshMission();
            SetMissionList(false);
        });

        m_element.btnAll.onClick.AddListener(()
            => OpenMissionResultAsync(DataManager.castle.mission.data.Where(x => x.tickEnd > 0 && x.tickEnd < Utils.GetUTC().Ticks).ToArray()).Forget());
    }

    public override void OpenPopup(params object[] _args)
    {
        Utils.SetActivePunch(m_element.panel, true);

        OnButton_Tab(TabType.MissionList);
        RefreshRemainCount();

        UpdateLevelInfo();
    }

    void UpdateLevelInfo()
    {
        var levelInfo = DataManager.castle.mission.levelInfo;
        m_element.exp.textTitle = $"Lv.{levelInfo.level}_관아_경험치 : ";
        m_element.exp.textAmount = $"{levelInfo.nowExp:#,0} / {levelInfo.maxExp:#,0}";
        m_element.exp.fillAmount = levelInfo.nowExp / (float)levelInfo.maxExp;
    }

    void RefreshRemainCount()
        => m_element.txtRemainCount.text = $"남은_횟수 : {DataManager.castle.mission.levelInfo.missionCount}";

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
            missionList = missionList.SortByDescending(x => x.tickEnd < Utils.GetUTC().Ticks);

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
                    _data => OnButtonAsync_Batch(_data).Forget(), _ts => OnUpdateTimer(_ts));

            item.gameObject.SetActive(true);
            item.name = missionData.idx.ToString();
            item.SetMissionInfo(missionData);
        }

        for (; i < m_element.scroll.content.childCount; i++)
            m_element.scroll.content.GetChild(i).gameObject.SetActive(false);

        m_element.txtEmpty.gameObject.SetActive(missionList.Length == 0);

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
                await OpenMissionResultAsync(_missionData);
            }
            // 아니면 시간단축 팝업 띄우기
            else
            {
                await OpenUseTimeStoneAsync(_missionData);
            }
        }
        // 아니면 상세페이지를 띄워주자
        else
        {
            if (DataManager.castle.mission.levelInfo.missionCount == 0)
            {
#if UNITY_EDITOR
                PopupManager.instance.AlertShow("더이상_임무_보낼_수_없지만_테스트니까ㄱ");
#else
                PopupManager.instance.AlertShow("더이상_임무를_보낼_수_없습니다.");
                return;
#endif
            }

            var countNoBatch = DataManager.userInfo.myHero.Where(x => DataManager.castle.mission.GetMissionIdxBatchHero(x.key) == -1)
                .Count();
            if (countNoBatch == 0)
            {
                PopupManager.instance.AlertShow("임무_보낼_장수가_없습니다.");
                return;
            }

            await Utils.SetActivePunchAsync(m_element.panel, false);

            m_element.info.Open(_missionData);

            //await UniTask.WaitUntil(() => m_element.info.resultType != StatusType.Wait, cancellationToken: destroyCancellationToken);
            await UniTask.WaitUntil(() => m_element.info.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

            Utils.SetActivePunch(m_element.panel, true);

            if (m_element.info.resultType == StatusType.Success)
            {
                RefreshRemainCount();
                SetMissionList(false);
            }
        }
    }

    async UniTask OpenMissionResultAsync(params CastleMissionData[] _missionData)
    {
        await Utils.SetActivePunchAsync(m_element.panel, false);
        await m_element.result.OpenAsync(_missionData);
        Utils.SetActivePunch(m_element.panel, true);

        if (m_element.result.resultType == StatusType.Success)
        {
            UpdateLevelInfo();
            SetMissionList(true);
        }
    }

    async UniTask OpenUseTimeStoneAsync(CastleMissionData _missionData)
    {
        m_popupTimeStone = await PopupManager.instance.OpenPopup<PopupUseTimeStoneComponent>(PopupType.UseTimeStone);

        var endTime = new DateTime(_missionData.tickEnd, DateTimeKind.Utc);
        var ts = endTime - Utils.GetUTC();
        OnUpdateTimer(ts);

        await UniTask.WaitUntil(() => m_popupTimeStone.statusType != StatusType.Wait);

        if (m_popupTimeStone.statusType == StatusType.Success)
        {
            var result = await DataManager.castle.mission.TimerBonusAsync(_missionData.idx, m_popupTimeStone.timeBonus);
            if (result == StatusType.Success)
                SetMissionList(true);
        }

        m_popupTimeStone = null;
    }

    void OnUpdateTimer(TimeSpan _ts)
    {
        m_popupTimeStone?.UpdateRemainTime(_ts);
    }

    void CloseEscape()
    {
        if (m_element.info.CloseEscape() == false)
            return;

        if (m_element.result.CloseEscape() == false)
            return;

        Close();
    }

    public override void Close()
        => CloseAsync().Forget();

    async UniTask CloseAsync()
    {
        ReleaseCTS();

        await Utils.SetActivePunchAsync(m_element.panel, false);
        base.Close();
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
        public PopupCastleMission_Popup_Result result;

        public PopupCastleMission_Item baseItem;
        public TextMeshProUGUI txtEmpty;

        public ScrollRect scroll;

        [SerializeField] ButtonHelper[] m_tab;
        public TextMeshProUGUI txtRemainCount;

        Dictionary<TabType, ButtonHelper> m_dbTab;

        public ButtonHelper btnRefresh;
        public ButtonHelper btnAll;

        public GaugeHelper exp;

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
            result = _transform.GetComponent<PopupCastleMission_Popup_Result>("Popup/MissionResult");

            scroll = _transform.GetComponent<ScrollRect>("Panel/List/Scroll");
            baseItem = scroll.content.GetChild(0).GetComponent<PopupCastleMission_Item>();

            txtRemainCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_remain_count");
            m_tab = _transform.Find("Panel/Tab").GetComponentsInChildren<ButtonHelper>();

            btnRefresh = _transform.GetComponent<ButtonHelper>("Panel/btn_refresh");
            btnAll = _transform.GetComponent<ButtonHelper>("Panel/btn_all");

            exp = _transform.GetComponent<GaugeHelper>("Panel/EXP");
            txtEmpty = scroll.transform.GetComponent<TextMeshProUGUI>("txt_empty");
        }

        public Transform panel => btnAll.transform.parent;
    }
    #endregion VALIDATE
}
