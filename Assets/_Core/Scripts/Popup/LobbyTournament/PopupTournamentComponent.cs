using Cysharp.Threading.Tasks;
using Rev9.Tournament;
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
                if (PopupManager.instance.IsOpenPopup(PopupType.LobbyTournament_History) == false)
                    Close();
            }
        });

        LoadDataAsync().Forget();
    }

    async UniTask LoadDataAsync()
    {
        m_element.panel.gameObject.SetActive(false);

        await TournamentWorker.instance.InitailizeAsync();
        Utils.SetActivePunch(m_element.panel, true);
    }

    //void ResetData()
    //{
    //    m_element.txtTier.text = "";
    //    m_element.txtRank.text = $"현재_순위";//\n<size=150%>{} 위</size>";
    //    m_element.txtPoint.text = $"점수";

    //    m_element.txtPlayCount.text = "";

    //    foreach (var slot in m_element.slots)
    //        slot.ResetData();
    //}

    async UniTask OpenPopupAsync(TournamentPopupType _popupType)
    {
        switch (_popupType)
        {
            case TournamentPopupType.History:
                PopupManager.instance.OpenPopup(PopupType.LobbyTournament_History);
                break;
            case TournamentPopupType.Ranking:
                await m_element.popupRanking.OpenPopupAsync();
                break;
            case TournamentPopupType.AD:
                await ShowADAsync();
                break;
            case TournamentPopupType.Batch:
                await m_element.popupBatch.OpenAsync();
                break;
            case TournamentPopupType.Reward:
                await m_element.popupRewardInfo.OpenAsync();
                break;
        }
    }

    async UniTask ShowADAsync()
    {
        await UniTask.NextFrame();
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

        public TextMeshProUGUI txtPlayCount;

        public ButtonHelper[] buttons;

        public PopupTournament_Batch_Panel panelBatch;

        public PopupTournament_Ranking popupRanking;
        public PopupTournament_Batch popupBatch;
        public PopupTournament_UserInfo popupUserInfo;
        public PopupTournament_RewardInfo popupRewardInfo;

        public PopupTournament_Slot[] slots;

        public void Initialize(Transform _transform)
        {
            imgTemp = _transform.GetComponent<Image>();

            txtTier = _transform.GetComponent<TextMeshProUGUI>("Panel/TierInfo/txt_tier");
            txtRank = _transform.GetComponent<TextMeshProUGUI>("Panel/TierInfo/txt_rank");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/TierInfo/txt_point");
            txtPlayCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_play_count");

            panelBatch = _transform.GetComponent<PopupTournament_Batch_Panel>("Panel/Batch");

            buttons = new ButtonHelper[(int)TournamentPopupType.MAX];
            for (var i = TournamentPopupType.None + 1; i < TournamentPopupType.MAX; i++)
                buttons[(int)i] = _transform.GetComponent<ButtonHelper>($"Panel/Button/btn_{i.ToString().ToLower()}");

            slots = _transform.Find("Panel/List").GetComponentsInChildren<PopupTournament_Slot>();

            popupRanking = _transform.GetComponent<PopupTournament_Ranking>("Popup/Ranking");
            popupBatch = _transform.GetComponent<PopupTournament_Batch>("Popup/Batch");
            popupUserInfo = _transform.GetComponent<PopupTournament_UserInfo>("Popup/UserInfo");
            popupRewardInfo = _transform.GetComponent<PopupTournament_RewardInfo>("Popup/RewardInfo");
        }

        public Transform panel => txtPlayCount.transform.parent;
        public Transform list => slots[0].transform.parent;

    }
    #endregion VALIDATE

}
