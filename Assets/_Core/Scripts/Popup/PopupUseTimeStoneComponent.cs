using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupUseTimeStoneComponent : BasePopupComponent
{
    enum TimeStoneCountType
    {
        type_1h,
        type_1d,
        type_max,
    }

    PopupUseTimeStoneComponent() : base(PopupType.UseTimeStone) { }

    public StatusType statusType { get; private set; }

    int m_idx;

    int m_myTimeStone;      // 내 보유 시간석
    int m_secTimeStone;     // 궁성에 따른 시간석 하나가 감속할 초
    int m_minuteAD;            // 광고당 감속 초
    int m_totalSeconds;     // 남은 초
    int m_countTimeStone;   // 사용하게 될 시간석 갯수

    TimeStoneADCountData m_adCountData;

    TimeStoneCountType m_countType;
    const string c_key = "pp_timestone_count";

    public int timeBonus { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        m_element.btnPanel.onClick.AddListener(() => { if (m_element.rtCountMenu.gameObject.activeSelf == true) SetActiveCountPanel(false); });
        m_element.btnMenu.onClick.AddListener(OnButton_Menu);

        // test
        m_myTimeStone = 3234;
        m_element.txtAsset.text = $"{m_myTimeStone:#,0}";
        m_element.txtAsset.transform.ForceRebuildLayout();

        m_adCountData = PPWorker.Get<TimeStoneADCountData>(c_key + "_ad");

        var countType = TimeStoneCountType.type_1h + PPWorker.Get<int>(c_key);
        m_countType = countType - 1;
        SetHeroCountType(countType);

        for (int i = 0; i < m_element.btnCounts.Length; i++)
        {
            var b = m_element.btnCounts[i];

            var type = (TimeStoneCountType)i;
            b.text = GetStringCountType(type);
            b.onClick.AddListener(() =>
            {
                SetHeroCountType(type);

                PPWorker.Set(c_key, (int)type);
            });
        }

        m_element.rtCountMenu.gameObject.SetActive(false);

        DataManager.castle.GetSecondTimeStone((_t, _a) =>
        {
            m_secTimeStone = _t;
            m_minuteAD = _a;
            m_element.btnAD.text = $"-{m_minuteAD}분 <color=#6D6D6D><size=90%>({m_adCountData.countAD}/5)";
        });

        m_element.btnTimeStone.onClick.AddListener(OnButton_TimeStone);
        m_element.btnAD.onClick.AddListener(OnButton_AD);
    }

    public override void OpenPopup(params object[] _args)
    {
        m_idx = _args.Length == 0 ? -1 : (int)_args[0];
        m_countTimeStone = -1;

        statusType = StatusType.Wait;
        Utils.SetActivePunch(m_element.panel, true);
    }

    public void UpdateRemainTime(TimeSpan _ts, int _idxMission = -1)
    {
        if (statusType != StatusType.Wait || m_idx != _idxMission)
            return;

        m_element.txtTimer.text = _ts.ToRemainTime(65);
        m_totalSeconds = (int)_ts.TotalSeconds;

        UpdateTimeStoneCount();
    }

    void UpdateTimeStoneCount()
    {
        int count = Mathf.CeilToInt(m_totalSeconds / (float)m_secTimeStone);

        var countTimeStone = m_countType switch
        {
            TimeStoneCountType.type_1h => Mathf.Min(count, Mathf.CeilToInt(60 * 60 / (float)m_secTimeStone)),
            TimeStoneCountType.type_1d => Mathf.Min(count, Mathf.CeilToInt(60 * 60 * 24 / (float)m_secTimeStone)),
            _ => count
        };

        countTimeStone = Mathf.Min(m_myTimeStone, countTimeStone);

        if (m_countTimeStone == countTimeStone)
            return;

        m_countTimeStone = countTimeStone;

        var dtNow = DateTime.Now;
        var ts = dtNow.AddSeconds(m_countTimeStone * m_secTimeStone) - dtNow;

        m_element.txtBonus.text = $"(-{ts.ToRemainTime(30)})";
        //if (ts.TotalMinutes > 0)
        //    m_element.txtBonus.text = $"(-{Utils.MSpace($"{Mathf.FloorToInt((float)ts.TotalHours):00}:{ts.ToString(@"mm\:ss")}", 30)})";
        //else
        //    m_element.txtBonus.text = $"(-{Utils.MSpace(ts.TotalSeconds.ToString("0.00"), 30)}s)";

        m_element.btnTimeStone.text = $"{m_countTimeStone.AmountKMBT()} <color=#6D6D6D><size=90%>({m_secTimeStone}s/ea)";
    }

    void OnButton_TimeStone()
    {
        timeBonus = m_countTimeStone * m_secTimeStone;

        statusType = StatusType.Success;
        Close();
    }

    void OnButton_AD()
    {
        timeBonus = m_minuteAD * 60;
        m_adCountData.countAD--;
        SaveData_ADCount();

        statusType = StatusType.Success;
        Close();
    }

    void OnButton_Menu()
    {
        bool isActive = m_element.rtCountMenu.gameObject.activeSelf == false;
        SetActiveCountPanel(isActive);
    }

    void SetActiveCountPanel(bool _isActive)
    {
        if (_isActive == true)
            m_element.rtCountMenu.gameObject.SetActive(true);

        m_element.btnMenu.interactable = false;

        var rtPanel = m_element.rtCountMenu;
        rtPanel.DOAnchorPosY(_isActive ? 0 : -rtPanel.rect.height, 0.1f)
            .OnComplete(() =>
            {
                m_element.btnMenu.interactable = true;

                if (_isActive == false)
                    m_element.rtCountMenu.gameObject.SetActive(false);
            });
    }

    void SetHeroCountType(TimeStoneCountType _type)
    {
        if (m_countType == _type)
            return;

        m_countType = _type;
        int curIdx = (int)m_countType;
        for (int i = 0; i < m_element.btnCounts.Length; i++)
            m_element.btnCounts[i].SetDrawSelect(i == curIdx);

        m_element.btnMenu.text = GetStringCountType(_type);

        UpdateTimeStoneCount();
    }

    string GetStringCountType(TimeStoneCountType _type)
        => _type switch
        {
            TimeStoneCountType.type_1h => "1H",
            TimeStoneCountType.type_1d => "1D",
            _ => "최대"
        };

    public bool CloseEscape()
    {
        Close();
        return true;
    }

    public override void Close()
        => CloseAsync().Forget();

    async UniTask CloseAsync()
    {
        if (statusType == StatusType.Wait)
            statusType = StatusType.Cancel;

        dimm.SetActive(false);

        await Utils.SetActivePunchAsync(m_element.panel, false);
        base.Close();
    }

    void SaveData_ADCount()
        => PPWorker.Set(c_key + "_ad", m_adCountData);

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);
    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtAsset;
        public TextMeshProUGUI txtTimer;
        public TextMeshProUGUI txtBonus;

        public Button btnPanel;
        public ButtonHelper btnTimeStone;
        public ButtonHelper btnAD;

        public RectTransform rtCountMenu;
        public ButtonHelper btnMenu;
        public ButtonHelper[] btnCounts;

        public void Initialize(Transform _transform)
        {
            txtAsset = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_asset");
            txtTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_timer");
            txtBonus = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_bonus");

            btnTimeStone = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_start");
            btnAD = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_ad");

            rtCountMenu = _transform.GetComponent<RectTransform>("Panel/Count/Viewport/Panel");
            btnMenu = _transform.GetComponent<ButtonHelper>("Panel/Count/btn_open");
            btnCounts = rtCountMenu.GetComponentsInChildren<ButtonHelper>();

            btnPanel = panel.GetComponent<Button>();
        }

        public Transform panel => txtTimer.transform.parent;
    }
    #endregion VALIDATE

    struct TimeStoneADCountData
    {
        [JsonProperty] int count_ad;
        [JsonProperty] long tickDate;

        DateTime dt => new DateTime(tickDate, DateTimeKind.Utc);

        public int countAD
        {
            get
            {
                CheckDate();
                return count_ad;
            }
            set
            {
                count_ad = value;
            }
        }

        void CheckDate()
        {
            if (Utils.GetUTC().Date > dt.Date)
            {
                count_ad = 5;
                tickDate = Utils.GetUTC().Ticks;
            }
        }
    }
}
