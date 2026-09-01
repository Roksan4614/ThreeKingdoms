using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo : BasePopupComponent
{
    PopupHeroInfo() : base(PopupType.Hero_HeroInfo) { }

    enum TabType
    {
        NONE = -1,
        stat,
        attribute,
        MAX
    }

    HeroInfoData m_heroInfoData;
    CharacterComponent m_character;

    public HeroInfoData heroInfoData => m_heroInfoData;

    public bool isDontDestroy { get; set; } = false;

    public StatusType statusType { get; private set; } = StatusType.Wait;
    public bool isNeedUpdate { get; private set; }

    void Start()
    {
        //m_element.btnCharacter.onClick.AddListener(() => m_character.anim.Play(CharacterAnimType.Attack));

        m_element.btnPosition.onClick.AddListener(
            () =>
            {
                if (m_heroInfoData.isMine == true)
                    OpenPopupAsync_Position().Forget();
            });

        for (int i = 0; i < m_element.popup.childCount; i++)
            m_element.popup.GetChild(i).gameObject.SetActive(false);

        Utils.WaitEscape(this, Close, _token: destroyCancellationToken);

        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
        {
            var tab = i;
            m_element.btnTap[(int)i].onClick.AddListener(() => SetActiveTab(tab));
        }

        m_element.btnConfirm.onClick.AddListener(Close);

        m_element.btnBatch.onClick.AddListener(() =>
        {
            DataManager.storyMode.isLockUI = false;
            PopupManager.instance.CloseAll();
            LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Hero);
            //Close();
        });

        m_element.btnEnchant.onClick.AddListener(() => OnButtonAsync_Upgrade(false).Forget());
        m_element.btnUpgrade.onClick.AddListener(() => OnButtonAsync_Upgrade(true).Forget());

        // POPUP UPGRADE
        {
            m_element.popupUpgrade.btnUpgradeLeft.onClick.AddListener(() => OnButton_UpgradeArrow(true));
            m_element.popupUpgrade.btnUpgradeRight.onClick.AddListener(() => OnButton_UpgradeArrow(false));
        }

        // Traits Reroll
        m_element.statAttribute.onClickReroll.AddListener(() => OnButtonAsync_TraitsReroll().Forget());
    }

    private void OnEnable()
    {
        statusType = StatusType.Wait;
        isNeedUpdate = false;
        Utils.SetActivePunch(m_element.panel, true, false);
        SetActiveTab(TabType.stat);
    }

    // HeroInfoData
    public override void OpenPopup(params object[] _args)
    {
        if (_args.Length > 0)
            SetHeroInfoDataAsync((HeroInfoData)_args[0]).Forget();
    }

    void SetActiveTab(TabType _tabType)
    {
        for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
            m_element.btnTap[(int)i].SetDrawSelect(i == _tabType);

        m_element.statBattle.SetActive(_tabType == TabType.stat);
        m_element.statAttribute.SetActive(_tabType == TabType.attribute, m_heroInfoData);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="_isJustWatch">오로지 확인용. 승급하기 강화하기 같은거 없음</param>
    /// <returns></returns>
    public async UniTask SetHeroInfoDataAsync(HeroInfoData _data, bool _isJustWatch = false, bool _isBatch = false)
    {
        // 하단 버튼 세팅
        {
            if (_data.isMine == false && _isJustWatch == false)
                _isJustWatch = true;

            m_element.btnEnchant.gameObject.SetActive(_isJustWatch == false);
            m_element.btnUpgrade.gameObject.SetActive(_isJustWatch == false);
            m_element.btnConfirm.gameObject.SetActive(_isJustWatch == true);
            m_element.btnBatch.gameObject.SetActive(_isBatch == true);

            m_element.btnEnchant.transform.parent.ForceRebuildLayout();

            m_element.txtTimer_AutoClose.text = "";
        }

        gameObject.SetActive(true);

        if (m_heroInfoData.IsActive() == true && _data.key == m_heroInfoData.key)
            return;

        m_heroInfoData = _data.DeepClone();

        // FRONT PANEL
        var key = $"{_data.regionType}_{_data.key}".ToUpper();
        m_element.txtName.text = $"{TableManager.stringHero.GetString("NAME_" + key)}<size=80%><color=#888888> {TableManager.stringHero.GetString("COURTESY_" + key)}";
        m_element.txtDescTalk.text = _data.talk;
        m_element.txtEnchantLevel.text = _data.enchantLevel == 0 ? "" : $"(+{_data.enchantLevel})";
        SetHeroInfoText($"등급_:_{_data.gradeClass}");
        SetPositionType();

        // 고유 능력치
        SetHeroInfo_CoreStat(m_heroInfoData);

        // 전투 능력치
        m_element.statBattle.SetStatData(m_heroInfoData);

        // 파워
        m_element.txtPower.text = $"cp {m_heroInfoData.power.AmountKMBT(_isMBT: true)}";

        // 내꺼 아니면 특성 감출까?
        m_element.btnTap[1].gameObject.SetActive(m_heroInfoData.isMine);

        // CHARACTER 
        {
            var parent = m_element.panelHero;
            bool isFinded = false;
            for (int i = 0; i < parent.childCount; i++)
            {
                var obj = parent.GetChild(i).gameObject;
                obj.SetActive(obj.name.Contains(_data.skin));

                if (isFinded == false && obj.activeSelf == true)
                    isFinded = true;
            }

            if (isFinded == false)
            {
                var heroCharacter = (await AddressableManager.instance.GetHeroCharacterAsync(_data.skin))?.GetComponent<CharacterComponent>();

                if (heroCharacter != null)
                {
                    m_character = Instantiate(heroCharacter, parent);
                    m_character.name = _data.skin;
                    m_character.transform.localPosition = Vector3.zero;

                    m_character.DeleteElement();
                }
            }
        }
    }

    void SetHeroInfo_CoreStat(HeroInfoData _heroInfoData, bool _isCompare = false)
    {
        var coreStat = _heroInfoData.resultCoreStat;
        for (int i = 0; i < m_element.stat.Count; i++)
        {
            var value = coreStat[(CoreStatType)i];
            var txt = m_element.stat[i].content;

            txt.text = value.ToString();
            if (_isCompare)
                txt.text = "<color=#BA0700>" + txt.text;

            m_element.stat[i].title.alpha = txt.alpha = value >= 90 ? 1 : value >= 80 ? .9f : value >= 70 ? .8f : value >= 60 ? .7f : .6f;
        }
    }

    async UniTask OnButtonAsync_Upgrade(bool _isUpgrade)
    {
        var heroInfoData = m_heroInfoData.DeepClone();

        if (heroInfoData.key == CharacterName.SunJian.ToString())
        {
            PopupManager.instance.AlertShow("시스템에_의해_차단되었습니다.");
            return;
        }

        if (_isUpgrade)
        {
            heroInfoData.grade++;

            if (heroInfoData.grade >= GradeType.MAX)
            {
                PopupManager.instance.AlertShow("이미_최대_등급입니다.");
                return;
            }

            SetHeroInfoText($"등급_:_<color=#BA0700>{heroInfoData.gradeClass}");
        }
        else
        {
            heroInfoData.enchantLevel++;

            if (heroInfoData.enchantLevel > 20)
            {
                PopupManager.instance.AlertShow("이미_최대_레벨입니다.");
                return;
            }

            m_element.txtEnchantLevel.text = $"<color=#BA0700>(+{heroInfoData.enchantLevel})";
        }

        //고유 능력치
        SetHeroInfo_CoreStat(heroInfoData, true);
        //전투 능력치 비교
        m_element.statBattle.SetCompareData(heroInfoData);

        await m_element.popupUpgrade.OpenAsync(heroInfoData, _isUpgrade);

        if (m_element.popupUpgrade.isNeedUpdate)
        {
            TournamentWorker.instance.UpdateHero();
            m_heroInfoData = m_element.popupUpgrade.heroInfoData;
            isNeedUpdate = true;

            m_element.txtPower.text = $"cp {m_heroInfoData.power.AmountKMBT(_isMBT: true)}";
            if (m_element.statAttribute.isActive)
                m_element.statAttribute.SetActive(true, m_heroInfoData);
        }

        m_element.statBattle.SetStatData(m_heroInfoData);
        SetHeroInfo_CoreStat(m_heroInfoData);

        if (_isUpgrade)
            SetHeroInfoText($"등급_:_{m_heroInfoData.gradeClass}");
        else
            m_element.txtEnchantLevel.text = $"(+{m_heroInfoData.enchantLevel})";
    }

    void OnButton_UpgradeArrow(bool _isPrev)
    {
        m_element.popupUpgrade.OnButton_UpgradeArrow(_isPrev);

        var heroInfoData = m_element.popupUpgrade.heroInfoData;

        SetHeroInfo_CoreStat(heroInfoData, true);
        m_element.statBattle.SetCompareData(heroInfoData);

        SetHeroInfoText($"등급_:_<color=#BA0700>{heroInfoData.gradeClass}");
    }

    void SetHeroInfoText(string _gradeInfo)
    {
        m_element.txtInfo.text = _gradeInfo;
        m_element.txtInfo.text += $"\n소속_:_{TableManager.stringTable.GetRegionType(m_heroInfoData.regionType, true)}";
    }

    async UniTask OnButtonAsync_TraitsReroll()
    {
        if (m_heroInfoData.countOpenTraits == 0)
        {
            PopupManager.instance.AlertShow("명장부터_특성_부여가_가능합니다.");
            return;
        }
        else if (m_heroInfoData.traits != null && m_heroInfoData.traits.Count(x => x.isLock == false) == 0 && m_heroInfoData.countOpenTraits == m_heroInfoData.traits.Count)
        {
            PopupManager.instance.AlertShow("모두_잠겨서_진행이_불가합니다.");
            return;
        }

        m_element.statAttribute.interactable = false;

        m_heroInfoData = await DataManager.userInfo.API_TraitsChange(m_heroInfoData.key);

        // 고유 능력치
        SetHeroInfo_CoreStat(m_heroInfoData);

        // 전투 능력치
        m_element.statBattle.SetStatData(m_heroInfoData);

        // 파워
        m_element.txtPower.text = $"cp {m_heroInfoData.power.AmountKMBT(_isMBT: true)}";
        isNeedUpdate = true;

        m_element.statAttribute.SetActive(true, m_heroInfoData);
        m_element.statAttribute.interactable = true;
    }

    async UniTask OpenPopupAsync_Position()
    {
        if( await m_element.popupPosition.OpenPopupAsync(m_heroInfoData.key))
            SetPositionType();
    }

    void SetPositionType()
    {
        var pd = DataManager.heroPosition.GetHeroPosition(m_heroInfoData.key);
        if (pd == null)
            m_element.btnPosition.text = "없음";
        else
            m_element.btnPosition.text = pd.positionData.name;
    }

    public async UniTask AutoCloseAsync(float _duration)
    {
        string key = "{0}초후_닫힘._터치하면_취소됩니다.";

        DateTime dtClose = DateTime.Now.AddSeconds(_duration);
        while (dtClose > DateTime.Now)
        {
            var timer = (dtClose - DateTime.Now).TotalSeconds;
            m_element.txtTimer_AutoClose.text = string.Format(key, Utils.MSpace($"{timer:0.0}"));

            if (ControllerManager.isClick)
            {
                m_element.txtTimer_AutoClose.text = "";
                return;
            }

            await UniTask.WaitForEndOfFrame();
        }

        Close();
    }

    public override void Close()
    {
        if (m_element.popupPosition.gameObject.activeSelf)
        {
            m_element.popupPosition.Close();
            return;
        }
        if (m_element.popupUpgrade.gameObject.activeSelf)
        {
            m_element.popupUpgrade.Close();
            return;
        }

        CloseAsync().Forget();
    }

    async UniTask CloseAsync()
    {
        statusType = StatusType.Cancel;

        await Utils.SetActivePunchAsync(m_element.panel, false, false);

        if (isDontDestroy == true)
            gameObject.SetActive(false);
        else if (gameObject != null)
            Destroy(gameObject);
    }

    public override void OnManualValidate()
        => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;
    [Serializable]
    struct ElementData
    {
        public Transform panel;
        public Transform panelHero;
        //public Button btnCharacter;
        public ButtonHelper btnPosition;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtInfo;
        public TextMeshProUGUI txtEnchantLevel;
        public TextMeshProUGUI txtDescTalk;

        public PopupHeroInfo_Popup_Position popupPosition;
        public PopupHeroInfo_Popup_Upgrade popupUpgrade;

        public PopupHeroInfo_Stat_Battle statBattle;
        public PopupHeroInfo_Stat_Attribute statAttribute;

        public Transform popup;

        public ButtonHelper btnEnchant;
        public ButtonHelper btnUpgrade;
        public ButtonHelper btnConfirm;
        public ButtonHelper btnBatch;
        public TextMeshProUGUI txtTimer_AutoClose;

        public ButtonHelper[] btnTap;
        public TextMeshProUGUI txtPower;

        public List<EntryData> stat;
        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            panelHero = panel.Find("btn_character").GetChild(0);
            //btnCharacter = panel.GetComponent<Button>("btn_character");

            var frontPanel = panel.Find("FrontPanel");
            btnPosition = frontPanel.GetComponent<ButtonHelper>("btn_position");
            txtName = frontPanel.GetComponent<TextMeshProUGUI>("txt_name");
            txtInfo = frontPanel.GetComponent<TextMeshProUGUI>("txt_info");
            txtEnchantLevel = frontPanel.GetComponent<TextMeshProUGUI>("txt_level");
            txtDescTalk = frontPanel.GetComponent<TextMeshProUGUI>("txt_desc");

            {
                popup = _transform.Find("Popup");
                popupPosition = popup.GetComponent<PopupHeroInfo_Popup_Position>("Position");
                popupUpgrade = popup.GetComponent<PopupHeroInfo_Popup_Upgrade>("Upgrade");
            }

            var pStat = panel.Find("Stat");
            stat = new();
            for (int i = 0; i < pStat.childCount; i++)
            {
                var item = pStat.GetChild(i);
                stat.Add(new EntryData()
                {
                    title = item.GetComponent<TextMeshProUGUI>("txt_title"),
                    content = item.GetComponent<TextMeshProUGUI>("txt_content"),
                });
            }
            txtPower = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_power");

            // BUTTON
            btnEnchant = panel.GetComponent<ButtonHelper>("Buttons/btn_enchant");
            btnUpgrade = panel.GetComponent<ButtonHelper>("Buttons/btn_upgrade");
            btnConfirm = panel.GetComponent<ButtonHelper>("Buttons/btn_confirm");
            btnBatch = panel.GetComponent<ButtonHelper>("Buttons/btn_batch");

            txtTimer_AutoClose = btnConfirm.transform.GetComponent<TextMeshProUGUI>("txt_timer");

            // TAB
            btnTap = panel.Find("Tab").GetComponentsInChildren<ButtonHelper>();

            statBattle = panel.GetComponent<PopupHeroInfo_Stat_Battle>("Stat_Battle");
            statAttribute = panel.GetComponent<PopupHeroInfo_Stat_Attribute>("Stat_Attribute");
        }

        //public Transform panelHero => btnCharacter.transform.GetChild(0);
    }

    [Serializable]
    public struct EntryData
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI content;
    }
}
