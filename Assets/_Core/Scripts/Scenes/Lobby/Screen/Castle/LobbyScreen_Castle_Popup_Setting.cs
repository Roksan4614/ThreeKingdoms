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

    CastleData m_castleData;

    private void Awake()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(() => Close());

        m_baseIcon = m_element.pHeroIcon.GetChild(0);
        m_baseIcon.transform.SetParent(m_element.pHeroIcon.parent);
        m_baseIcon.gameObject.SetActive(false);

        m_element.btnAdd.onClick.AddListener(() => OpenHeroListPopupAsync().Forget());

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
    }

    public async UniTask OpenAsync(CastleObjectType _type, CancellationToken _cancelToken)
    {
        var rtScroll = (RectTransform)m_element.scroll.transform;
        gameObject.SetActive(true);
        m_isClose = false;

        m_castleData = DataManager.castle.GetCaslteData(_type);

        Dictionary<CastleObjectType, string> dbTitle = new()
        {
            { CastleObjectType.Palace, "궁성"},
            { CastleObjectType.Market, "시장"},
            { CastleObjectType.Farm, "농지"},
            { CastleObjectType.Office, "관아"},
            { CastleObjectType.Merchant, "행상"},
            { CastleObjectType.Gate, "성문"},
        };

        m_element.txtTitle.text = $"Lv.{m_castleData.level} {dbTitle[_type]}";

        m_element.scroll.content.anchoredPosition = Vector2.zero;
        m_element.scroll.enabled = true;

        rtScroll.anchoredPosition = Vector2.zero;
        rtScroll.DOAnchorPosY(rtScroll.rect.height, 0.1f);

        SetBatchHero();

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

    async UniTask OpenHeroListPopupAsync()
    {
        m_poupHeroList = await PopupManager.instance
            .OpenPopup<PopupCastleHeroListComponent>(PopupType.Castle_HeroList, m_castleData.heroes);

        await UniTask.WaitUntil(() => m_poupHeroList == null);

        var heroes = m_poupHeroList.heroes;
        m_poupHeroList = null;

        if (heroes.Count == m_castleData.heroes.Count)
        {
            int i = 0;
            for (; i < heroes.Count; i++)
            {
                if (m_castleData.heroes.Contains(heroes[i]) == false)
                    break;
            }

            if (i == heroes.Count)
                return;
        }

        m_castleData.heroes = heroes;
        SetBatchHero();
        DataManager.castle.UpdateBatchHero(m_castleData);
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

        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>("Panel");
            txtTitle = scroll.content.GetComponent<TextMeshProUGUI>("txt_title");

            pHeroIcon = scroll.content.Find("Batch/Icons/List");
            btnAdd = pHeroIcon.parent.GetComponent<ButtonHelper>("btn_add");
        }
    }
    #endregion VALIDATE
}
