using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroIconComponent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IValidatable
{
    public HeroInfoData data { get; private set; }
    public Button.ButtonClickedEvent onClick
        => m_element.btnHero?.onClick;

    UnityAction<HeroIconComponent, bool> m_onClick;
    UnityAction<HeroIconComponent> m_onClickAction;

    LobbyScreen_Hero_Hero m_screenHero;
    TooltipWorker m_toolkip;

    bool m_isOpenPopup;

    private void Awake()
    {
        m_toolkip = transform.GetComponent<TooltipWorker>("Panel/Icon/Tooltip");
    }

    private void Start()
    {
        m_element.btnHero?.onClick.AddListener(() => m_onClick?.Invoke(this, false));
        m_element.btnAction?.onClick.AddListener(() =>
        {
            if (m_isOpenPopup == false)
                m_onClickAction?.Invoke(this);
        });
    }

    public void SetHeroData(HeroInfoData _data
        , UnityAction<HeroIconComponent, bool> _onClick //bool > isRightClick
        , UnityAction<HeroIconComponent> _onClickAction
        , bool _isForceUpdate = false
        )
    {
        if (_data.skin.Equals(data.skin) && _isForceUpdate == false)
            return;

        if (_onClick != null)
        {
            m_screenHero = _onClick.Target as LobbyScreen_Hero_Hero;

            m_onClick = _onClick;
            m_onClickAction = _onClickAction;
        }

        m_element.icon.parent.gameObject.SetActive(true);
        m_element.btnAction?.gameObject.SetActive(false);

        if (m_element.btnHero != null)
            m_element.btnHero.interactable = true;

        UpdateHeroInfo(_data);

        if (m_toolkip != null)
            m_toolkip.text = _data.name;
    }
    public void UpdateHeroInfo(HeroInfoData _data)
    {
        UpdateHeroInfoAsync(_data).Forget();
    }

    public async UniTask UpdateHeroInfoAsync(HeroInfoData _data)
    {
        data = _data;

        if (m_element.badge)
            m_element.badge.SetActive(_data.isBatch);

        if (m_element.txtLevel)
            m_element.txtLevel.text = $"+{_data.enchantLevel}";

        if (m_element.txtName)
            m_element.txtName.text = _data.name;

        if (m_element.dimm)
            m_element.dimm.SetActive(_data.isMine == false);

        if (_data.isMine)
        {
            m_element.txtName.color = Color.gray1;
            m_element.outline.color =
                Palette.instance.data.Get("icon_outline_grade_" + _data.grade.ToString().ToLower());
        }
        else
        {
            m_element.outline.color = Color.gray;
            m_element.txtName.color = Color.gray;
        }


        bool isFinded = false;
        for (int i = 0; i < m_element.icon.childCount; i++)
        {
            var obj = m_element.icon.GetChild(i).gameObject;
            obj.SetActive(obj.name.Contains(_data.skin));

            if (isFinded == false && obj.activeSelf == true)
                isFinded = true;
        }

        if (isFinded == false)
        {
            var prefab = await AddressableManager.instance.GetHeroIconAsync(_data.skin);

            if (prefab != null)
            {
                var icon = Instantiate(prefab, m_element.icon);

                var rtParent = icon.transform.parent as RectTransform;
                await UniTask.WaitUntil(() => rtParent.rect.width > 0 || rtParent.rect.height > 0, cancellationToken: destroyCancellationToken);

                if (icon != null)
                    icon.AutoResizeParent().name = _data.skin;
            }
            else
            {
                m_element.SetActiveName(true);
                IngameLog.Add("UpdateHeroInfoAsync: prefab is null");
            }
        }
    }

    public void Disable()
    {
        m_element.txtLevel.text = "";
        m_element.txtName.text = "";
        m_element.icon.parent.gameObject.SetActive(false);
        m_element.btnAction.gameObject.SetActive(false);
        m_element.btnHero.interactable = false;
        m_element.badge.SetActive(false);
        data = default;
    }

    public void SetActiveButton(bool _isActive, bool _isChange = false)
    {
        _isActive = _isActive && data.isActive;

        m_element.btnAction.gameObject.SetActive(_isActive);
        if (_isActive == true)
        {
            m_element.objActionText.SetActive(_isChange == false);
            m_element.objActionChange.SetActive(_isChange);
        }
    }

    public void IsValide(string _keyHero)
        => data.key.Equals(_keyHero);

    private CancellationTokenSource m_cts;
    public async void OnPointerDown(PointerEventData eventData)
    {
        if (m_screenHero == null && m_onClick == null)
            return;

        if (ControllerManager.instance.isRightClick == true)
        {
            m_onClick(this, true);
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift) == false)
        {
            RelaseCTS();
            m_cts = new CancellationTokenSource();

            bool isCanceled = await UniTask.Delay(500, cancellationToken: m_cts.Token).SuppressCancellationThrow();

            RelaseCTS();
            if (isCanceled == true)
                return;
        }
        if (m_screenHero == null && m_onClick != null)
        {
            m_onClick(this, true);
        }
        else
        {
            m_isOpenPopup = true;
            await m_screenHero.OpenHeroInfoPopupAsync(data);
            m_isOpenPopup = false;
            SetActiveButton(false);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RelaseCTS();
    }

    void RelaseCTS()
        => m_cts = m_cts.ReleaseCTS();

    public virtual void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public Transform icon;
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtLevel;
        public Button btnHero;
        public Button btnAction;

        public GameObject objActionChange;
        public GameObject objActionText;

        public GameObject dimm;
        public GameObject badge;
        public Image outline;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            icon = panel.Find("Icon/Panel");
            txtName = panel.Find("txt_name")?.GetComponent<TextMeshProUGUI>();
            txtLevel = panel.Find("txt_level")?.GetComponent<TextMeshProUGUI>();

            btnHero = _transform.GetComponent<Button>();
            btnAction = panel.GetComponent<Button>("btn_action");

            if (btnAction != null)
            {
                objActionChange = btnAction.transform.Find("Image").gameObject;
                objActionText = btnAction.transform.Find("Text").gameObject;
            }

            dimm = panel.Find("Icon/Dimm")?.gameObject;
            outline = panel.GetComponent<Image>("Icon/Outline");

            badge = panel.Find("Badge")?.gameObject;
        }

        public void SetActiveName(bool _isActive)
            => txtName.gameObject.SetActive(_isActive);
    }
}
