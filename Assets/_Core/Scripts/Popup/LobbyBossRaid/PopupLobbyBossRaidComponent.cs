using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyBossRaidComponent : BasePopupComponent
{
    PopupLobbyBossRaidComponent() : base(PopupType.LobbyBossRaid) { }

    CharacterComponent m_characterBoss;

    [SerializeField]
    List<BossPositionData> m_posData = new();


    protected override void Awake()
    {
        base.Awake();

        m_element.panel.gameObject.SetActive(true);
        m_element.img.enabled = true;

        var popup = transform.Find("Popup");
        for (int i = 0; i < popup.childCount; i++)
            popup.GetChild(i).gameObject.SetActive(false);

        m_element.btnRanking.onClick.AddListener(() => OnButtonAsync_Ranking().Forget());
        m_element.btnHero.onClick.AddListener(OnButton_Hero);
        m_element.btnStart.onClick.AddListener(() => OnButtonAsync_Start().Forget());
    }

    private void Start()
	{
		Utils.SetActivePunch(m_element.panel, true);

		var dataRaid = DataManager.bossRaid.data;
		m_element.txtDifficult.text = $"[{TableManager.stringTable.GetGradeType(dataRaid.gradeMin)}~{TableManager.stringTable.GetGradeType(dataRaid.gradeMax)}]";

		DoLoadBossCharacter().Forget();
		TimerAsync_Round().Forget();
		OnUpdateSeasonTimerAsync().Forget();

		Utils.WaitEscape(this, () =>
        {
            if (m_element.popupRanking.CloseEscape() == false)
                return;

            Close();
        }, _token: destroyCancellationToken);
    }

    async UniTask DoLoadBossCharacter()
    {
        var raidData = DataManager.bossRaid.data;

        var key = raidData.keyBoss + "_BossRaid";

        // 일단 테스트로 있는거기때문에 없애주자.
        m_characterBoss = null;
        for (int i = 0; i < m_element.pHero.childCount; i++)
        {
            var item = m_element.pHero.GetChild(i).gameObject;
            if (key == item.name)
            {
                m_characterBoss = item.GetComponent<CharacterComponent>();
                break;
            }
            Destroy(item);
        }

        if (m_characterBoss == null)
        {
            var asset = await AddressableManager.instance.GetHeroCharacterAsync(key);
            m_characterBoss = Instantiate(asset, m_element.pHero).GetComponent<CharacterComponent>();
        }

        if (m_characterBoss == null)
            return;

        m_characterBoss.transform.localPosition = Vector3.zero;

        var anchorPos = m_element.rtHero.anchoredPosition;
        anchorPos.x = m_posData.Find(x => x.key == raidData.keyBoss).x;
        m_element.rtHero.anchoredPosition = anchorPos;

        // 이전 라운드 정보
        if (raidData.tickPrevRound == 0)
            m_element.txtInfoPrevRound.text = "이전_라운드_정보_없음";
        else
            m_element.txtInfoPrevRound.text = $"이전_라운드_ :_[{TableManager.stringTable.GetGradeType(raidData.prevGrade)}]\n{raidData.dtPrevRound.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")}";
	}

	async UniTask OnButtonAsync_Start()
	{
		m_element.btnStart.interactable = false;
		m_characterBoss.anim.Play(CharacterAnimType.Skill, 0, 0.4f);
		m_characterBoss.anim.SetSpeed(1.2f);

		await UniTask.WaitForSeconds(1f);

		BossRaidWorker.instance.InitializeAsync(BossRaidWorker.BossRaidType.LuBu).Forget();
	}

	async UniTask OnUpdateSeasonTimerAsync()
    {
        var dtEnd = DataManager.bossRaid.data.dtEndSeason;

        while (true)
        {
            var ts = dtEnd - Utils.GetUTC();

            if (ts.TotalSeconds <= 0)
                break;

            m_element.txtSeasonRemainTimer.text = ts.ToRemainTime(40, _isStringMode: true);
            await UniTask.WaitForEndOfFrame(cancellationToken: destroyCancellationToken);
        }

        m_element.txtSeasonRemainTimer.text = "_정산중_";
    }

    async UniTask TimerAsync_Round()
    {
        m_element.btnStart.interactable = false;

        if (DataManager.bossRaid.data.tickNextRound == 0)
        {
            m_element.btnStart.text = "_미출현_";
            m_element.txtRoundRemainTimer.text = "";
            m_element.btnStart.TMPText.alignment = TextAlignmentOptions.Center;

            await UniTask.WaitUntil(() => DataManager.bossRaid.data.tickNextRound > 0, cancellationToken: destroyCancellationToken);
        }

        // 시작까지 남은 시간
        var dtStart = DataManager.bossRaid.data.dtNextRound.AddSeconds(-Configure.instance.timeGapFromServer);
        m_element.btnStart.text = "_대기_";
        m_element.btnStart.TMPText.alignment = TextAlignmentOptions.Top;

        while (true)
        {
            var ts = dtStart - DateTime.UtcNow;

            if (ts.TotalSeconds <= 0)
                break;

            m_element.txtRoundRemainTimer.text = $"({ts.ToRemainTime(25, _isStartMinute: true)} 후 시작)";
            await UniTask.WaitForEndOfFrame(cancellationToken: destroyCancellationToken);
        }

        // 진행 남은 시간
        var dtFinished = DataManager.bossRaid.data.dtEndRound.AddSeconds(-Configure.instance.timeGapFromServer);

        m_element.btnStart.interactable = true;
        m_element.btnStart.text = "_참가_";

        while (true)
        {
            var ts = dtFinished - DateTime.UtcNow;

            if (ts.TotalSeconds <= 0)
                break;

            m_element.txtRoundRemainTimer.text = $"<color=#{Palette.htmlString_Up}>({ts.ToRemainTime(25, _isStartMinute: true)} 남음)</color>";
            await UniTask.WaitForEndOfFrame(cancellationToken: destroyCancellationToken);
        }

        //m_element.txtRoundRemainTimer.text = "";
        //m_element.btnStart.TMPText.alignment = TextAlignmentOptions.Center;

        await UniTask.WaitForEndOfFrame(cancellationToken: destroyCancellationToken);
        TimerAsync_Round().Forget();
    }

    async UniTask OnButtonAsync_Ranking()
    {
        await DataManager.bossRaid.DoLoadAsync_RankData();
        await Utils.SetActivePunchAsync(m_element.panel, false);

        m_element.popupRanking.OpenPopup();
        await UniTask.WaitUntil(() => m_element.popupRanking.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

        Utils.SetActivePunch(m_element.panel, true);
    }

    void OnButton_Hero()
    {
        //m_characterBoss.anim.PlayAttack();
    }

    public override void Close()
        => Utils.SetActivePunch(m_element.panel, false, _callback: base.Close);

    #region VALIDATE
    public override void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image img;
        public Transform panel;

        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtPoint;

        public TextMeshProUGUI txtDifficult;
        public TextMeshProUGUI txtBossName;
        public TextMeshProUGUI txtSeasonRemainTimer; // 라운드 남은 시간
        public TextMeshProUGUI txtInfoPrevRound;
        public TextMeshProUGUI txtRoundRemainTimer;      // 보스 등장 남은 시간

        public ButtonHelper btnRanking;
        public ButtonHelper btnStart;
        public ButtonHelper btnShop;

        public Button btnHero;
        public Transform pHero;

        public PopupLobbyBossRaid_PopupRanking popupRanking;

        public void Initialize(Transform _transform)
        {
            img = _transform.GetComponent<Image>();
            panel = _transform.Find("Panel");

            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_point");
            txtDifficult = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_difficult");
            txtBossName = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_title");
            txtSeasonRemainTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_remain");
            txtInfoPrevRound = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_prev_round_info");
            txtRoundRemainTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/Button/btn_start/txt_timer");

            btnStart = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_start");
            btnRanking = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_ranking");
            btnShop = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_shop");

            btnHero = _transform.GetComponent<Button>("Panel/btn_character");
            pHero = btnHero.transform.Find("Panel");

            popupRanking = _transform.GetComponent<PopupLobbyBossRaid_PopupRanking>("Popup/Ranking");
        }

        public RectTransform rtHero => (RectTransform)btnHero.transform;
    }
    #endregion VALIDATE

    [System.Serializable]
    struct BossPositionData
    {
        public string key;
        public float x;
    }
}
