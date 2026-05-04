using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Castle_Popup_Setting : MonoBehaviour, IValidatable
{
    bool m_isClose = false;

    PopupCastleHeroListComponent m_poupHeroList;

    private void Awake()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(() => Close());

        m_element.btnAdd.onClick.AddListener(() => OpenHeroListPopupAsync().Forget());
    }

    public async UniTask OpenAsync(CastleObjectType _type, CancellationToken _cancelToken)
    {
        var rtScroll = (RectTransform)m_element.scroll.transform;
        gameObject.SetActive(true);
        m_isClose = false;

        Dictionary<CastleObjectType, string> dbTitle = new()
        {
            { CastleObjectType.Palace, "궁성"},
            { CastleObjectType.Market, "시장"},
            { CastleObjectType.Farm, "농지"},
            { CastleObjectType.Office, "관아"},
            { CastleObjectType.Merchant, "행상"},
            { CastleObjectType.Gate, "성문"},
        };

        m_element.txtTitle.text = $"Lv.1 " + dbTitle[_type];

        rtScroll.anchoredPosition = Vector2.zero;
        rtScroll.DOAnchorPosY(rtScroll.rect.height, 0.1f);

        await UniTask.WaitUntil(() => m_isClose == true, cancellationToken: _cancelToken);
    }


    public bool CloseEscape()
    {
        if (m_poupHeroList != null)
        {
            m_poupHeroList.Close();
            return false;
        }

        gameObject.SetActive(false);
        return true;
    }

    public void Close(StatusType _result = StatusType.Cancel)
    {
        var rtScroll = (RectTransform)m_element.scroll.transform;
        rtScroll.DOAnchorPosY(0, 0.1f).SetEase(Ease.InBack).OnComplete(() => gameObject.SetActive(false));

        Utils.AfterSecond(() => m_isClose = true, 0.05f);
    }

    async UniTask OpenHeroListPopupAsync()
    {
        m_poupHeroList = await PopupManager.instance.OpenPopup<PopupCastleHeroListComponent>(PopupType.Castle_HeroList);

        await UniTask.WaitUntil(() => m_poupHeroList == null);
        m_poupHeroList = null;
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
