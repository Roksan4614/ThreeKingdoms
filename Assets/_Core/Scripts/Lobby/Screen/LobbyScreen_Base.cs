using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public abstract class LobbyScreen_Base : MonoBehaviour, IValidatable
{
    [SerializeField]
    Button m_btnBack;

    [SerializeField]
    RectTransform m_panel;

    bool m_isDoing = false;

    LobbyScreenType m_screenType;

    CancellationTokenSource m_ctsEscape;

    protected virtual void Awake()
    {
        m_btnBack.onClick.AddListener(
            () => Signal.instance.CloseLobbyScreen.Emit(m_screenType));
        m_btnBack = null;
    }

    protected virtual void OnEnable()
    {
        m_ctsEscape = new();
        Utils.WaitEscape(this, () =>
        {
            if (IsCloseScreen())
                Signal.instance.CloseLobbyScreen.Emit(m_screenType);
        }, _token: m_ctsEscape);
    }

    protected virtual void OnDisable()
    {
        m_ctsEscape.Cancel();
        m_ctsEscape.Dispose();
        m_ctsEscape = null;
    }

    protected virtual bool IsCloseScreen() => true;

#if UNITY_EDITOR
    public virtual void OnManualValidate()
    {
        m_btnBack = transform.GetComponent<Button>("Panel/Top/btn_back");
        m_panel = transform.GetComponent<RectTransform>("Panel");
    }
#endif

    public bool isOpenned => gameObject.activeSelf && m_isDoing == false;

    public void Initilize(LobbyScreenType _type)
    {
        gameObject.SetActive(false);
        m_screenType = _type;
    }

    public virtual void Open(LobbyScreenType _prevScreen)
    {
        ActivePanel(true, _prevScreen == LobbyScreenType.None);
    }

    public virtual void Close(bool _isTween = true)
    {
        if (_isTween == false)
            CloseAsync().Forget();
        else
            ActivePanel(false, _isTween);
    }

    protected virtual async UniTask CloseAsync()
    {
        gameObject.SetActive(false);
        await UniTask.WaitForEndOfFrame();
    }

    void ActivePanel(bool _isOpen, bool _isTween)
    {
        if (m_isDoing == true)
            return;

        m_isDoing = true;

        if (_isOpen == true)
            gameObject.SetActive(true);

        var targetScale = Vector3.one * (_isOpen ? 1 : 0.5f);

        TweenCallback callbackFinished = () =>
        {
            m_isDoing = false;

            if (_isOpen == false)
                CloseAsync().Forget();
        };

        if (_isTween)
        {
            m_panel.localScale = Vector3.one * (_isOpen ? 0.5f : 1);
            var duration = 0.15f;

            m_panel.DOScale(targetScale, duration).SetEase(_isOpen ? Ease.OutBack : Ease.InBack)
                .OnComplete(callbackFinished);
        }
        else
        {
            m_panel.localScale = targetScale;
            callbackFinished();
        }
    }
}
