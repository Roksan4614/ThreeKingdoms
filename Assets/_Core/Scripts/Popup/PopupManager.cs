using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public enum PopupType
{
    NONE = -1,

    Hero_Filter,
    Hero_Sort,
    Hero_HeroInfo,
    SelectRegion,

    Modal_Start,
    Modal,
    Modal_TalkSelect,

    MAX
}

public class PopupManager : MonoSingleton<PopupManager>, IValidatable
{
    private Dictionary<PopupType, AsyncOperationHandle<GameObject>> m_dicPopup = new();

    protected override void OnAwake()
    {
        transform.SetSiblingIndex(0);
    }

    protected override void OnDestroy()
    {
        foreach (var h in m_dicPopup)
            h.Value.Release();

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

    public async UniTask<GameObject> OpenPopup(PopupType _popupType, params object[] _data)
    {
        return (await OpenPopup<BasePopupComponent>(_popupType, _data)).gameObject;
    }

    public async UniTask<T> OpenPopup<T>(PopupType _popupType, params object[] _data) where T : BasePopupComponent
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

    public IEnumerator DoOpenPopup(PopupType _popupType, params object[] _data)
    {
        yield return OpenPopupAndWait(_popupType, _data).ToCoroutine();
    }

    public async UniTask OpenPopupAndWait(PopupType _popupType, params object[] _data)
    {
        await OpenPopupAndWait<BasePopupComponent>(_popupType, _data);
    }

    public async UniTask<T> OpenPopupAndWait<T>(PopupType _popupType, params object[] _data) where T : BasePopupComponent
    {
        var popup = await OpenPopup<T>(_popupType, _data);

        await UniTask.WaitUntil(() => popup == null || popup.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken)
            .SuppressCancellationThrow();

        return popup;
    }

    public bool isOpenModal => m_element.pModal.childCount > 0;
    public PopupModalComponent lastPopupModal => isOpenModal ?
        m_element.pModal.GetChild(m_element.pModal.childCount - 1).GetComponent<PopupModalComponent>() : null;

    public void ShowDimm(bool _isShow, bool _isFade = true, bool _isOpercity = false)
    {
        ShowDimmAsync(_isShow, _isFade, _isOpercity).Forget();
    }

    public async UniTask ShowDimmAsync(bool _isShow, bool _isFade = true, bool _isOpercity = false)
    {
        if (_isFade)
        {
            if (_isShow)
                m_element.cgMaxDimm.gameObject.SetActive(true);

            await m_element.cgMaxDimm.DOFade(_isShow ? _isOpercity ? 0.0001f : 1f : 0f, 0.5f).AsyncWaitForCompletion();
        }
        else if (_isShow)
            m_element.cgMaxDimm.alpha = _isOpercity ? 0.0001f : 1f;

        m_element.cgMaxDimm.gameObject.SetActive(_isShow);
    }

    public void SetCanvasCamera() => m_element.canvas.worldCamera = CameraManager.instance.main;

    public async UniTask<StatusType> OpenModalAsync(string _content = null, string _confirm = null, string _cancel = null)
    {
        PopupModalComponent.ModalPopupData popupData = new()
        {
            content = _confirm,
            confirm = _confirm,
            cancel = _cancel,
        };

        var popup = await OpenPopupAndWait<PopupModalComponent>(PopupType.Modal, popupData);

        return popup.statusType;
    }


    #region ALERT
    CancellationTokenSource m_ctsAlert;
    public void AlertShow(string _message, float _posY = 0, bool _isTyping = false, float _duration = 3f)
        => AlertShowAsync(_message, _posY, _isTyping, _duration).Forget();

    public async UniTask AlertShowAsync(string _message, float _posY = 0, bool _isTyping = false, float _duration = 3f)
    {
        if (m_ctsAlert != null)
        {
            m_ctsAlert.Cancel();
            m_ctsAlert.Dispose();
        }
        m_ctsAlert = new();

        await m_element.alertData.ShowAsync(_message, m_ctsAlert.Token, _posY, _isTyping, _duration);

        m_ctsAlert = null;
    }
    public void AlertDisable() => AlertDisableAsync().Forget();

    public async UniTask AlertDisableAsync()
    {
        if (m_ctsAlert != null)
        {
            m_ctsAlert.Cancel();
            m_ctsAlert.Dispose();
            m_ctsAlert = null;
        }
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
            if (size.x > Screen.width * 0.9f)
            {
                m_fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                size.x = Screen.width * 0.9f;
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

        public void Initialize(Transform _transform)
        {
            m_canvas = _transform.GetComponent<Canvas>();
            m_cgMaxDimm = _transform.GetComponent<CanvasGroup>("MAX_Dimm");

            alertData.Initialize(_transform.Find("Alert"));

            pPopup = _transform.Find("Popup");
            pModal = _transform.Find("Modal");
        }
    }
    #endregion VALIDATE
}
