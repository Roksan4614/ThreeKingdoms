using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournamentComponent : BasePopupComponent
{
    enum TournamentPopupType
    {
        None = -1,

        History,
        Ranking,
        AD,
        Batch,
        Shop,
        Reward,

        MAX
    }
    PopupTournamentComponent() : base(PopupType.LobbyTournament) { }

    protected override void Awake()
    {
        base.Awake();

        m_element.imgTemp.enabled = true;

        // 하단 버튼 세팅
        for (int i = 0; i < m_element.buttons.Length; i++)
        {
            var type = (TournamentPopupType)i;
            m_element.buttons[i].onClick.AddListener(() => OpenPopupAsync(type).Forget());
        }

        // 팝업 다 끄기
        var popup = transform.Find("Popup");
        for (int i = 0; i < popup.childCount; i++)
            popup.GetChild(i).gameObject.SetActive(false);

        m_element.btnRefresh.onClick.AddListener(() => OnButtonAsync_Refresh().Forget());
    }

    private void Start()
    {
        Utils.WaitEscape(this, () =>
        {
            if (m_element.popupRanking.CloseEscape() &&
                m_element.popupBatch.CloseEscape() &&
                m_element.popupUserInfo.CloseEscape() &&
                m_element.popupRewardInfo.CloseEscape())
            {
                if (m_popupHistory == null ||  m_popupHistory.CloseEscape() == true)
                    Close();
            }
        });

        LoadDataAsync().Forget();
    }

    private void OnDestroy() => TournamentWorker.instance?.StopTimer();

    async UniTask LoadDataAsync()
    {
        m_element.panel.gameObject.SetActive(false);

        await TournamentWorker.instance.InitailizeAsync();

        m_element.txtTier.text = $"[{TableManager.stringTable.GetGradeRankType(TournamentWorker.data.grade)}]";
        m_element.txtRank.text = "_현재순위\n<size=150%>";
        m_element.txtRank.text += TournamentWorker.data.rank == 0 ? "- 위" : $"{TournamentWorker.data.rank:#,0}_위";
        m_element.txtPoint.text = "_점수\n<size=150%>";
        m_element.txtPoint.text += $"{TournamentWorker.data.point:#,0}";

        SetPlayCount();
        SetRefreshCount();
        SetUserList();

        Utils.SetActivePunch(m_element.panel, true);

        var batchData = TournamentWorker.instance.GetBatchData(true);
        m_element.txtPower.text = batchData.totalPower.AmountKMBT(_isMBT: true);
        await m_element.panelBatch.SetBatchDataAsync(batchData);
    }

    public void SetPlayCount()
    {
        m_element.txtPlayCount.text = $"일일_입장_가능_횟수: {TournamentWorker.data.countPlay}";
        m_element.buttons[(int)TournamentPopupType.AD].text = $"{TournamentWorker.data.countAD}/3";
    }

    void SetRefreshCount()
    {
        var tournamentData = TournamentWorker.data;
        int count = tournamentData.countRefresh;

        // 최대치가 아니라면 카운트를 쳐줘야 해
        if (count < 3)
            TimerAsync().Forget();
        else
            m_element.txtRefreshTimer.gameObject.SetActive(false);

        m_element.iconAsset.SetActive(tournamentData.isFreeRefresh == false);

        if (tournamentData.isFreeRefresh == true)
            m_element.txtRefreshCount.text = $"{TournamentWorker.data.countRefresh}/3";
        else
            m_element.txtRefreshCount.text = "1,200";

        m_element.iconAsset.transform.parent.ForceRebuildLayout();
    }
    void SetUserList()
    {
        var user = TournamentWorker.data.battleUserList;
        for (int i = 0; i < m_element.slots.Length; i++)
            m_element.slots[i].SetUserData(user[i],
                _rankerData => OnButtonAsync_Start(_rankerData).Forget(),
                _rankerData => m_element.popupUserInfo.OpenAsync(_rankerData.uid).Forget()
                );
    }

    async UniTask TimerAsync()
    {
        var dtEnd = new System.DateTime(TournamentWorker.data.tickRefresh, System.DateTimeKind.Utc);
        var prevCount = TournamentWorker.data.countRefresh;

        m_element.txtRefreshTimer.gameObject.SetActive(true);

        while (true)
        {
            var ts = dtEnd - Utils.GetUTC();

            m_element.txtRefreshTimer.text = ts.ToRemainTime(15, _isStartMinute: true) + $" 후_무료_갱신_횟수_추가";

            if (ts.TotalSeconds < 0)
                break;

            await UniTask.NextFrame(destroyCancellationToken);
        }

        await UniTask.WaitUntil(() => prevCount != TournamentWorker.data.countRefresh, cancellationToken: destroyCancellationToken);


        m_element.txtRefreshTimer.gameObject.SetActive(false);
        SetRefreshCount();
    }

    bool m_isEnter = false;
    async UniTask OnButtonAsync_Start(RankerUserData _rankerData)
    {
        if (m_isEnter == true)
            return;

        m_isEnter = true;
        if (TournamentWorker.data.countPlay <= 0)
        {
            if (TournamentWorker.data.countAD <= 0)
                PopupManager.instance.AlertShow("플레이_가능_횟수가_초과되었습니다.");
            else if (await TournamentWorker.instance.ShowAdsAsync())
                SetPlayCount();

            m_isEnter = false;
            return;
        }

        TournamentWorker.instance.EnterBattleAsync(_rankerData.uid).Forget();

        //test
        SetPlayCount();
    }

    async UniTask OnButtonAsync_Refresh()
    {
        if (TournamentWorker.data.countRefresh <= 0)
        {
            int cost = 1200;

            if (DataManager.userInfo.rice < cost)
            {
                PopupManager.instance.AlertShow("재화가_부족합니다.");
                return;
            }

            var result = await PopupManager.instance.OpenModalAsync("재화를_사용해서_갱신하시겠습니까?");

            if (result == StatusType.Success)
                DataManager.userInfo.AddAsset(ItemType.Rice, -cost);
            else
                return;
        }
        m_element.btnRefresh.interactable = false;

        await TournamentWorker.instance.RefreshListAsync();

        m_element.btnRefresh.interactable = true;
        SetUserList();
        SetRefreshCount();
    }

    PopupTournamentHistoryComponent m_popupHistory;
    async UniTask OpenPopupAsync(TournamentPopupType _popupType)
    {
        switch (_popupType)
        {
            case TournamentPopupType.History:
                {
                    if (m_popupHistory == null)
                    {
                        m_popupHistory = await PopupManager.instance.OpenPopupAsync<PopupTournamentHistoryComponent>(PopupType.LobbyTournament_History);
                        m_popupHistory.transform.SetParent(m_element.popupBatch.transform.parent);
                    }
                    else
                        m_popupHistory.OpenPopup_Rebirth();
                }
                break;
            case TournamentPopupType.Ranking:
                await m_element.popupRanking.OpenPopupAsync();
                break;
            case TournamentPopupType.AD:
                {
                    if (await TournamentWorker.instance.ShowAdsAsync() == true)
                        SetPlayCount();
                }
                break;
            case TournamentPopupType.Batch:
                {
                    // 열었는데 공격 배치가 바뀌었어? 그럼 업데이트 해줘야지
                    if (await m_element.popupBatch.OpenAsync())
                    {
                        var batchData = TournamentWorker.instance.GetBatchData(true);
                        m_element.txtPower.text = batchData.totalPower.AmountKMBT(_isMBT: true);
                        await m_element.panelBatch.SetBatchDataAsync(batchData);
                    }
                }
                break;
            case TournamentPopupType.Reward:
                await m_element.popupRewardInfo.OpenAsync();
                break;
        }
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
        public Image imgTemp;

        public TextMeshProUGUI txtTier;
        public TextMeshProUGUI txtRank;
        public TextMeshProUGUI txtPoint;
        public TextMeshProUGUI txtPower;

        public TextMeshProUGUI txtPlayCount;

        public ButtonHelper[] buttons;

        public PopupTournament_Batch_Panel panelBatch;

        public PopupTournament_Ranking popupRanking;
        public PopupTournament_Batch popupBatch;
        public PopupTournament_UserInfo popupUserInfo;
        public PopupTournament_RewardInfo popupRewardInfo;

        public PopupTournament_Slot[] slots;
        public ButtonHelper btnRefresh;
        public TextMeshProUGUI txtRefreshTimer;
        public GameObject iconAsset;
        public TextMeshProUGUI txtRefreshCount;

        public void Initialize(Transform _transform)
        {
            imgTemp = _transform.GetComponent<Image>();

            txtTier = _transform.GetComponent<TextMeshProUGUI>("Panel/TierInfo/txt_tier");
            txtRank = _transform.GetComponent<TextMeshProUGUI>("Panel/TierInfo/txt_rank");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/TierInfo/txt_point");
            txtPower = _transform.GetComponent<TextMeshProUGUI>("Panel/Power/Text");
            txtPlayCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_play_count");

            panelBatch = _transform.GetComponent<PopupTournament_Batch_Panel>("Panel/Batch");

            buttons = new ButtonHelper[(int)TournamentPopupType.MAX];
            for (var i = TournamentPopupType.None + 1; i < TournamentPopupType.MAX; i++)
                buttons[(int)i] = _transform.GetComponent<ButtonHelper>($"Panel/Button/btn_{i.ToString().ToLower()}");

            popupRanking = _transform.GetComponent<PopupTournament_Ranking>("Popup/Ranking");
            popupBatch = _transform.GetComponent<PopupTournament_Batch>("Popup/Batch");
            popupUserInfo = _transform.GetComponent<PopupTournament_UserInfo>("Popup/UserInfo");
            popupRewardInfo = _transform.GetComponent<PopupTournament_RewardInfo>("Popup/RewardInfo");

            slots = _transform.Find("Panel/List").GetComponentsInChildren<PopupTournament_Slot>();
            btnRefresh = _transform.GetComponent<ButtonHelper>("Panel/btn_refresh");
            txtRefreshTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/btn_refresh/Desc/txt_timer");
            txtRefreshCount = _transform.GetComponent<TextMeshProUGUI>("Panel/btn_refresh/Desc/txt_count");
            iconAsset = _transform.Find("Panel/btn_refresh/Desc/Icon").gameObject;
        }

        public Transform panel => txtPlayCount.transform.parent;
        public Transform list => slots[0].transform.parent;

    }
    #endregion VALIDATE

}
