using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BottomComponent : MonoBehaviour
{
    Transform m_panel;

    Dictionary<LobbyScreenType, RectTransform> m_dicScreen = new();

    List<Text> texts = new List<Text>();
    IEnumerator Start()
    {
        m_panel = transform.Find("Panel");
        for (int i = 0; i < m_panel.childCount; i++)
        {
            var type = LobbyScreenType.None + 1 + i;

            var button = m_panel.GetChild((int)type).GetComponent<Button>();
            button.GetComponent<Button>().onClick
                .AddListener(() => OnButton(type));

            button.name = type.ToString().ToUpper();
            texts.Add(button.transform.SetText("Panel/txt_name", button.name).GetComponent<Text>());

            m_dicScreen.Add(type, (RectTransform)button.transform.Find("Panel"));
        }

        m_panel.ForceRebuildLayout();
        m_panel.GetComponent<HorizontalLayoutGroup>().enabled = false;

        yield return null;

        // Text 크기 맞추기
        {
            int minSize = (int)texts.Min(x => x.preferredHeight);
            if (minSize < texts[0].fontSize)
            {
                foreach (var t in texts)
                {
                    t.resizeTextForBestFit = false;
                    t.fontSize = minSize;
                }
            }
        }

        Signal.instance.CloseLobbyScreen.connectLambda = new(this, _screen => SelectButton(_screen, false));
    }

    void OnButton(LobbyScreenType _screen)
    {
        SelectButton(LobbyScreenManager.instance.curScreen, false);

        var screen = LobbyScreenManager.instance.OpenScreen(_screen);
        if (screen != null)
        {
            SelectButton(_screen, true);
        }
    }

    void SelectButton(LobbyScreenType _screen, bool _isSelect)
    {
        if (_screen == LobbyScreenType.None)
            return;

        m_dicScreen[_screen].DOScale(_isSelect ? Vector3.one * 1.2f : Vector3.one, 0.1f);

        if (_isSelect)
            m_dicScreen[_screen].parent.SetAsFirstSibling();
    }
}
