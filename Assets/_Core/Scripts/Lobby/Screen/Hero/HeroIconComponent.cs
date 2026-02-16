using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class HeroIconComponent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IValidatable
{
    [SerializeField]
    public HeroInfoData data { get; private set; }

    UnityAction<HeroIconComponent> m_onClick;
    UnityAction<HeroIconComponent> m_onClickAction;

    bool m_isOpenPopup;
    Coroutine m_coPushHold;

    private void Start()
    {
        m_element.btnHero.onClick.AddListener(() => m_onClick(this));
        m_element.btnAction?.onClick.AddListener(() =>
        {
            if (m_isOpenPopup == false)
                m_onClickAction(this);
        });
    }


    public void SetHeroData(HeroInfoData _data
        , UnityAction<HeroIconComponent> _onClick
        , UnityAction<HeroIconComponent> _onClickAction)
    {
        if (_data.skin.Equals(data.skin))
            return;

        m_onClick = _onClick;
        m_onClickAction = _onClickAction;

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
        var prevData = data;
        data = _data;

        m_element.batch.SetActive(_data.isBatch);

        m_element.txtLevel.text = _data.enchantLevel.ToString();
        m_element.txtName.text = _data.name;

        m_element.dimm.SetActive(_data.isMine == false);
        m_element.outline.color = _data.isMine ? Color.black : Color.gray;

        bool isFinded = false;
        for (int i = 0; i < m_element.icon.childCount; i++)
        {
            bool isValid = prevData.isActive && prevData.skin.Equals(_data.skin);
            m_element.icon.GetChild(i).gameObject.SetActive(isValid);

            if (isFinded == false && isValid == true)
                isFinded = isValid;
        }

        if (isFinded == false)
        {
            var prefab = await AddressableManager.instance.GetHeroIcon(_data.skin)
                .AttachExternalCancellation(destroyCancellationToken);

            if (prefab != null)
            {
                Instantiate(prefab, m_element.icon)
                    .AutoResizeParent()
                    .name = _data.skin;
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
        m_element.batch.SetActive(false);
        data = default;
    }

    public void SetActiveButton(bool _isActive)
    {
        m_element.btnAction.gameObject.SetActive(_isActive);
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
        RelaseCTS();
        m_cts = new CancellationTokenSource();

        bool isCanceled = await UniTask.Delay(500, cancellationToken: m_cts.Token).SuppressCancellationThrow();

        RelaseCTS();
        if (isCanceled == true)
            return;

        OpenHeroInfoPopup().Forget();
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

    async UniTask OpenHeroInfoPopup()
    {
        m_isOpenPopup = true;
        await PopupManager.instance.OpenPopupAndWait(PopupType.Hero_HeroInfo);

        m_isOpenPopup = false;
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

        public GameObject dimm;
        public GameObject batch;
        public Image outline;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            icon = panel.Find("Icon/Panel");
            txtName = panel.Find("txt_name").GetComponent<TextMeshProUGUI>();
            txtLevel = panel.Find("txt_level").GetComponent<TextMeshProUGUI>();

            btnHero = _transform.GetComponent<Button>();
            btnAction = panel.GetComponent<Button>("btn_action");

            dimm = panel.Find("Icon/Dimm").gameObject;
            outline = panel.GetComponent<Image>("Icon/Outline");

            batch = panel.Find("Batch").gameObject;
        }
    }
}
