using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using CastleMissionData = Data_Castle_Mission.CastleMissionData;

public class PopupCastleMission_Popup_Info : BasePopupComponent
{
    protected PopupCastleMission_Popup_Info() : base(PopupType.NONE) { }

    protected CastleMissionData m_missionData;

    PopupHeroInfo m_popupHeroInfo;

    public StatusType resultType { get; protected set; }

    protected override void Awake()
    {
        base.Awake();

        m_element.btnStart.onClick.AddListener(OnButton_Start);

        m_element.baseHeroIcon.transform.SetParent(m_element.pHeroIcon.parent);
        m_element.baseHeroIcon.gameObject.SetActive(false);

        m_element.btnAdd.onClick.AddListener(() => OpenHeroListPopupAsync().Forget());
    }

    private void OnDisable()
    {
        m_ctsTimer = m_ctsTimer.ReleaseCTS();
    }

    CancellationTokenSource m_ctsTimer;
    async UniTask TimerAsync()
    {
        m_ctsTimer = m_ctsTimer.ReleaseCTS(true);
        var token = m_ctsTimer.Token;

        var endTime = new DateTime(m_missionData.tickEnd, DateTimeKind.Utc);

        TimeSpan ts = endTime - Utils.GetUTC();
        int prev = -1;
        while (ts.TotalSeconds > 0)
        {
            if (ts.TotalSeconds <= 10)
            {
                m_element.btnStart.text = ts.ToRemainTime(30);
                m_popupTimeStone?.UpdateRemainTime(ts, m_missionData.idx);
            }
            else if (ts.Seconds != prev)
            {
                m_element.btnStart.text = ts.ToRemainTime(30);
                prev = ts.Seconds;
                m_popupTimeStone?.UpdateRemainTime(ts, m_missionData.idx);
            }

            await UniTask.NextFrame(token);

            ts = endTime - Utils.GetUTC();
        }

        m_element.btnStart.text = "_확인_";
    }

    public void Open(CastleMissionData _mission, bool _isRunning)
    {
        m_missionData = _mission;

        if (_mission.isRunning == true)
        {
            TimerAsync().Forget();
            m_element.btnAdd.gameObject.SetActive(false);
        }
        else
        {
            m_element.btnStart.text = "_시작하기_";
            m_element.btnAdd.gameObject.SetActive(true);
            m_missionData.heroes = new();

            var coreStat = m_missionData.dbData.statType;
            int coreStatMax = m_missionData.coreStatMax;

            var myHero = DataManager.userInfo.myHero.Where(x => DataManager.castle.mission.GetMissionIdxBatchHero(x.key) == -1).ToList()
                .SortByDescending(x => x.resultCoreStat[coreStat]);

            int totalCoreStat = 0;

            foreach (var hero in myHero)
            {
                var heroData = hero;

                heroData.isBatch = totalCoreStat < coreStatMax;
                if (heroData.isBatch == true)
                {
                    totalCoreStat += heroData.resultCoreStat[coreStat];
                    m_missionData.heroes.Add(heroData.key);
                }
                else
                    break;
            }
        }

        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);
        resultType = StatusType.Wait;

        m_element.txtTitle.text = $"임무_:_[{TableManager.stringTable.GetGradeType(_mission.grade)}]";
        m_element.txtName.text = _mission.missionNameStat;
        m_element.gauge.textTitle = $"고유_능력({TableManager.stringTable.GetString($"CORESTAT_{_mission.dbData.statType.ToString().ToUpper()}")})_요구치";

        //소요시간
        var now = DateTime.Now;
        TimeSpan ts = now.AddSeconds(_mission.dbGradeData.durationSeconds) - now;
        m_element.txtContent_Time.text = $"소요시간_:_{ts.ToRemainTime()}";
        m_element.txtContent_Exp.text = $"경험치_:_+{_mission.dbGradeData.missionXp.AmountKMBT()}";
        m_element.txtContent_Exp.transform.parent.ForceRebuildLayout();

        UpdateHero(true);
    }

    void UpdateHero(bool _isForceUpdate)
    {
        var myHeroes = DataManager.userInfo.myHero.Where(x => m_missionData.heroes.Contains(x.key)).ToList();

        var parent = m_element.pHeroIcon;
        int i = 0;

        CoreStatType coreStat = m_missionData.dbData.statType;
        int totalCoreStat = 0;
        for (; i < myHeroes.Count; i++)
        {
            var heroData = myHeroes[i];

            bool isNew = i == parent.childCount;
            var item = isNew ? Instantiate(m_element.baseHeroIcon, parent) :
                parent.GetChild(i).GetComponent<HeroIconComponent>();

            item.gameObject.SetActive(true);
            item.SetHeroData(heroData, (_icon, _) => OpenHeroInfoPopupAsync(_icon.data).Forget(), null, _isForceUpdate);

            totalCoreStat += heroData.resultCoreStat[coreStat];
        }

        for (; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(false);

        parent.ForceRebuildLayout();

        var percent = Mathf.Min(1f, totalCoreStat / (float)m_missionData.coreStatMax);
        m_element.gauge.fillAmount = percent;
        percent *= 100;
        m_element.gauge.textAmount = $"({percent:0.##}%) {totalCoreStat.AmountKMBT()}/{m_missionData.coreStatMax.AmountKMBT()}";
        m_element.btnAdd.text = $"({myHeroes.Count}/6)";


        m_missionData.percentStat = percent;

        // 보상 리스트 업데이트
        m_element.reward.SetRewardList(m_missionData, percent);
    }

    async UniTask OpenHeroInfoPopupAsync(HeroInfoData _data)
    {
        if (m_missionData.isRunning == true)
            return;

        Utils.SetActivePunch(m_element.panel, false);

        if (m_popupHeroInfo == null)
        {
            m_popupHeroInfo = await PopupManager.instance.OpenPopupAsync<PopupHeroInfo>(PopupType.Hero_HeroInfo, _data);
            m_popupHeroInfo.isDontDestroy = true;
        }
        else
            await m_popupHeroInfo.SetHeroInfoDataAsync(_data);

        await UniTask.WaitUntil(() => m_popupHeroInfo.statusType != StatusType.Wait, cancellationToken: destroyCancellationToken);

        Utils.SetActivePunch(m_element.panel, true);

        if (m_popupHeroInfo.isNeedUpdate)
            UpdateHero(true);
    }

    async UniTask OpenHeroListPopupAsync()
    {
        Utils.SetActivePunch(m_element.panel, false);

        var popup = m_element.popupHeroList;
        popup.Open(m_missionData.DeepClone());

        await UniTask.WaitUntil(() => popup.statusType != StatusType.Wait, cancellationToken: destroyCancellationToken);

        Utils.SetActivePunch(m_element.panel, true);

        if (popup.isUpdated == true)
        {
            m_missionData.heroes.Clear();
            m_missionData.heroes.AddRange(popup.heroes);

            UpdateHero(true);
        }
    }

    protected virtual void OnButton_Start()
    {
        // 진행중인 미션일 경우
        if (m_missionData.isRunning == true)
        {
            // 끝났으면 꺼주고
            if (m_missionData.isFinished)
                OpenResultAsync().Forget();
            // 아직 안끝났으면 시간석 사용팝업 열어주자
            else
                OpenUseTimeStoneAsync().Forget();

            return;
        }

        if (m_missionData.percentStat < 10)
        {
            PopupManager.instance.AlertShow("요구_능력치를_10%이상_달성해줘!");
            return;
        }

        DataManager.castle.mission.StartMissionAsync(m_missionData, _result =>
        {
            if (_result == StatusType.Success)
            {
                resultType = StatusType.Success;
                Close();
            }
        }).Forget();
    }

    async UniTask OpenResultAsync()
    {
        Utils.SetActivePunch(m_element.panel, false);
        await UniTask.WaitForSeconds(.1f);
        await PopupManager.instance.GetPopup<PopupCastleMissionComponent>(PopupType.Castle_Mission)
            .OpenResultAsync_FromInfoPopup(m_missionData);

        resultType = StatusType.Cancel;
        gameObject.SetActive(false);
    }

    PopupUseTimeStoneComponent m_popupTimeStone;
    async UniTask OpenUseTimeStoneAsync()
    {
        bool isSuccessed = await PopupManager.instance.GetPopup<PopupCastleMissionComponent>(PopupType.Castle_Mission).OpenUseTimeStoneAsync(m_missionData, _popup => m_popupTimeStone = _popup);

        m_popupTimeStone = null;
        if (isSuccessed == true)
        {
            m_missionData = DataManager.castle.mission.GetMissionData(m_missionData.idx);
            TimerAsync().Forget();
        }
    }

    public virtual bool CloseEscape()
    {
        if (m_popupHeroInfo != null && m_popupHeroInfo.gameObject.activeSelf == true)
            return false;

        if (m_element.popupHeroList.CloseEscape() == false)
            return false;

        if (gameObject.activeSelf == true)
        {
            Close();
            return false;
        }

        return true;
    }

    public override void Close()
        => CloseAsync().Forget();

    async UniTask CloseAsync()
    {
        if (resultType == StatusType.Wait)
            resultType = StatusType.Cancel;

        await Utils.SetActivePunchAsync(m_element.panel, false);
        gameObject.SetActive(false);
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [Serializable]
    protected struct ElementData
    {
        public TextMeshProUGUI txtTitle;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtContent_Time;
        public TextMeshProUGUI txtContent_Exp;

        public Transform pHeroIcon;
        public HeroIconComponent baseHeroIcon;
        public GaugeHelper gauge;
        public ButtonHelper btnAdd;

        public PopupCastleMission_Popup_Info_Reward reward;

        public ButtonHelper btnStart;

        public PopupCastleHeroListComponent_Mission popupHeroList;

        public void Initialize(Transform _transform)
        {
            btnStart = _transform.GetComponent<ButtonHelper>("Panel/btn_start");

            txtTitle = panel.GetComponent<TextMeshProUGUI>("txt_title");
            txtName = panel.GetComponent<TextMeshProUGUI>("Info/txt_name");
            txtContent_Time = panel.GetComponent<TextMeshProUGUI>("Info/Content/txt_time");
            txtContent_Exp = panel.GetComponent<TextMeshProUGUI>("Info/Content/txt_exp");

            pHeroIcon = panel.Find("Info/Heroes/Panel");
            if (pHeroIcon != null)
            {
                baseHeroIcon = pHeroIcon.GetComponent<HeroIconComponent>("Slot_Hero");
                btnAdd = pHeroIcon.parent.GetComponent<ButtonHelper>("btn_add");
            }

            gauge = panel.GetComponent<GaugeHelper>("Info/Status");
            reward = panel.GetComponent<PopupCastleMission_Popup_Info_Reward>("Reward");

            popupHeroList = _transform.parent?.GetComponent<PopupCastleHeroListComponent_Mission>("Castle_HeroList");
        }

        public Transform panel => btnStart.transform.parent;
    }
    #endregion VALIDATE
}
