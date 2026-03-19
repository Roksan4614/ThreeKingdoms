using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

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

    protected override void Awake()
    {
        base.Awake();

        package.SetActive(true);
        m_element.pResult.gameObject.SetActive(false);
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

        // setlocalization
        {
            m_element.btnStart.text = "_시작하기";
            m_element.btnSkip.text = "_건너띄기";
        }
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
            if (dtEnd < DateTime.UtcNow)
            {
                await LoadHostDataAsync();

                dtEnd = m_hostData.dtEnd;
                m_element.pHost.gameObject.SetActive(true);
            }

            if (package.gameObject.activeSelf == true)
                package.SetHostRemainTime(dtEnd - DateTime.UtcNow);

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
        m_hostData.dt_end = DateTime.UtcNow.AddHours(1f).ToString();

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

            m_hostData.comp = Instantiate(result, m_element.pHost).GetComponent<CharacterComponent>();
            m_hostData.comp.move.SetFlip(true);
            m_hostData.comp.name = m_hostData.key;
            m_hostData.comp.transform.localPosition = Vector3.zero;
        }

        m_element.txtHostInfo.text = $"_주최자:{TableManager.hero.Get(m_hostData.key).name}\n<color=#636363><size=80%>영혼석 10개 획득 100%";

        return m_hostData;
    }

    public void SetEnableRegion(params RegionType[] _region)
        => package.SetEnableRegion(_region);

    public async UniTask StartAsync()
    {
        LobbyScreenManager.instance.isLock = true;

        m_hostData.comp.anim.Play(CharacterAnimType.Attack);
        m_element.btnStart.text = "진행_중";

        await Utils.SetActivePunchAsync(package.transform, false);
        package.gameObject.SetActive(false);

        await m_element.result.StartAsync(package.curRegion, m_hostData.key, m_isSkipAction);

        m_element.btnStart.text = "시작_하기";

        package.gameObject.SetActive(true);
        Utils.SetActivePunch(package.transform, true);

        LobbyScreenManager.instance.isLock = false;
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
        }
    }
    #endregion VALIDATA
}
