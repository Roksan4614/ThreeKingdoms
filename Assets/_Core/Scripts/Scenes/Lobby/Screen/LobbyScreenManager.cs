using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

public enum LobbyScreenType
{
    None = -1,

    Hero,
    Castle,
    Boss,
    Shop,
    Summon,

    MAX
}
public class LobbyScreenManager : Singleton<LobbyScreenManager>
{
    Dictionary<LobbyScreenType, LobbyScreen_Base> m_dicScreen = new();

    LobbyScreenType m_curScreen = LobbyScreenType.None;
    public LobbyScreenType curScreen => m_curScreen;
    public bool isLock { get; set; } = false;

    //public T GetScreen<T>(LobbyScreenType _type) where T: LobbyScreen_Base => m_dicScreen[_type] as T;
    public LobbyScreen_Hero GetScreenHero() => m_dicScreen[LobbyScreenType.Hero] as LobbyScreen_Hero;
    public LobbyScreen_Summon GetScreenSummon() => m_dicScreen[LobbyScreenType.Summon] as LobbyScreen_Summon;

    protected override void OnAwake()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }

    private void Start()
    {
        SetActiveDimm(false, false);

        Signal.instance.CloseLobbyScreen.connect = CloseScreen;
    }

    // Ω∫≈©∏∞ø°º≠ ¥›±‚∏¶ ¥≠∑Øº≠ ¥›¿ª ∂ß
    public void CloseScreen(LobbyScreenType _screenType)
    {
        if (_screenType == m_curScreen)
        {
            m_dicScreen[_screenType].Close();
            SetActiveDimm(false);
        }

        m_curScreen = LobbyScreenType.None;

        ControllerManager.instance.isSwitch = true;
    }

    public async UniTask OpenScreenAsync(LobbyScreenType _screenType, UnityAction<LobbyScreen_Base> _callback)
    {
        ControllerManager.instance.isSwitch = true;

        if (m_doing_ActiveDimm == true)
        {
            _callback(null);
            return;
        }

        if (m_dicScreen.ContainsKey(_screenType) == false)
        {
            var screen = await AddressableManager.instance.GetLobbyScreen(_screenType);
            var item = Instantiate(screen, transform).GetComponent<LobbyScreen_Base>();
            item.name = _screenType.ToString();

            item.Initilize(_screenType);
            m_dicScreen.Add(_screenType, item);
        }

        if (m_curScreen == _screenType ||
            m_dicScreen.ContainsKey(_screenType) == false ||
            m_dicScreen[_screenType] == null)
        {
            if (m_curScreen > LobbyScreenType.None)
                CloseScreen(m_curScreen);
            {
                _callback(null);
                return;
            }
        }

        if (m_curScreen == LobbyScreenType.None)
        {
            // BOSS ≥≠ µı æ»±Ú∞≈æﬂ
            if (_screenType != LobbyScreenType.Boss)
                SetActiveDimm(true);
        }
        else if (m_dicScreen[m_curScreen].isOpenned)
            m_dicScreen[m_curScreen].Close(false);

        // ∫∏Ω∫¿œ ∂© µı √≥∏Æ∞° ¡ª ¥ﬁ∂Û¡Æ
        {
            if (m_curScreen == LobbyScreenType.Boss)
                SetActiveDimm(true);
            else if (_screenType == LobbyScreenType.Boss)
                SetActiveDimm(false, false);
        }

        m_dicScreen[_screenType].Open(m_curScreen);
        m_curScreen = _screenType;

        ControllerManager.instance.isSwitch = false;
        _callback(m_dicScreen[_screenType]);
    }

    bool m_doing_ActiveDimm;
    void SetActiveDimm(bool _isActive, bool _isTween = true)
    {
        var targetAlpha = _isActive ? 1 : 0;

        var imgDimm = GetComponent<Image>();
        if (imgDimm.color.a == targetAlpha)
            return;

        var c = imgDimm.color;

        if (_isTween)
        {
            m_doing_ActiveDimm = true;
            c.a = _isActive ? 0 : 1;

            var duration = 0.1f;
            imgDimm.DOFade(targetAlpha, duration).OnComplete(() => m_doing_ActiveDimm = false);
        }
        else
        {
            c.a = _isActive ? 1 : 0;
        }

        imgDimm.color = c;
    }
}
