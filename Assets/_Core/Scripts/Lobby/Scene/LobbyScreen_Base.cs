using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class LobbyScreen_Base : MonoBehaviour
{
    bool m_isDoing = false;

    LobbyScreenType m_screenType;

    private void Awake()
    {
        transform.GetComponent<Button>("Panel/Top/btn_back").onClick.AddListener(
            () => Signal.instance.CloseLobbyScreen.Emit(m_screenType));
    }

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
            gameObject.SetActive(false);
        else
            ActivePanel(false, _isTween);
    }

    void ActivePanel(bool _isOpen, bool _isTween)
    {
        if (m_isDoing == true)
            return;

        m_isDoing = true;

        if (_isOpen == true)
            gameObject.SetActive(true);

        RectTransform panel = transform.GetComponent<RectTransform>("Panel");

        var targetScale = Vector3.one * (_isOpen ? 1 : 0.5f);

        TweenCallback callbackFinished = () =>
        {
            m_isDoing = false;

            if (_isOpen == false)
                gameObject.SetActive(false);
        };

        if (_isTween)
        {
            panel.localScale = Vector3.one * (_isOpen ? 0.5f : 1);
            var duration = 0.15f;

            panel.DOScale(targetScale, duration).SetEase(_isOpen ? Ease.OutBack : Ease.InBack)
                .OnComplete(callbackFinished);
        }
        else
        {
            panel.localScale = targetScale;
            callbackFinished();
        }
    }
}
