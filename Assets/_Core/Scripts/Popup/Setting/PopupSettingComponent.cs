using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupSettingComponent : BasePopupComponent
{
    enum ButtonType
    {
        NONE = -1,
        Terms,
        Policy,
        AS,
        Delete_Account,
    }

    PopupSettingComponent() : base(PopupType.Setting) { }

    private void Start()
    {
        List<OptionType> ot = new() {
            OptionType.MUTE_SOUND_BGM,
            OptionType.MUTE_SOUND_SFX,
            OptionType.OFF_HAPTIC,
            OptionType.OFF_SCREEN_SHAKE,
        };

        List<string> keys = new() { "BGM", "SFX", "HAPTIC", "SHAKE" };
        for (int i = 0; i < ot.Count; i++)
        {
            var optionType = ot[i];
            var toggle = m_element.toggle[i];

            // 옵션에는 MUTE 나 꺼진걸 true로 간주한다.
            toggle.isOn = DataManager.option.IsOn(optionType) == false;
            toggle.onClick.AddListener(() =>
            {
                DataManager.option.SetOption(optionType, toggle.isOn);
                toggle.isOn = toggle.isOn == false;
            });
            toggle.text = TableManager.stringTable.GetString($"UI_SET_GAME_{keys[i]}");
        }

        m_element.btnConnectAccount.onClick.AddListener(() => PopupManager.instance.AlertShow_Table("NOT_YET_READY"));
        m_element.btnConnectAccount.text = TableManager.stringTable.GetString("BUTTON_ADMIN_CONNECT");

        int idx = 0;
        foreach (var b in m_element.buttons)
        {
            b.text = TableManager.stringTable.GetString($"UI_SET_{(ButtonType.NONE + 1 + idx).ToString().ToUpper()}");
            b.onClick.AddListener(() => PopupManager.instance.AlertShow_Table("NOT_YET_READY"));
            idx++;
        }

        Utils.WaitEscape(this, () =>
        {
            if (m_element.popupLanguage.gameObject.activeSelf == true)
            {
                m_element.popupLanguage.gameObject.SetActive(false);
                return;
            }
            Close();
        }, _isMenuPopup: true);

        m_element.popupLanguage.gameObject.SetActive(false);
        m_element.btnLanguage.onClick.AddListener(() => OpenPopupLanguageAsync().Forget());
        //setlocalization
        {
            m_element.txtLanguageTitle.text = TableManager.stringTable.GetString("UI_SET_GAME_LANG");
            m_element.btnLanguage.text = TableManager.stringTable.GetString("UI_SET_LANGUAGE");
            m_element.btnLogout.text = TableManager.stringTable.GetString("UI_SET_LOGOUT");

            transform.SetTextTable("Panel/txt_title", "UI_SET_TITLE");
            transform.SetTextTable("Panel/Content/txt_setting", "UI_SET_GAME_TITLE");
            transform.SetTextTable("Panel/Content/txt_account", "UI_SET_ADMIN_CONNECT");
            transform.SetTextTable("Panel/Content/Google/Text", "UI_SET_ADMIN_GOOGLE");
        }
    }

    public override void OpenPopup(params object[] _args)
    {
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);
    }

    public override void Close()
    {
        Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
    }

    async UniTask OpenPopupLanguageAsync()
    {
        if (DataManager.instance.isLobby == false)
        {
            PopupManager.instance.AlertShow_Table("INVALID_CHANGE_LANGUAGE");
            return;
        }

        m_element.rtArrowLanguage.rotation = Quaternion.Euler(0, 0, 180);
        m_element.popupLanguage.gameObject.SetActive(true);

        await UniTask.WaitUntil(() => m_element.popupLanguage.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);

        m_element.rtArrowLanguage.rotation = Quaternion.Euler(0, 0, 0);
    }


    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public ToggleHelper[] toggle;

        public ButtonHelper btnConnectAccount;
        public ButtonHelper btnLogout;
        public ButtonHelper[] buttons;

        public TextMeshProUGUI txtLanguageTitle;
        public ButtonHelper btnLanguage;
        public RectTransform rtArrowLanguage;

        public PopupSetting_Language popupLanguage;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            toggle = _transform.Find("Panel/Content").GetComponentsInChildren<ToggleHelper>();

            btnConnectAccount = _transform.GetComponent<ButtonHelper>("Panel/Content/Google/btn_connect_account");
            buttons = _transform.Find("Panel/Content/Button").GetComponentsInChildren<ButtonHelper>();
            btnLogout = _transform.GetComponent<ButtonHelper>("Panel/btn_logout");

            txtLanguageTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/Content/Language/Text");
            btnLanguage = _transform.GetComponent<ButtonHelper>("Panel/Content/Language/btn_open");
            rtArrowLanguage = (RectTransform)btnLanguage.transform.Find("img_arrow");

            popupLanguage = _transform.GetComponent<PopupSetting_Language>("Popup_Language");
        }

        public Transform panel => txtTitle.transform.parent;
    }
    #endregion VALIDATE

}
