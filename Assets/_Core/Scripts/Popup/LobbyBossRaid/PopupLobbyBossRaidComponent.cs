using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyBossRaidComponent : BasePopupComponent
{
    PopupLobbyBossRaidComponent() : base(PopupType.LobbyBossRaid) { }

    protected override void Awake()
    {
        base.Awake();

        m_element.panel.gameObject.SetActive(true);
        m_element.img.enabled = true;

        var popup = transform.Find("Popup");
        for (int i = 0; i < popup.childCount; i++)
            popup.GetChild(i).gameObject.SetActive(false);

        m_element.btnRanking.onClick.AddListener(() => OnButtonAsync_Ranking().Forget());
    }

    public override void OpenPopup(params object[] _args)
    {
        Utils.SetActivePunch(m_element.panel, true);
    }

    async UniTask OnButtonAsync_Ranking()
    {
        await Utils.SetActivePunchAsync(m_element.panel, false);

        m_element.popupRanking.OpenPopup();
        await UniTask.WaitUntil(() => m_element.popupRanking.gameObject.activeSelf == false);

        Utils.SetActivePunch(m_element.panel, true);
    }

    public override void Close()
        => CloseAsync().Forget();

    async UniTask CloseAsync()
    {
        await Utils.SetActivePunchAsync(m_element.panel, false);
        base.Close();
    }

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
        public TextMeshProUGUI txtRoundRemainTimer; // 라운드 남은 시간
        public TextMeshProUGUI txtInfoPrevRound;
        public TextMeshProUGUI txtRemainTimer;      // 보스 등장 남은 시간

        public ButtonHelper btnRanking;
        public ButtonHelper btnStart;
        public ButtonHelper btnShop;

        public Button btnHero;
        public Transform trnsHero;

        public PopupLobbyBossRaid_PopupRanking popupRanking;

        public void Initialize(Transform _transform)
        {
            img = _transform.GetComponent<Image>();
            panel = _transform.Find("Panel");

            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_point");
            txtDifficult = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_difficult");
            txtBossName = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_title");
            txtRoundRemainTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_remain");
            txtInfoPrevRound = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_prev_round_info");
            txtRemainTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/Button/btn_start/txt_timer");

            btnStart = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_start");
            btnRanking = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_ranking");
            btnShop = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_shop");

            btnHero = _transform.GetComponent<Button>("Panel/btn_character");
            trnsHero = btnHero.transform.Find("Panel");

            popupRanking = _transform.GetComponent<PopupLobbyBossRaid_PopupRanking>("Popup/Ranking");
        }
    }
    #endregion VALIDATE

}
