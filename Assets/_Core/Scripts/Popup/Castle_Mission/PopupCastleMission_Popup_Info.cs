using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CastleMissionData = Data_Castle_Mission.CastleMissionData;

public class PopupCastleMission_Popup_Info : BasePopupComponent
{
    PopupCastleMission_Popup_Info() : base(PopupType.NONE) { }

    CastleMissionData m_missionData;

    PopupHeroInfo m_popupHeroInfo;

    public StatusType resultType { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        m_element.btnStart.onClick.AddListener(OnButton_Start);

        m_element.baseHeroIcon.transform.SetParent(m_element.pHeroIcon.parent);
        m_element.baseHeroIcon.gameObject.SetActive(false);

        m_element.btnAdd.onClick.AddListener(() => OpenHeroListPopupAsync().Forget());
    }

    public void Open(CastleMissionData _mission)
    {
        m_missionData = _mission;
        m_missionData.heroes = new();

        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);
        resultType = StatusType.Wait;

        m_element.txtTitle.text = $"임무_:_[{TableManager.stringTable.GetString($"GRADE_{_mission.grade.ToString().ToUpper()}")}]";
        m_element.txtName.text = _mission.missionNameStat;
        m_element.gauge.textTitle = $"고유_능력({TableManager.stringTable.GetString($"CORESTAT_{_mission.dbData.statType.ToString().ToUpper()}")})_요구치";

        // 자동으로 추가해줘보자
        {
            var coreStat = m_missionData.dbData.statType;
            int coreStatMax = m_missionData.coreStatMax;

            var myHero = DataManager.userInfo.myHero.Where(x => DataManager.castle.mission.GetMissionIdxBatchHero(x.key) == -1)
                .OrderByDescending(x => x.resultCoreStat[coreStat]);

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

        //소요시간
        var now = DateTime.Now;
        TimeSpan ts = now.AddSeconds(_mission.dbGradeData.durationSeconds) - now;
        m_element.txtContent_Time.text = $"소요시간_:_{ts.TotalHours:00}:{ts.ToString(@"mm\:ss")}";
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
        Utils.SetActivePunch(m_element.panel, false);

        if (m_popupHeroInfo == null)
        {
            m_popupHeroInfo = await PopupManager.instance.OpenPopup<PopupHeroInfo>(PopupType.Hero_HeroInfo, _data);
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
        popup.Open(m_missionData);

        await UniTask.WaitUntil(() => popup.statusType != StatusType.Wait, cancellationToken: destroyCancellationToken);

        Utils.SetActivePunch(m_element.panel, true);

        if (popup.isUpdated == true)
        {
            m_missionData.heroes.Clear();
            m_missionData.heroes.AddRange(popup.heroes);

            UpdateHero(true);
        }
    }

    void OnButton_Start()
    {
        if (m_missionData.percentStat < 10)
        {
            PopupManager.instance.AlertShow("요구_능력치를_10%이상_달성해줘!");
            return;
        }

        DataManager.castle.mission.StartMission(m_missionData);

        resultType = StatusType.Success;
        Close();
    }

    public bool CloseEscape()
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
    ElementData m_element;

    [Serializable]
    struct ElementData
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
            baseHeroIcon = pHeroIcon.GetComponent<HeroIconComponent>("Slot_Hero");
            btnAdd = pHeroIcon.parent.GetComponent<ButtonHelper>("btn_add");
            gauge = panel.GetComponent<GaugeHelper>("Info/Status");

            reward = panel.GetComponent<PopupCastleMission_Popup_Info_Reward>("Reward");

            popupHeroList = _transform.parent.GetComponent<PopupCastleHeroListComponent_Mission>("Castle_HeroList");
        }

        public Transform panel => btnStart.transform.parent;
    }
    #endregion VALIDATE
}
