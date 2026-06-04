using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CastleData = Data_Castle.CastleData;

public class LobbyScreen_Castle_Popup_Setting : MonoBehaviour, IValidatable
{
    bool m_isClose = false;

    Transform m_baseIcon;
    PopupCastleHeroListComponent m_popupHeroList;
    PopupHeroInfo m_popupHeroInfo;
    PopupUseTimeStoneComponent m_popupTimeStone;

    LobbyScreen_Castle_Popup_Setting_UpgradeInfo m_upgradeInfo;

    CastleData m_castleData;

    string m_logUpgrade;
    bool m_isInfoVersion;
    bool m_isNeedUpdate;

    private void Awake()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(() => Close());

        m_baseIcon = m_element.pHeroIcon.GetChild(0);
        m_baseIcon.transform.SetParent(m_element.pHeroIcon.parent);
        m_baseIcon.gameObject.SetActive(false);

        m_element.btnChange.onClick.AddListener(() => AdjustUI(!m_isInfoVersion, m_castleData.type, true));
        m_element.btnAdd.onClick.AddListener(() => OpenHeroListPopupAsync().Forget());
        m_element.btnUpgrade.onClick.AddListener(OnButton_Upgrade);
        m_element.btnUpgradeTimer.onClick.AddListener(() => OnButton_TimeStoneAsync().Forget());

        m_upgradeInfo = m_element.scroll.content.GetComponent<LobbyScreen_Castle_Popup_Setting_UpgradeInfo>("UpgradeInfo");

        m_element.scroll.onValueChanged.AddListener(_pos =>
        {
            var scroll = m_element.scroll;
            if (_pos.y < 1)
                scroll.velocity = scroll.content.anchoredPosition = Vector2.zero;
            else if (ControllerManager.isClick == false)
            {
                if (scroll.viewport.rect.height * .05f < -scroll.content.anchoredPosition.y)
                {
                    scroll.enabled = false;
                    scroll.velocity = Vector2.zero;
                    Close(_ease: Ease.Linear);
                }
            }
        });

        Signal.instance.UpdateHeroStat.connectLambda = new(this, _ => m_isNeedUpdate = true);
        Signal.instance.UpdateFarmMarketData.connect = SlotUpdateFarmMarketData;
        Signal.instance.CompleteCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            //세팅 화면 업그레이드 결과 세팅해줘야 해
            if (m_castleData.type != _castleData.type || gameObject.activeInHierarchy == false)
                return;
            m_castleData = _castleData;
            m_upgradeInfo.SetUpgradeInfo(m_castleData);

            SetBatchHero(false);
            SetCoreStatInfo();
            m_element.txtTitle.text = $"Lv.{m_castleData.level} {DataManager.castle.GetObjectName(m_castleData.type)}: {(m_isInfoVersion ? "_개요" : "_관리")}";

            m_element.btnUpgrade.gameObject.SetActive(true);
            m_element.btnUpgradeTimer.gameObject.SetActive(false);

            if (m_popupTimeStone != null && m_popupTimeStone.statusType == StatusType.Wait)
                m_popupTimeStone.Close();
        });
        Signal.instance.StopCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (m_castleData.type != _castleData.type || gameObject.activeInHierarchy == false)
                return;
            m_castleData = _castleData;

            var upgradeData = DataManager.castle.building.GetUpgradeData(m_castleData);

            var ts = upgradeData.ts;
            if (ts.Minutes > 0)
                m_element.btnUpgradeTimer.text = $"<color=#{Palette.htmlString_Up}>{Utils.MSpace($"{ts.TotalHours:00}:{ts.ToString(@"mm\:ss")}", 30)}\n남은_시간";
            else
                m_element.btnUpgradeTimer.text = $"<color=#{Palette.htmlString_Up}>{Utils.MSpace(ts.TotalSeconds.ToString("0.00"), 30)}s\n남은_시간";
        });
        Signal.instance.StartCaslteBuildingUpgrade.connect = SlotStartCaslteBuildingUpgrade;
        Signal.instance.UpdateCaslteBuildingUpgrade.connect = SlotUpdateCaslteBuildingUpgrade;
    }

    public async UniTask OpenAsync(bool _isInfo, CastleObjectType _type, CancellationToken _cancelToken)
    {
        gameObject.SetActive(true);
        m_isClose = false;

        AdjustUI(_isInfo, _type, false);

        await UniTask.WaitUntil(() => m_isClose == true, cancellationToken: _cancelToken);
    }

    void AdjustUI(bool _isInfo, CastleObjectType _type, bool _isChange)
    {
        m_castleData = DataManager.castle.GetCaslteData(_type);
        m_isInfoVersion = _isInfo;

        m_element.btnChange.text = _isInfo ? "_관리" : "_개요";

        if (_isInfo)
        {
            m_element.btnUpgrade.gameObject.SetActive(false);
            m_element.btnUpgradeTimer.gameObject.SetActive(false);
            m_upgradeInfo.gameObject.SetActive(false);

            if (_type == CastleObjectType.Market || _type == CastleObjectType.Farm)
            {
                m_element.gauge.gameObject.SetActive(true);
                SlotUpdateFarmMarketData(m_castleData);
            }
            else
                m_element.gauge.gameObject.SetActive(false);

            m_element.btnGoShop.transform.parent.gameObject.SetActive(_type == CastleObjectType.Merchant);
            m_element.palace.gameObject.SetActive(_type == CastleObjectType.Palace);
            if (_type == CastleObjectType.Gate)
                m_upgradeInfo.SetGateInfo(m_castleData);
            //m_element.gate.gameObject.SetActive(_type == CastleObjectType.Gate);
        }
        else
        {
            m_element.gauge.gameObject.SetActive(false);
            m_upgradeInfo.SetUpgradeInfo(m_castleData);

            m_element.btnGoShop.transform.parent.gameObject.SetActive(false);
            m_element.palace.gameObject.SetActive(false);
        }
        m_element.scroll.content.ForceRebuildLayout();

        m_element.txtTitle.text = $"Lv.{m_castleData.level} {DataManager.castle.GetObjectName(_type)}: {(_isInfo ? "_개요" : "_관리")}";

        m_element.scroll.content.anchoredPosition = Vector2.zero;
        m_element.scroll.enabled = true;

        if (_isChange == false)
        {
            var rtScroll = (RectTransform)m_element.scroll.transform;
            rtScroll.anchoredPosition = Vector2.zero;
            rtScroll.DOAnchorPosY(rtScroll.rect.height, 0.1f);
        }

        m_element.pHeroIcon.parent.gameObject.SetActive(m_castleData.type != CastleObjectType.Office);
        SetBatchHero(m_isNeedUpdate);
        SetCoreStatInfo();
        m_isNeedUpdate = false;

        if (_isInfo == false)
        {
            if (m_castleData.isDoingUpgrade == true)
            {
                var upgradeData = DataManager.castle.building.GetUpgradeData(m_castleData);
                SlotUpdateCaslteBuildingUpgrade(upgradeData);

                if (m_castleData.remainUpgradeSeconds > 0)
                    m_element.btnUpgradeTimer.text = $"<color=#{Palette.htmlString_Up}>{m_element.btnUpgradeTimer.text}";
            }
            else
            {
                m_element.btnUpgrade.gameObject.SetActive(true);
                m_element.btnUpgradeTimer.gameObject.SetActive(false);
            }
        }

        // 관리야?? 그런데 업그레이드 조건 미달성이야?? 알람 ㄱㄱ
        //if (_isInfo == false && m_logUpgrade.IsActive() == true)
        //    PopupManager.instance.AlertShow(m_logUpgrade, -100);
    }

    void SlotStartCaslteBuildingUpgrade(CastleData _castleData)
    {
        if (m_castleData.type != _castleData.type || gameObject.activeInHierarchy == false)
            return;

        m_castleData = _castleData;
        m_upgradeInfo.SetUpgradeInfo(m_castleData);

        m_element.btnUpgrade.gameObject.SetActive(true);
        m_element.btnUpgradeTimer.gameObject.SetActive(false);
    }

    void SlotUpdateCaslteBuildingUpgrade(Data_Castle_Building.CastleBuildingUpgradeData _upgradeData)
    {
        if (m_castleData.type != _upgradeData.objectType || gameObject.activeInHierarchy == false)
            return;

        if (m_element.btnUpgradeTimer.gameObject.activeSelf == false)
        {
            m_element.btnUpgrade.gameObject.SetActive(false);
            m_element.btnUpgradeTimer.gameObject.SetActive(true);
        }

        var ts = _upgradeData.ts;
        if (ts.TotalMinutes > 0)
            m_element.btnUpgradeTimer.text = $"{Utils.MSpace($"{ts.Hours:00}:{ts.ToString(@"mm\:ss")}", 30)}\n남은_시간";
        else
            m_element.btnUpgradeTimer.text = $"{Utils.MSpace(ts.TotalSeconds.ToString("0.00"), 30)}s\n남은_시간";

        m_popupTimeStone?.UpdateRemainTime(ts);
    }

    void OnButton_Upgrade()
    {
        if (m_logUpgrade.IsActive() == true)
        {
            PopupManager.instance.AlertShow(m_logUpgrade, -100);
            return;
        }

        DataManager.castle.building.StartUpgradeAsync(m_castleData.type, null).Forget();
    }

    async UniTask OnButton_TimeStoneAsync()
    {
        m_popupTimeStone = await PopupManager.instance.OpenPopup<PopupUseTimeStoneComponent>(PopupType.UseTimeStone);
        m_popupTimeStone.UpdateRemainTime(DataManager.castle.building.GetUpgradeData(m_castleData).ts);

        var rtScroll = (RectTransform)m_element.scroll.transform;
        var prevPos = rtScroll.anchoredPosition.y;
        rtScroll.DOAnchorPosY(0, 0.1f);

        await UniTask.WaitUntil(() => m_popupTimeStone.statusType != StatusType.Wait);

        if (m_popupTimeStone.statusType == StatusType.Success)
            DataManager.castle.building.UpgradeTimerBonusAsync(m_castleData.type, m_popupTimeStone.timeBonus).Forget();

        await rtScroll.DOAnchorPosY(prevPos, 0.1f).AsyncWaitForCompletion();

        m_popupTimeStone = null;
    }

    void SetBatchHero(bool _isForceUpdate = false)
    {
        int i = 0;
        var pIcon = m_element.pHeroIcon;
        for (; i < m_castleData.heroes.Count; i++)
        {
            var item = (i == pIcon.childCount ?
                Instantiate(m_baseIcon, pIcon) : pIcon.GetChild(i)).GetChild(0)
                .GetComponent<HeroIconComponent>();

            var heroData = DataManager.userInfo.GetHeroInfoData(m_castleData.heroes[i]);

            item.transform.parent.gameObject.SetActive(true);
            item.SetHeroData(heroData, (_heroIcon, _isRightClick) => OnButtonAsync_BatchHero(heroData).Forget(), null, _isForceUpdate);
            item.name = heroData.skin;
        }

        for (; i < m_element.pHeroIcon.childCount; i++)
            m_element.pHeroIcon.GetChild(i).gameObject.SetActive(false);

        m_element.btnAdd.text = $"{m_castleData.heroes.Count}/{6}";

        m_element.pHeroIcon.ForceRebuildLayout(1);
    }

    void SetCoreStatInfo()
    {
        var dbCastle = TableManager.castle.GetCastleData(m_castleData.type);
        var dbCastleRise = m_castleData.dbRise;

        m_logUpgrade = "";
        // 관아일 경우
        if (m_castleData.type == CastleObjectType.Office)
        {
            var levelInfo = DataManager.castle.mission.levelInfo;
            m_element.txtBatchStat[0].text = $"경험치_:_{levelInfo.nowExp}/{levelInfo.maxExp}";

            m_logUpgrade = levelInfo.isUpgradable ? "" : "경험치가_부족합니다.";
            m_element.txtBatchStat[1].gameObject.SetActive(false);
        }
        else
        {
            for (int i = 0; i < m_element.txtBatchStat.Length; i++)
            {
                var coreStat = dbCastle.coreStat[i];
                var txt = m_element.txtBatchStat[i];

                if (coreStat == CoreStatType.NONE)
                    txt.gameObject.SetActive(false);
                else
                {
                    txt.gameObject.SetActive(true);

                    var total = DataManager.castle.GetTotalCoreStat(m_castleData, coreStat);
                    var max = m_castleData.type == CastleObjectType.Palace ? dbCastleRise.orinValue01 : dbCastleRise.maxCoreStat[i];

                    string color = "", colorBack = "";
                    if (total < max)
                    {
                        color = $"<color=#{Palette.htmlString_Down}>";
                        colorBack = "</color>";
                        m_logUpgrade = "요구_능력치가_부족합니다.";
                    }

                    txt.text = $"{TableManager.stringTable.GetString($"CORESTAT_{coreStat.ToString().ToUpper()}")} : {color}{total}/{max}{colorBack} ";
                    if (m_isInfoVersion == true)
                    {
                        string format = "<size=80%>", message = "";

                        switch (m_castleData.type)
                        {
                            case CastleObjectType.Palace:
                                format += "(고유_능력치_감소_{0})";
                                break;
                            case CastleObjectType.Market:
                            case CastleObjectType.Farm:
                                format += i == 0 ? "(획득량_{0})" : "(최대치_{0})";
                                break;
                            case CastleObjectType.Merchant:
                                format += i == 0 ? "(할인율_{0})" : "(판매_개수_{0})";
                                break;
                            case CastleObjectType.Gate:
                                format += i == 0 ? "(청렴도_{0})" : "(치안율_{0})";
                                break;
                        }

                        if (total > 0)
                        {
                            switch (m_castleData.type)
                            {
                                case CastleObjectType.Palace:
                                    message = $"<color=#{Palette.htmlString_Up}>-{(Mathf.Min(1, total / (float)max)) * 0.5f * 100:0.##}%</color>";
                                    break;
                                case CastleObjectType.Gate:
                                    message = $"<color=#{Palette.htmlString_Up}>{Mathf.Min(1, total / (float)max) * 100:0.##}%</color>";
                                    break;
                                default:
                                    message = $"<color=#{Palette.htmlString_Up}>+{(Mathf.Min(1, total / (float)max) * 0.45f) * 100:0.##}%</color>";
                                    break;
                            }
                        }
                        else
                            message = "0%";

                        txt.text += string.Format(format, message);
                    }
                }

                // 궁성일 경우 다른 모든 건물 레벨이 궁성과 같아야 한다.
                if (m_logUpgrade.IsActive() == false)
                {
                    if (m_castleData.type == CastleObjectType.Palace)
                    {
                        for (var type = CastleObjectType.NONE + 1; type < CastleObjectType.MAX; type++)
                        {
                            if (m_castleData.level != DataManager.castle.GetCaslteData(type).level)
                            {
                                m_logUpgrade = "다른_건물의_레벨이_낮습니다.";
                                break;
                            }
                        }
                    }
                    else
                    {
                        var palaceLevel = DataManager.castle.GetCaslteData(CastleObjectType.Palace).level;
                        m_logUpgrade = m_castleData.level < palaceLevel ? "" : "궁성의_레벨보다_높을_수_없습니다.";
                    }
                }
            }
        }

        m_element.btnUpgrade.text = m_logUpgrade.IsActive() == false ? "증축_시작" : "조건_미달성";

        m_element.txtBatchStat[0].transform.parent.ForceRebuildLayout();

        if (m_element.gauge.gameObject.activeSelf == true)
            m_element.txtPerSecond.text = $"시간당_획득량: {DataManager.castle.GetAmountPerSecond(m_castleData).AmountKMBT()}";
    }

    public bool CloseEscape()
    {
        if (m_popupHeroInfo != null && m_popupHeroInfo.gameObject.activeSelf == true)
            return false;

        if (m_popupHeroList?.CloseEscape() == false)
            return false;

        if (m_popupTimeStone?.CloseEscape() == false)
        {
            m_popupTimeStone = null;
            return false;
        }

        if (gameObject.activeSelf == true)
        {
            Close();
            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (m_popupHeroInfo != null)
            Destroy(m_popupHeroInfo.gameObject);
        if (m_popupHeroList != null)
            Destroy(m_popupHeroList.gameObject);
    }

    public void Close(StatusType _result = StatusType.Cancel, Ease _ease = Ease.InBack)
    {
        var rtScroll = (RectTransform)m_element.scroll.transform;
        rtScroll.DOAnchorPosY(0, 0.1f).SetEase(_ease).OnComplete(() => gameObject.SetActive(false));

        Utils.AfterSecond(() => m_isClose = true, 0.05f);
    }

    void SlotUpdateFarmMarketData(CastleData _castleData)
    {
        if (gameObject.activeInHierarchy == false || m_castleData.type != _castleData.type)
            return;

        var maxAmount = DataManager.castle.GetMaxAmount(_castleData);

        if (maxAmount == 0)
        {
            m_element.gauge.textAmount = $"_비활성화";
            m_element.gauge.fillAmount = 0;
        }
        else
        {
            //m_element.gauge.textAmount = $"{Mathf.RoundToInt(_castleData.totalAmount).AmountKMBT()}/{maxAmount.AmountKMBT()}";
            m_element.gauge.textAmount = $"{Mathf.RoundToInt(_castleData.totalAmount):#,0}/{maxAmount:#,0}";
            m_element.gauge.fillAmount = _castleData.totalAmount / (float)maxAmount;
        }

        m_castleData = _castleData;
    }

    async UniTask OpenHeroListPopupAsync()
    {
        m_element.btnAdd.interactable = false;

        var rtScroll = (RectTransform)m_element.scroll.transform;
        var prevPos = rtScroll.anchoredPosition.y;
        rtScroll.DOAnchorPosY(0, 0.1f);

        if (m_popupHeroList == null)
            m_popupHeroList = await PopupManager.instance
                .OpenPopup<PopupCastleHeroListComponent>(PopupType.Castle_HeroList,
                m_castleData);
        else
        {
            m_popupHeroList.gameObject.SetActive(true);
            m_popupHeroList.OpenPopup(m_castleData);
        }

        List<string> prevheroes = new();
        prevheroes.AddRange(m_castleData.heroes);

        await UniTask.WaitUntil(() => m_popupHeroList.statusType != StatusType.Wait, cancellationToken: destroyCancellationToken);

        m_element.btnAdd.interactable = true;
        rtScroll.DOAnchorPosY(prevPos, 0.1f);

        if (m_popupHeroList.resultType == StatusType.Success)
        {
            m_castleData.heroes = new();
            m_castleData.heroes.AddRange(m_popupHeroList.heroes);

            SetBatchHero(true);
            SetCoreStatInfo();

            //if (m_upgradeInfo.gameObject.activeSelf == true)
            //    m_upgradeInfo.SetUpgradeInfo(m_castleData);
        }
    }

    async UniTask OnButtonAsync_BatchHero(HeroInfoData _heroInfoData)
    {
        var rtScroll = (RectTransform)m_element.scroll.transform;
        var prevPos = rtScroll.anchoredPosition.y;
        rtScroll.DOAnchorPosY(0, 0.1f);

        if (m_popupHeroInfo == null)
        {
            m_popupHeroInfo = await PopupManager.instance.OpenPopup<PopupHeroInfo>(PopupType.Hero_HeroInfo, _heroInfoData);
            m_popupHeroInfo.isDontDestroy = true;
        }
        else
            await m_popupHeroInfo.SetHeroInfoDataAsync(_heroInfoData);

        await UniTask.WaitUntil(() => m_popupHeroInfo.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

        rtScroll.DOAnchorPosY(prevPos, 0.1f);
        if (m_popupHeroInfo.isNeedUpdate == true)
        {
            SetBatchHero(true);
            SetCoreStatInfo();
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public ScrollRect scroll;

        public TextMeshProUGUI txtTitle;
        public ButtonHelper btnChange;

        public Transform pHeroIcon;
        public ButtonHelper btnAdd;

        public GaugeHelper gauge;
        public TextMeshProUGUI txtPerSecond;

        public ButtonHelper btnUpgrade;
        public ButtonHelper btnUpgradeTimer;
        public TextMeshProUGUI[] txtBatchStat;

        public ButtonHelper btnGoShop;
        public LobbyScreen_Castle_Popup_Setting_Palace palace;

        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>("Panel");
            txtTitle = scroll.content.GetComponent<TextMeshProUGUI>("txt_title");
            btnChange = scroll.content.GetComponent<ButtonHelper>("btn_change");

            pHeroIcon = scroll.content.Find("Batch/Icons/List");
            btnAdd = pHeroIcon.parent.GetComponent<ButtonHelper>("btn_add");

            gauge = scroll.content.GetComponent<GaugeHelper>("Gauge");
            txtPerSecond = gauge.transform.GetComponent<TextMeshProUGUI>("txt_per");

            var batchStat = scroll.content.Find("Batch/Stat");
            txtBatchStat = batchStat.GetComponentsInChildren<TextMeshProUGUI>();
            btnUpgrade = scroll.content.GetComponent<ButtonHelper>("Batch/btn_upgrade");
            btnUpgradeTimer = scroll.content.GetComponent<ButtonHelper>("Batch/btn_upgrade_timer");

            btnGoShop = scroll.content.GetComponent<ButtonHelper>("Merchant/btn_shop");
            palace = scroll.content.GetComponent<LobbyScreen_Castle_Popup_Setting_Palace>("Palace");
        }
    }
    #endregion VALIDATE
}
