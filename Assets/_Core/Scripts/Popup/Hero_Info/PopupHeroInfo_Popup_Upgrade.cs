using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Popup_Upgrade : MonoBehaviour, IValidatable
{
    enum UpgradeType
    {
        Upgrade,
        Enchant
    }

    float m_startPosY;
    StatusType m_status = StatusType.Wait;

    HeroInfoData m_prevHeroData;
    HeroInfoData m_heroInfoData;
    public HeroInfoData heroInfoData => m_heroInfoData;

    public bool isNeedUpdate => m_status == StatusType.Success;

    private void Awake()
    {
        m_startPosY = m_element.panel.anchoredPosition.y;

        var size = m_element.panel.sizeDelta;
        size.y = Screen.height * 0.5f + m_startPosY;
        m_element.panel.sizeDelta = size;

        m_element.dimm.onClick.AddListener(() => Close());

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
                    Close(Ease.Linear);
                }
            }
        });

        m_element.btnConfirm.onClick.AddListener(() => OnButtonAsync_Confirm().Forget());
    }

    void SetInfo(UpgradeType _type)
    {
        m_element.btnConfirm.text = $"_{_type}";

        m_element.parentUpgrade.gameObject.SetActive(_type == UpgradeType.Upgrade);
        m_element.parentEnchant.gameObject.SetActive(_type == UpgradeType.Enchant);

        if (_type == UpgradeType.Upgrade)
        {
            m_element.txtTitle.text = m_heroInfoData.gradeName;

            m_element.btnArrowLeft.gameObject.SetActive(m_heroInfoData.grade > m_prevHeroData.grade);
            m_element.btnArrowRight.gameObject.SetActive(m_heroInfoData.grade < GradeType.MAX - 1);
        }
        else
        {
            m_element.txtTitle.text = $"{m_heroInfoData.enchantLevel - 1}_>_{m_heroInfoData.enchantLevel}";
        }
    }

    public void OnButton_UpgradeArrow(bool _isLeft)
    {
        m_heroInfoData.grade += _isLeft ? -1 : 1;
        SetInfo(UpgradeType.Upgrade);
    }

    async UniTask OnButtonAsync_Confirm()
    {
        m_element.btnConfirm.interactable = false;

        await UniTask.WaitForEndOfFrame();

        DataManager.userInfo.Update(m_heroInfoData);
        Signal.instance.UpdateHeroStat.Emit(m_heroInfoData.key);

        Close();
        m_status = StatusType.Success;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_isUpgrade">Upgrade: true / Enchant: false</param>
    /// <returns></returns>
    public async UniTask OpenAsyn(HeroInfoData _heroInfoData, bool _isUpgrade)
    {
        m_prevHeroData = m_heroInfoData = _heroInfoData;

        m_element.scroll.content.anchoredPosition = Vector2.zero;
        m_element.scroll.enabled = true;

        m_element.btnConfirm.interactable = true;
        m_element.dimm.interactable = true;

        m_status = StatusType.Wait;
        gameObject.SetActive(true);

        SetInfo(_isUpgrade ? UpgradeType.Upgrade : UpgradeType.Enchant);

        var pos = m_element.panel.anchoredPosition;
        pos.y = m_startPosY - m_element.panel.sizeDelta.y;

        m_element.panel.anchoredPosition = pos;

        await m_element.panel.DOAnchorPosY(m_startPosY, .1f).SetEase(Ease.OutCubic).AsyncWaitForCompletion();

        await UniTask.WaitUntil(() => m_element.dimm.interactable == false, cancellationToken: destroyCancellationToken);
    }

    public void Close(Ease _easeType = Ease.InBack)
    {
        m_element.dimm.interactable = false;
        var targetPosY = m_startPosY - m_element.panel.sizeDelta.y;

        m_element.panel.DOAnchorPosY(targetPosY, .1f).SetEase(_easeType)
            .OnComplete(() => gameObject.SetActive(false));
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    public ButtonHelper btnUpgradeLeft => m_element.btnArrowLeft;
    public ButtonHelper btnUpgradeRight => m_element.btnArrowRight;

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform panel;
        public Button dimm;
        public ButtonHelper btnConfirm;
        public ScrollRect scroll;

        public TextMeshProUGUI txtTitle;

        public ButtonHelper btnArrowLeft;
        public ButtonHelper btnArrowRight;
        public Transform parentUpgrade => btnArrowLeft.transform.parent;

        public RectTransform rtBar;
        public Transform parentEnchant => rtBar.parent.parent;


        public void Initialize(Transform _transform)
        {
            panel = (RectTransform)_transform.Find("Panel");
            dimm = _transform.GetComponent<Button>("Dimm");
            scroll = panel.GetComponent<ScrollRect>();
            btnConfirm = scroll.content.GetComponent<ButtonHelper>("btn_confirm");

            txtTitle = scroll.content.GetComponent<TextMeshProUGUI>("txt_grade");

            var parentUpgrade = scroll.content.Find("Upgrade");
            {
                btnArrowLeft = parentUpgrade.GetComponent<ButtonHelper>("btn_left");
                btnArrowRight = parentUpgrade.GetComponent<ButtonHelper>("btn_right");
            }

            var parentEnchant = scroll.content.Find("Enchant");
            {
                rtBar = parentEnchant.GetComponent<RectTransform>("Mileage/img_bar");
            }
        }
    }
    #endregion VALIDATA
}
