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

    protected CancellationTokenSource m_cts;

    protected virtual void Awake()
    {
        m_btnBack.onClick.AddListener(
            () => Signal.instance.CloseLobbyScreen.Emit(m_screenType));
        m_btnBack = null;
    }

    protected virtual void OnEnable()
    {
        Release_CTS();

        m_cts = new();
        Utils.WaitEscape(this, () =>
        {
            if (IsCloseScreen())
                Signal.instance.CloseLobbyScreen.Emit(m_screenType);
        }, _token: m_cts.Token);
    }

    protected virtual void OnDisable()
        => Release_CTS();


    void Release_CTS()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
    }

    protected virtual bool IsCloseScreen() => true;

    public virtual void OnManualValidate()
    {
        m_btnBack = transform.GetComponent<Button>("Panel/Top/btn_back");
        m_panel = transform.GetComponent<RectTransform>("Panel");
    }

    public bool isOpenned => gameObject.activeSelf && m_isDoing == false;

    public void Initilize(LobbyScreenType _type)
    {
        gameObject.SetActive(false);
        m_screenType = _type;
    }

    public virtual void Open(LobbyScreenType _prevScreen)
    {
        ActivePanelAsync(true, _prevScreen == LobbyScreenType.None).Forget();
    }

    public virtual void Close(bool _isTween = true)
    {
        if (_isTween == false)
            CloseAsync().Forget();
        else
            ActivePanelAsync(false, _isTween).Forget();
    }

    protected virtual async UniTask CloseAsync()
    {
        gameObject.SetActive(false);
        await UniTask.WaitForEndOfFrame();
    }

    async UniTask ActivePanelAsync(bool _isOpen, bool _isTween)
    {
        if (m_isDoing == true)
            return;
        m_isDoing = true;

        if (_isOpen == true)
            gameObject.SetActive(true);

        var targetScale = Vector3.one * (_isOpen ? 1 : 0.5f);

        if (_isTween)
            await Utils.SetActivePunchAsync(m_panel, _isOpen, false);
        else
            m_panel.localScale = targetScale;

        m_isDoing = false;

        if (_isOpen == false)
            CloseAsync().Forget();
    }
}
