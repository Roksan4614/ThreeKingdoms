using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BottomComponent : Singleton<BottomComponent>, IValidatable
{
    Dictionary<LobbyScreenType, ScreenData> m_dbScreen = new();

    IEnumerator Start()
    {
        m_dbScreen = m_element.screens.ToDictionary(x => x.type, x => x);

        foreach (var screen in m_dbScreen.Values)
        {
            screen.button.onClick
                .AddListener(() => OnButton(screen.type));

            screen.txtName.text = screen.button.name = screen.type.ToString().ToUpper();
        }

        m_element.panel.ForceRebuildLayout();
        m_element.panel.GetComponent<HorizontalLayoutGroup>().enabled = false;

        yield return null;

        // Text 크기 맞추기
        {
            int minSize = (int)m_dbScreen.Values.Min(x => x.txtName.preferredHeight);
            foreach (var screen in m_dbScreen.Values)
                screen.txtName.fontSizeMax = minSize;
        }

        Signal.instance.CloseLobbyScreen.connectLambda = new(this, _screen => SelectButton(_screen, false));
        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive => gameObject.SetActive(_isActive));
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

        m_dbScreen[_screen].rt.DOScale(_isSelect ? Vector3.one * 1.2f : Vector3.one, 0.1f);

        if (_isSelect)
            m_dbScreen[_screen].rt.parent.SetAsFirstSibling();
    }

    public Transform GetIconScreen(ItemType _itemType)
        => m_dbScreen[_itemType switch
        {
            ItemType.Scroll_Party => LobbyScreenType.Summon,
            _ => LobbyScreenType.Heros
        }].icon;

    #region VALIDATA
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;

    [Serializable]
    public struct ElementData
    {
        public Transform panel;
        public List<ScreenData> screens;
        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");

            screens = new();
            for (int i = 0; i < panel.childCount; i++)
            {
                ScreenData data = new()
                {
                    type = LobbyScreenType.None + 1 + i,
                    button = panel.GetChild(i).GetComponent<Button>()
                };
                data.rt = (RectTransform)data.button.transform;
                data.txtName = data.rt.GetComponent<TextMeshProUGUI>("Panel/txt_name");
                data.icon = data.rt.Find("Panel/Icon");
                screens.Add(data);
            }
        }
    }

    [Serializable]
    public struct ScreenData
    {
        public LobbyScreenType type;
        public Button button;
        public TextMeshProUGUI txtName;
        public RectTransform rt;
        public Transform icon;

        public bool isActive => type > LobbyScreenType.None;
    }
    #endregion
}
