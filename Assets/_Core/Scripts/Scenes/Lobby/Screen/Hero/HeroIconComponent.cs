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

    UnityAction<HeroIconComponent, bool> m_onClick;
    UnityAction<HeroIconComponent> m_onClickAction;

    LobbyScreen_Hero m_screenHero;

    bool m_isOpenPopup;
    Coroutine m_coPushHold;

    private void Start()
    {
        m_element.btnHero.onClick.AddListener(() => m_onClick?.Invoke(this, false));
        m_element.btnAction?.onClick.AddListener(() =>
        {
            if (m_isOpenPopup == false)
                m_onClickAction?.Invoke(this);
        });
    }


    public void SetHeroData(HeroInfoData _data
        , UnityAction<HeroIconComponent, bool> _onClick
        , UnityAction<HeroIconComponent> _onClickAction
        )
    {
        if (_data.skin.Equals(data.skin))
            return;

        if (_onClick != null)
        {
            m_screenHero = _onClick.Target as LobbyScreen_Hero;

            m_onClick = _onClick;
            m_onClickAction = _onClickAction;
        }

        m_element.icon.parent.gameObject.SetActive(true);
        m_element.btnAction.gameObject.SetActive(false);

        m_element.btnHero.interactable = true;

        UpdateHeroInfo(_data);
    }
    public void UpdateHeroInfo(HeroInfoData _data)
    {
        UpdateHeroInfoAsync(_data).Forget();
    }

    public async UniTask UpdateHeroInfoAsync(HeroInfoData _data)
    {
        data = _data;

        m_element.badge.SetActive(_data.isBatch);

        m_element.txtLevel.text = _data.enchantLevel.ToString();
        m_element.txtName.text = _data.name;

        m_element.dimm.SetActive(_data.isMine == false);
        m_element.outline.color = _data.isMine ? Color.black : Color.gray;

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
            var prefab = await AddressableManager.instance.GetHeroIconAsync(_data.skin)
                .AttachExternalCancellation(destroyCancellationToken);

            if (prefab != null)
            {
                var icon = Instantiate(prefab, m_element.icon);

                var rtParent = icon.transform.parent as RectTransform;
                await UniTask.WaitUntil(() => rtParent.rect.width > 0 || rtParent.rect.height > 0);

                icon.AutoResizeParent().name = _data.skin;
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
        if (data.isMain == true && StageManager.instance.isClearFirstStage == false)
            return;

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

    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }

    private CancellationTokenSource m_cts;
    public async void OnPointerDown(PointerEventData eventData)
    {
        if (m_screenHero == null)
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

        m_isOpenPopup = true;
        await m_screenHero.OpenHeroInfoPopup(data);
        m_isOpenPopup = false;
        SetActiveButton(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RelaseCTS();
    }

    void RelaseCTS()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
    }


    [SerializeField]
    //[SerializeField, HideInInspector]
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
            txtName = panel.Find("txt_name").GetComponent<TextMeshProUGUI>();
            txtLevel = panel.Find("txt_level").GetComponent<TextMeshProUGUI>();

            btnHero = _transform.GetComponent<Button>();
            btnAction = panel.GetComponent<Button>("btn_action");

            objActionChange = btnAction.transform.Find("Image").gameObject;
            objActionText = btnAction.transform.Find("Text").gameObject;

            dimm = panel.Find("Icon/Dimm").gameObject;
            outline = panel.GetComponent<Image>("Icon/Outline");

            badge = panel.Find("Badge").gameObject;
        }

        public void SetActiveName(bool _isActive)
            => txtName.gameObject.SetActive(_isActive);
    }
}
