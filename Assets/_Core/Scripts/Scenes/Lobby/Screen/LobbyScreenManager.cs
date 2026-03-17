using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum LobbyScreenType
{
    None = -1,

    Heros,
    Summon,
    Relic,
    Shop,
    Castle,

    MAX
}
public class LobbyScreenManager : Singleton<LobbyScreenManager>
{
    Dictionary<LobbyScreenType, LobbyScreen_Base> m_dicScreen = new();

    LobbyScreenType m_curScreen = LobbyScreenType.None;
    public LobbyScreenType curScreen => m_curScreen;
    public bool isLock { get; set; } = false;

    public LobbyScreen_Summon GetScreenSummon() => m_dicScreen[LobbyScreenType.Summon] as LobbyScreen_Summon;

    protected override void OnAwake()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }

    private void Start()
    {
        List<string> screens = new() {
            "Screen_Hero",
            "Screen_Summon",
        };

        for (int i = 0; i < screens.Count; i++)
        {
            var screen = transform.GetComponent<LobbyScreen_Base>(screens[i]);
            var type = LobbyScreenType.None + i + 1;
            screen.Initilize(type);

            m_dicScreen.Add(type, screen);
        }

        SetActiveDimm(false, false);

        Signal.instance.CloseLobbyScreen.connect = CloseScreen;
    }

    // 스크린에서 닫기를 눌러서 닫을 때
    public void CloseScreen(LobbyScreenType _nextScreen)
    {
        m_dicScreen[_nextScreen].Close();
        SetActiveDimm(false, true);

        m_curScreen = LobbyScreenType.None;

        ControllerManager.instance.isSwitch = true;
    }

    public LobbyScreen_Base OpenScreen(LobbyScreenType _screen)
    {
        ControllerManager.instance.isSwitch = true;

        if (m_doing_ActiveDimm == true)
            return null;

        if (m_curScreen == _screen || m_dicScreen.ContainsKey(_screen) == false)
        {
            if (m_curScreen > LobbyScreenType.None)
                CloseScreen(m_curScreen);
            return null;
        }

        if (m_curScreen == LobbyScreenType.None)
            SetActiveDimm(true, true);
        else if (m_dicScreen[m_curScreen].isOpenned)
            m_dicScreen[m_curScreen].Close(false);

        m_dicScreen[_screen].Open(m_curScreen);
        m_curScreen = _screen;

        ControllerManager.instance.isSwitch = false;
        return m_dicScreen[_screen];
    }

    bool m_doing_ActiveDimm;
    void SetActiveDimm(bool _isActive, bool _isTween)
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

            var duration = 0.15f;
            imgDimm.DOFade(targetAlpha, duration).OnComplete(() => m_doing_ActiveDimm = false);
        }
        else
        {
            c.a = _isActive ? 1 : 0;
        }

        imgDimm.color = c;
    }
}
