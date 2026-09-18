using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LobbyScreen_Summon : LobbyScreen_Base
{
    LobbyScreen_Summon_Package package => m_element.package;

    struct PartyHostData
    {
        public CharacterComponent comp;
        public string key;
        public string dt_end;

        public DateTime dtEnd => dt_end.IsActive() ? Utils.DateTimeParse(dt_end) : default;
    }

    PartyHostData m_hostData;

    bool m_isSkipAction;
    public bool isOpenResult => m_element.result.gameObject.activeSelf == true;

    protected override void Awake()
    {
        base.Awake();

        package.SetActive(true);
        m_element.hostDash.SetActive(false);
    }

    private void Start()
    {
        m_element.btnStart.onClick.AddListener(() =>
        {
            if (package.gameObject.activeSelf == true)
                StartAsync().Forget();
        });

        m_element.btnSkip.onClick.AddListener(() => OnButtonAsync_Skip().Forget());

        m_isSkipAction = PPWorker.Get<int>(PlayerPrefsType.SUMMON_SKIP_ACTION) == 1;
        m_element.btnSkip.isCheck = m_isSkipAction;

        m_element.pResult.gameObject.SetActive(false);

        // setlocalization
        {
            transform.SetText("Panel/Top/txt_title", TableManager.stringTable.GetString("SUMMON_TITLE"));
            m_element.btnStart.text = TableManager.stringTable.GetString("UI_START");
            m_element.btnSkip.text = TableManager.stringTable.GetString("UI_SKIP");
        }

        UpdateTicketCount();
    }

    protected override bool IsEscapeloseScreen()
    {
        return m_element.result.gameObject.activeSelf == false;
    }

    public override void Open(LobbyScreenType _prevScreen)
    {
        base.Open(_prevScreen);
        SetHostHeroAsync().Forget();
    }

    async UniTask SetHostHeroAsync()
    {
        DateTime dtEnd = m_hostData.dtEnd;

        while (true)
        {
            if (dtEnd < Utils.GetUTC())
            {
                await LoadHostDataAsync();

                dtEnd = m_hostData.dtEnd;
                m_element.pHost.gameObject.SetActive(true);
            }

            if (package.gameObject.activeSelf == true)
                package.SetHostRemainTime(dtEnd - Utils.GetUTC());

            await UniTask.WaitForEndOfFrame(m_cts.Token);
        }
    }

    async UniTask<PartyHostData> LoadHostDataAsync()
    {
        m_element.pHost.gameObject.SetActive(false);
        // TODO: Request
        {
            var key = DataManager.userInfo.myHero[UnityEngine.Random.Range(0, DataManager.userInfo.myHero.Count)].key;
            while (m_hostData.key == key && DataManager.userInfo.myHero.Count > 1)
                key = DataManager.userInfo.myHero[UnityEngine.Random.Range(0, DataManager.userInfo.myHero.Count)].key;

            m_hostData.key = key;
        }

        m_element.pHost.gameObject.SetActive(true);
        m_hostData.dt_end = Utils.GetUTC().AddHours(1f).ToString();

        bool isHas = false;
        for (int i = 0; i < m_element.pHost.childCount; i++)
        {
            var child = m_element.pHost.GetChild(i);
            child.gameObject.SetActive(child.gameObject.name.Equals(m_hostData.key));
            if (isHas == false && child.gameObject.activeSelf == true)
            {
                isHas = true;
                m_hostData.comp = child.GetComponent<CharacterComponent>();
            }
        }

        if (isHas == false)
        {
            var result = await AddressableManager.instance.GetHeroCharacterAsync(m_hostData.key);

            if (result != null)
            {
                m_hostData.comp = Instantiate(result, m_element.pHost).GetComponent<CharacterComponent>();
                if (m_hostData.comp != null)
                {
                    m_hostData.comp.move.SetFlip(true);
                    m_hostData.comp.name = m_hostData.key;
                    m_hostData.comp.transform.localPosition = Vector3.zero;
                }
            }
        }

        m_hostData.comp.element.collider.enabled = false;
        m_element.txtHostInfo.text = $"{TableManager.stringTable.GetString("SUMMON_HOST")} : {TableManager.hero.Get(m_hostData.key).name}\n<color=#636363><size=80%>{TableManager.stringTable.GetString("SUMMON_DESC_REWARD_INFO")}";

        return m_hostData;
    }

    public void SetEnableRegion(params RegionType[] _region)
        => package.SetEnableRegion(_region);
    public void SetRegionType(RegionType _region)
        => package.SetRegionType(_region);

    public async UniTask StartAsync()
    {
        UpdateTicketCount();
        LobbyScreenManager.instance.isLock = true;

        StartAsync_HostAction().Forget();
        m_element.btnStart.text = TableManager.stringTable.GetString("UI_RUNNING");

        await Utils.SetActivePunchAsync(package.transform, false);
        package.gameObject.SetActive(false);

        await m_element.result.StartAsync(package.curRegion, m_hostData.key, m_isSkipAction);

        m_element.btnStart.text = TableManager.stringTable.GetString("UI_START");

        package.gameObject.SetActive(true);
        Utils.SetActivePunch(package.transform, true);

        LobbyScreenManager.instance.isLock = false;
    }

    public async UniTask StartAsync_HostAction()
    {
        var hero = m_hostData.comp;

        if (m_isSkipAction == false)
        {
            hero.anim.Play(CharacterAnimType.Dash);

            hero.move.SetFlip(false);
            m_element.hostDash.SetActive(true);

            var prevPos = hero.transform.localPosition;
            hero.transform.DOLocalMoveX(prevPos.x - 4, .2f).SetEase(Ease.InBack)
                .OnComplete(() => hero.gameObject.SetActive(false)).Forget();

            await UniTask.WaitUntil(() => m_element.result.step == LobbyScreen_Summon_Result.ResultStepType.ReceiveEnd);

            hero.anim.Play(CharacterAnimType.Dash);

            hero.move.SetFlip(true);
            hero.gameObject.SetActive(true);
            hero.transform.DOLocalMoveX(prevPos.x, .1f).Forget();//.SetEase(Ease.InCubic).Forget();
        }

        //hero.anim.PlayAttack();
    }

    public async UniTask OnButtonAsync_Skip()
    {
        if (m_isSkipAction == false && m_element.result.gameObject.activeSelf == true)
        {
            var status = await PopupManager.instance.OpenModalAsync();
            await UniTask.WaitForEndOfFrame(cancellationToken: destroyCancellationToken);

            if (status != StatusType.Success)
                return;

            m_element.result.AllSkip();
        }

        m_isSkipAction = !m_isSkipAction;
        PPWorker.Set(PlayerPrefsType.SUMMON_SKIP_ACTION, m_isSkipAction ? 1 : 0);
        m_element.btnSkip.isCheck = m_isSkipAction;
    }

    void UpdateTicketCount()
        => m_element.txtTicketCount.text = TableManager.stringTable.GetStringFormat("SUMMON_TICKET_COUNT", InventoryWorker.instance.GetItemCount("normal_gacha_ticket").ToString());


    #region VALIDATE
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_element.Initialize(transform);
    }

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        [SerializeField] LobbyScreen_Summon_Result m_result;
        [SerializeField] LobbyScreen_Summon_Package m_package;
        public LobbyScreen_Summon_Package package => m_package;
        public LobbyScreen_Summon_Result result => m_result;

        public Transform pBackHero;
        public Transform pHost;
        public Transform pResult;

        public ButtonHelper btnStart;
        public ButtonHelper btnSkip;
        public TextMeshProUGUI txtHostInfo;
        public TextMeshProUGUI txtTicketCount;

        public GameObject hostDash;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            var room = panel.Find("Room");

            pBackHero = panel.Find("Back_Hero");
            pHost = panel.Find("Host");

            m_package = room.GetComponent<LobbyScreen_Summon_Package>("Content/Package");
            m_result = room.GetComponent<LobbyScreen_Summon_Result>("Content/Result");

            btnStart = panel.GetComponent<ButtonHelper>("btn_start");
            btnSkip = panel.GetComponent<ButtonHelper>("btn_skip");

            txtHostInfo = panel.GetComponent<TextMeshProUGUI>("txt_hostInfo");
            txtTicketCount = panel.GetComponent<TextMeshProUGUI>("Ticket/txt_amount");

            hostDash = room.Find("Dash").gameObject;
        }
    }
    #endregion VALIDATA
}
