using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupSettingComponent : BasePopupComponent
{
    enum ButtonType
    {
        Terms,
        Information,
        AS,
        DeleteAccount,
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
        }

        m_element.btnConnectAccount.onClick.AddListener(() => PopupManager.instance.AlertShow_Table("NOT_YET_READY"));

        foreach(var b in m_element.buttons)
            b.onClick.AddListener(() => PopupManager.instance.AlertShow_Table("NOT_YET_READY"));

        Utils.WaitEscape(this, () =>
        {
            Close();
        }, _isMenuPopup: true);
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
        public ButtonHelper[] buttons;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            toggle = _transform.Find("Panel/Content").GetComponentsInChildren<ToggleHelper>();

            btnConnectAccount = _transform.GetComponent<ButtonHelper>("Panel/Content/Google/btn_connect_account");
            buttons = _transform.Find("Panel/Content/Button").GetComponentsInChildren<ButtonHelper>();
        }

        public Transform panel => txtTitle.transform.parent;
    }
    #endregion VALIDATE

}
