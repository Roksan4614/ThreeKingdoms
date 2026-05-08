using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Data_Castle;

public class LobbyScreen_Castle_Popup_Setting : MonoBehaviour, IValidatable
{
    bool m_isClose = false;

    Transform m_baseIcon;
    PopupCastleHeroListComponent m_poupHeroList;

    LobbyScreen_Castle_Popup_Setting_UpgradeInfo m_upgradeInfo;

    CastleData m_castleData;
    bool m_isInfoVersion;

    private void Awake()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(() => Close());

        m_baseIcon = m_element.pHeroIcon.GetChild(0);
        m_baseIcon.transform.SetParent(m_element.pHeroIcon.parent);
        m_baseIcon.gameObject.SetActive(false);

        m_element.btnAdd.onClick.AddListener(() => OpenHeroListPopupAsync().Forget());

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

        Signal.instance.UpdateCastleData.connect = SlotUpdateCastleData;
    }

    public async UniTask OpenAsync(bool _isInfo, CastleObjectType _type, CancellationToken _cancelToken)
    {
        var rtScroll = (RectTransform)m_element.scroll.transform;
        gameObject.SetActive(true);
        m_isClose = false;

        m_castleData = DataManager.castle.GetCaslteData(_type);
        m_isInfoVersion = _isInfo;

        if (_isInfo)
        {
            m_element.btnUpgrade.gameObject.SetActive(false);
            m_upgradeInfo.gameObject.SetActive(false);

            if (_type == CastleObjectType.Market || _type == CastleObjectType.Farm)
            {
                m_element.amountObject.SetActive(true);
                SlotUpdateCastleData(m_castleData);
            }
            else
                m_element.amountObject.SetActive(false);
        }
        else
        {
            m_element.amountObject.SetActive(false);

            m_element.btnUpgrade.gameObject.SetActive(true);
            m_upgradeInfo.SetUpgradeInfo(m_castleData);
        }
        m_element.scroll.content.ForceRebuildLayout();

        Dictionary<CastleObjectType, string> dbTitle = new()
        {
            { CastleObjectType.Palace, "궁성"},
            { CastleObjectType.Market, "시장"},
            { CastleObjectType.Farm, "농지"},
            { CastleObjectType.Office, "관아"},
            { CastleObjectType.Merchant, "행상"},
            { CastleObjectType.Gate, "성문"},
        };

        m_element.txtTitle.text = $"Lv.{m_castleData.level} {dbTitle[_type]}:{(_isInfo ? "_개요" : "_관리")}";

        m_element.scroll.content.anchoredPosition = Vector2.zero;
        m_element.scroll.enabled = true;

        rtScroll.anchoredPosition = Vector2.zero;
        rtScroll.DOAnchorPosY(rtScroll.rect.height, 0.1f);

        SetBatchHero();
        SetCoreStatInfo();

        var maxAmount = DataManager.castle.GetMaxAmount(m_castleData);

        await UniTask.WaitUntil(() => m_isClose == true, cancellationToken: _cancelToken);
    }

    void SetBatchHero()
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
            item.SetHeroData(heroData, null, null);
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
        var dbCastleRise = TableManager.castleRise.GetRiseData(m_castleData.type, m_castleData.level);

        bool isAvail = true;
        bool isOffice = m_castleData.type == CastleObjectType.Office;
        m_element.pHeroIcon.parent.gameObject.SetActive(isOffice == false);

        if (isOffice)
        {
            m_element.txtBatchStat[0].text = "경험치_:_0/1,000";
            m_element.txtBatchStat[1].gameObject.SetActive(false);
            isAvail = false;
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

                    Dictionary<CoreStatType, string> dbString = new()
                {
                    { CoreStatType.Leadership, "통솔" },
                    { CoreStatType.Strength, "무력" },
                    { CoreStatType.Intellect, "지력" },
                    { CoreStatType.Politics, "정치" },
                    { CoreStatType.Charisma, "매력" },
                };

                    var total = DataManager.castle.GetTotalCoreStat(m_castleData, coreStat);
                    var max = dbCastleRise.maxCoreStat[i];

                    string color = "", colorBack = "";
                    if (total < max)
                    {
                        color = $"<color=#{Palette.htmlString_Down}>";
                        colorBack = "</color>";
                        isAvail = false;
                    }

                    txt.text = $"{dbString[coreStat]}: {color}{total}/{max}{colorBack} ";
                    if (m_isInfoVersion == true)
                    {
                        string format = "<size=80%>", message = "";

                        switch (m_castleData.type)
                        {
                            case CastleObjectType.Palace:
                                format += "(건설_속도_{0}증가)";
                                break;
                            case CastleObjectType.Market:
                            case CastleObjectType.Farm:
                                format += i == 0 ? "(획득량_{0}증가)" : "(최대치_{0}증가";
                                break;
                            case CastleObjectType.Merchant:
                                format += i == 0 ? "(할인율_{0}증가)" : "(판매_개수_{0}증가";
                                break;
                            case CastleObjectType.Gate:
                                format += i == 0 ? "(깨끗율_{0}증가)" : "(치안율_{0}증가";
                                break;
                        }

                        if (total > 0)
                            message = $"<color=#{Palette.htmlString_Up}>+{(Mathf.Min(1, total / (float)max) * 0.45f) * 100:0.##}%</color>_";

                        txt.text += string.Format(format, message);
                    }
                }
            }
        }

        m_element.btnUpgrade.interactable = isAvail;
        m_element.btnUpgrade.text = isAvail ? "업그레이드" : "조건미달성";

        m_element.txtBatchStat[0].transform.parent.ForceRebuildLayout();

        m_element.txtPerSecond.text = $"시간당_획득량: {DataManager.castle.GetAmountPerSecond(m_castleData).AmountKMBT()}";
    }

    public bool CloseEscape()
    {
        if (m_poupHeroList != null)
        {
            m_poupHeroList.Close();
            return false;
        }
        if (gameObject.activeSelf == true)
        {
            Close();
            return false;
        }
        return true;
    }

    public void Close(StatusType _result = StatusType.Cancel, Ease _ease = Ease.InBack)
    {
        var rtScroll = (RectTransform)m_element.scroll.transform;
        rtScroll.DOAnchorPosY(0, 0.1f).SetEase(_ease).OnComplete(() => gameObject.SetActive(false));

        Utils.AfterSecond(() => m_isClose = true, 0.05f);
    }

    void SlotUpdateCastleData(CastleData _castleData)
    {
        if (gameObject.activeInHierarchy == false || m_castleData.type != _castleData.type)
            return;

        var maxAmount = DataManager.castle.GetMaxAmount(m_castleData);

        if (maxAmount == 0)
        {
            m_element.textAmount = $"_비활성화";
            m_element.imgBar_CalimAmount.fillAmount = 0;
        }
        else
        {
            m_element.textAmount = $"{Mathf.RoundToInt(_castleData.totalAmount).AmountKMBT()}/{maxAmount.AmountKMBT()}";
            m_element.imgBar_CalimAmount.fillAmount = _castleData.totalAmount / (float)maxAmount;
        }

        m_castleData = _castleData;
    }

    async UniTask OpenHeroListPopupAsync()
    {
        var rtScroll = (RectTransform)m_element.scroll.transform;
        var prevPos = rtScroll.anchoredPosition.y;
        rtScroll.DOAnchorPosY(0, 0.1f);

        m_poupHeroList = await PopupManager.instance
            .OpenPopup<PopupCastleHeroListComponent>(PopupType.Castle_HeroList,
            m_castleData);

        List<string> prevheroes = new();
        prevheroes.AddRange(m_castleData.heroes);

        await UniTask.WaitUntil(() => m_poupHeroList == null);

        rtScroll.DOAnchorPosY(prevPos, 0.1f);

        var heroes = m_poupHeroList.heroes;
        m_poupHeroList = null;

        if (heroes.Count == prevheroes.Count)
        {
            int i = 0;
            for (; i < heroes.Count; i++)
            {
                if (prevheroes.Contains(heroes[i]) == false)
                    break;
            }

            if (i == heroes.Count)
                return;
        }

        //m_castleData.heroes = heroes;
        SetBatchHero();
        SetCoreStatInfo();

        if (m_upgradeInfo.gameObject.activeSelf == true)
            m_upgradeInfo.SetUpgradeInfo(m_castleData);

        DataManager.castle.UpdateCastleData(m_castleData);
        DataManager.castle.OnUpdateClaim();
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

        public Transform pHeroIcon;
        public ButtonHelper btnAdd;

        public Image imgBar_CalimAmount;
        public TextMeshProUGUI txtClaimAmount;
        public TextMeshProUGUI txtClaimAmount_Front;
        public TextMeshProUGUI txtPerSecond;

        public ButtonHelper btnUpgrade;
        public TextMeshProUGUI[] txtBatchStat;

        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>("Panel");
            txtTitle = scroll.content.GetComponent<TextMeshProUGUI>("txt_title");

            pHeroIcon = scroll.content.Find("Batch/Icons/List");
            btnAdd = pHeroIcon.parent.GetComponent<ButtonHelper>("btn_add");

            var amount = scroll.content.Find("Amount");
            imgBar_CalimAmount = amount.GetComponent<Image>("Bar/img_bar");
            txtClaimAmount_Front = imgBar_CalimAmount.transform.GetComponent<TextMeshProUGUI>("txt_amount");
            txtClaimAmount = amount.GetComponent<TextMeshProUGUI>("txt_amount");
            txtPerSecond = amount.GetComponent<TextMeshProUGUI>("txt_per");

            var batchStat = scroll.content.Find("Batch/Stat");
            txtBatchStat = batchStat.GetComponentsInChildren<TextMeshProUGUI>();
            btnUpgrade = scroll.content.GetComponent<ButtonHelper>("Batch/btn_upgrade");
        }

        public GameObject amountObject => txtClaimAmount.transform.parent.gameObject;

        public string textAmount
        {
            set
            {
                txtClaimAmount.text = value;
                txtClaimAmount_Front.text = value;
            }
        }
    }
    #endregion VALIDATE
}
