using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public enum PopupType
{
    NONE = -1,

    SelectRegion,

    Hero_Filter,
    Hero_HeroInfo,

    Castle_HeroList,
    Castle_Mission,

    UseTimeStone,

    LobbyStoryMode,
    LobbyBossRaid,
    LobbyTournament,
    LobbyTournament_History,
    UserInfo,

    UpgradeGuide,

    BossRaidResult,

    DailyDungeonResult,

    Modal_Start,
    Modal,
    Modal_TalkSelect,

    MAX
}

public class PopupManager : MonoSingleton<PopupManager>, IValidatable
{
    private Dictionary<PopupType, AsyncOperationHandle<GameObject>> m_dicPopup = new();

    CancellationTokenSource m_cts;

    PopupHeroInfo m_heroInfo;

    protected override void OnAwake()
    {
        transform.SetSiblingIndex(0);

        m_cts = m_cts.ReleaseCTS(true);

        Signal.instance.ChangeDisplayMode.connect = OnChangeDisplayMode;
    }

    protected override void OnDestroy()
    {
        foreach (var h in m_dicPopup)
            h.Value.Release();

        m_cts = m_cts.ReleaseCTS();
        base.OnDestroy();
    }

    public async UniTask<GameObject> LoadAsset(PopupType _popupType)
    {
        if (m_dicPopup.ContainsKey(_popupType))
            return m_dicPopup[_popupType].Result;

        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_dicPopup.ContainsKey(_popupType) == false)
                    m_dicPopup.Add(_popupType, data.Value);
            }
        }, null, $"Popup/{_popupType}.prefab");

        return m_dicPopup.ContainsKey(_popupType) ? m_dicPopup[_popupType].Result : null;
    }

    public void OpenPopup(PopupType _popupType, params object[] _data)
        => OpenPopupAsync<BasePopupComponent>(_popupType, _data).Forget();

    public async UniTask<T> OpenPopupAsync<T>(PopupType _popupType, params object[] _data) where T : BasePopupComponent
    {
        GameObject popupObject = await LoadAsset(_popupType);

        if (popupObject == null)
            return null;

        var popup = Instantiate(popupObject,
            _popupType > PopupType.Modal_Start ? m_element.pModal : m_element.pPopup)
            .GetComponent<BasePopupComponent>();
        popup.name = _popupType.ToString();

        popup.OpenPopup(_data);

        return popup?.GetComponent<T>();
    }

    public async UniTask OpenPopupAndWait(PopupType _popupType, params object[] _data)
    {
        await OpenPopupAndWait<BasePopupComponent>(_popupType, _data);
    }

    public async UniTask<T> OpenPopupAndWait<T>(PopupType _popupType, params object[] _data) where T : BasePopupComponent
    {
        var popup = await OpenPopupAsync<T>(_popupType, _data);

        await UniTask.WaitUntil(() => popup == null || popup.gameObject.activeSelf == false, cancellationToken: m_cts.Token)
            .SuppressCancellationThrow();

        return popup;
    }

    public bool IsOpenPopup(params PopupType[] _popupType)
    {
        if (_popupType.Length == 0)
        {
            for (int i = 0; i < m_element.pPopup.childCount; i++)
            {
                if (m_element.pPopup.GetChild(i).gameObject.activeSelf)
                    return true;
            }
        }
        else
        {
            List<PopupType> popups = _popupType.ToList();
            for (int i = 0; i < m_element.pPopup.childCount; i++)
            {
                var popup = m_element.pPopup.GetChild(i).GetComponent<BasePopupComponent>();
                if (popup.gameObject.activeSelf == true && popups.Contains(popup.popupType) == true)
                    return true;
            }
        }

        return false;
    }

    public T GetPopup<T>(PopupType _popupType) where T : BasePopupComponent
    {
        for (int i = 0; i < m_element.pPopup.childCount; i++)
        {
            var popup = m_element.pPopup.GetChild(i).GetComponent<T>();
            if (popup != null)
                return popup;
        }

        return null;
    }

    public bool isOpenModal => m_element.pModal.childCount > 0;
    public PopupModalComponent lastPopupModal => isOpenModal ?
        m_element.pModal.GetChild(m_element.pModal.childCount - 1).GetComponent<PopupModalComponent>() : null;

    public void ShowDimm(bool _isShow, bool _isFade = true, bool _isOpercity = false, float _duration = .5f, float _durationWait = .5f)
    {
        ShowDimmAsync(_isShow, _isFade, _isOpercity, _duration, _durationWait).Forget();
    }

    public bool isTweenDimm => m_tweenDimm != null && m_tweenDimm.IsPlaying();
    Tween m_tweenDimm;
    CancellationTokenSource m_ctsDimm;
    public async UniTask ShowDimmAsync(bool _isShow, bool _isFade = true, bool _isOpercity = false, float _duration = .5f, float _durationWait = .5f)
    {
        m_tweenDimm?.Kill();

        m_ctsDimm = m_ctsDimm.ReleaseCTS(true);

        if (_isFade)
        {
            if (_isShow)
                m_element.cgMaxDimm.gameObject.SetActive(true);

            m_tweenDimm = m_element.cgMaxDimm.DOFade(_isShow ? _isOpercity ? 0.0001f : 1f : 0f, _duration);
            await m_tweenDimm.ToUniTask(TweenCancelBehaviour.Kill, m_ctsDimm.Token);
        }
        else if (_isShow)
            m_element.cgMaxDimm.alpha = _isOpercity ? 0.0001f : 1f;

        m_tweenDimm = null;
        m_ctsDimm = null;
        m_element.cgMaxDimm.gameObject.SetActive(_isShow);

        await UniTask.WaitForSeconds(_durationWait);
    }

    public void SetCanvasCamera() => m_element.canvas.worldCamera = CameraManager.instance.main;

    public async UniTask<StatusType> OpenModalAsync(string _content = null, string _confirm = null, string _cancel = null, UnityAction<StatusType> _callback = null)
    {
        PopupModalComponent.ModalPopupData popupData = new()
        {
            content = _content,
            confirm = _confirm,
            cancel = _cancel,
            callback = _callback
        };

        var popup = await OpenPopupAndWait<PopupModalComponent>(PopupType.Modal, popupData);

        _callback?.Invoke(popup.statusType);

        return popup.statusType;
    }

    public async UniTask<int> OpenTalkSelectAsync(params string[] _questions)
    {
        PopupModal_TalkSelectComponent.ModalTalkData talkData = new();
        talkData.options = _questions.ToArray();

        var popup = await OpenPopupAndWait<PopupModal_TalkSelectComponent>(PopupType.Modal_TalkSelect, talkData);

        await UniTask.WaitForEndOfFrame(cancellationToken: m_cts.Token);

        return popup.selelctOption + 1;
    }

    void OnChangeDisplayMode(bool _isLandscape)
    {
        CanvasScaler canvasScaler = m_element.canvasScaler;

        var r = canvasScaler.referenceResolution;
        r.x = _isLandscape ? 1920 : 1080;
        r.y = _isLandscape ? 1080 : 1920;
        canvasScaler.referenceResolution = r;

        canvasScaler.matchWidthOrHeight = _isLandscape ? 1 : 0;
    }

    public void CloseAll()
    {
        int max = Mathf.Max(m_element.pPopup.childCount, m_element.pModal.childCount);

        for (int i = 0; i < max; i++)
        {
            if (i < m_element.pPopup.childCount)
                m_element.pPopup.GetChild(i).GetComponent<BasePopupComponent>().Close();
            if (i < m_element.pModal.childCount)
                m_element.pModal.GetChild(i).GetComponent<BasePopupComponent>().Close();
        }
    }

    public Vector2 canvasSize => m_element.canvasScaler.referenceResolution;

    public Vector2 lt => m_element.lt.position;
    public Vector2 rb => m_element.rb.position;

    #region ALERT
    CancellationTokenSource m_ctsAlert;
    public void AlertShow(string _message, float _addPosY = 0, bool _isTyping = false, float _duration = 3f)
        => AlertShowAsync(_message, _addPosY, _isTyping, _duration).Forget();

    public async UniTask AlertShowAsync(string _message, float _addPosY = 0, bool _isTyping = false, float _duration = 3f)
    {
        m_ctsAlert = m_ctsAlert.ReleaseCTS(true);

        await m_element.alertData.ShowAsync(_message, m_ctsAlert.Token, _addPosY, _isTyping, _duration);

        m_ctsAlert = null;
    }
    public void AlertDisable() => AlertDisableAsync().Forget();

    public async UniTask AlertDisableAsync()
    {
        m_ctsAlert = m_ctsAlert.ReleaseCTS();
        await m_element.alertData.DisableAsync();
    }

    public bool isAlerting => m_element.alertData.isActive;

    [Serializable]
    struct AlertData
    {
        [SerializeField] RectTransform m_rt;
        [SerializeField] HorizontalLayoutGroup m_layout;
        [SerializeField] ContentSizeFitter m_fitter;
        [SerializeField] TextMeshProUGUI m_txtAlert;

        public float posYDefault;

        public void Initialize(Transform _transform)
        {
            m_rt = (RectTransform)_transform;
            m_layout = _transform.GetComponent<HorizontalLayoutGroup>();
            m_fitter = _transform.GetComponent<ContentSizeFitter>();
            m_txtAlert = _transform.GetComponent<TextMeshProUGUI>("Text");

            posYDefault = m_rt.anchoredPosition.y;

            m_rt.gameObject.SetActive(false);
        }

        public bool isActive => m_rt.gameObject.activeSelf;

        public async UniTask ShowAsync(string _message, CancellationToken _token, float _addPosY = 0, bool _isTyping = false, float _duration = 2)
        {
            var anchorPos = m_rt.anchoredPosition;
            anchorPos.y = posYDefault + _addPosY;
            m_rt.anchoredPosition = anchorPos;

            if (m_rt.gameObject.activeSelf == false)
                Utils.SetActivePunch(m_rt, true);

            m_fitter.verticalFit = m_fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            m_txtAlert.text = _message;
            m_rt.ForceRebuildLayout();

            var size = m_rt.sizeDelta;
            var screenWidth = instance.canvasSize.x;
            if (size.x > screenWidth * 0.9f)
            {
                m_fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                size.x = screenWidth * 0.9f;
                m_rt.sizeDelta = size;

                m_rt.ForceRebuildLayout();
            }

            // 타이핑 연출 할거야?
            if (_isTyping == true)
            {
                m_fitter.verticalFit = m_fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                m_txtAlert.text = "";

                for (int i = 0; i < _message.Length; i++)
                {
                    var m = _message[i];

                    string visibleText = _message.Substring(0, i);
                    string invisibleText = _message.Substring(i);

                    m_txtAlert.text = $"{visibleText}<color=#00000000>{invisibleText}";

                    if (m == '<')
                    {
                        char tagMsg = m;
                        while (true)
                        {
                            var fm = _message[i++];
                            tagMsg += fm;
                            if (fm == '>')
                                break;
                        }

                        continue;
                    }
                    await UniTask.WaitForSeconds(0.03f, cancellationToken: _token);

                    if (ControllerManager.isClick)
                        break;
                }

                m_txtAlert.text = _message;
            }

            // 자동 사라지기 껏어??
            if (_duration > 0)
            {
                await UniTask.WaitForSeconds(_duration, cancellationToken: _token);
                Disable();
            }
            else
                await UniTask.WaitUntilCanceled(_token);
        }

        public void Disable() => DisableAsync().Forget();
        public async UniTask DisableAsync()
        {
            if (m_rt.gameObject.activeSelf == false)
                return;

            await Utils.SetActivePunchAsync(m_rt, false);
        }
    }
    #endregion ALERT

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        [SerializeField] Canvas m_canvas;
        public Canvas canvas => m_canvas;

        [SerializeField] CanvasGroup m_cgMaxDimm;
        public CanvasGroup cgMaxDimm => m_cgMaxDimm;

        public AlertData alertData;

        public Transform pPopup;
        public Transform pModal;

        public CanvasScaler canvasScaler;

        public Transform lt;
        public Transform rb;

        public void Initialize(Transform _transform)
        {
            m_canvas = _transform.GetComponent<Canvas>();
            m_cgMaxDimm = _transform.GetComponent<CanvasGroup>("MAX_Dimm");

            alertData.Initialize(_transform.Find("Alert"));

            pPopup = _transform.Find("Popup");
            pModal = _transform.Find("Modal");
            canvasScaler = _transform.GetComponent<CanvasScaler>();

            lt = _transform.Find("lt");
            rb = _transform.Find("rb");
        }
    }
    #endregion VALIDATE
}
