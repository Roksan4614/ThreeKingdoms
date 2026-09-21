using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupUserInfoComponent : BasePopupComponent, IValidatable
{
    PopupUserInfoComponent() : base(PopupType.UserInfo) { }
    protected PopupUserInfoComponent(PopupType _popupType) : base(_popupType) { }

    public StatusType statusType;
    protected long m_uid;

    protected virtual void Start()
    {
        m_element.btnConfirm.onClick.AddListener(OnButtonClose);
        m_element.btnCopy.onClick.AddListener(OnButtonCopy);

        //setlocalization
        {
            transform.SetTextTable("Panel/txt_title", "UI_USERINFO_TITLE");
            m_element.btnConfirm.text = TableManager.stringTable.GetString("BUTTON_CONFIRM");
            SetLocalization();
        }
    }

    protected virtual void SetLocalization()
    {
        transform.SetTextTable("Panel/Batch/txt_title", "UI_BATCH");
        transform.SetTextTable("Panel/Batch/Layout/Hero/txt_main", "UI_POSITION_MAIN");
        transform.SetTextTable("Panel/Batch/Layout/Hero_2/txt_sub", "UI_POSITION_SUB");

        transform.SetTextTable("Panel/Treasure/txt_title", "UI_TREASURE");
    }

    protected virtual void OnButtonClose()
        => Close();

    protected virtual void OnButtonCopy()
    {
        PopupManager.instance.AlertShow_Table("COPY_COMPLETE");
        Utils.CopyText(m_uid.ToString());
    }

    public override void OpenPopup(params object[] _args)
    {
        statusType = StatusType.Wait;
        var userInfo = (UserInfoData)_args[0];

        m_element.panel.gameObject.SetActive(false);
        Utils.SetActivePunch(m_element.panel, true);

        SetUserInfoAsync(userInfo).Forget();
    }

    protected async UniTask SetUserInfoAsync(UserInfoData _userInfo)
    {
        m_uid = _userInfo.uid;
        m_element.profile.SetProfileData(_userInfo.profileIdx, _userInfo.profileSkin);
        m_element.infNickname.text = _userInfo.nickname;
        m_element.txtInfo.text = $"UID: {m_uid}\n{TableManager.stringTable.GetString("UI_REGION")}: {_userInfo.regionName}";
        m_element.infDesc.text = $"\"{(_userInfo.desc)}\"";
        
        int i = 0;
        for (; i < _userInfo.batchHeroes.Count; i++)
            m_element.slotHeroes[i].SetHeroData_UserInfoAsync(_userInfo.batchHeroes[i]).Forget();

        for (; i < m_element.slotHeroes.Length; i++)
            m_element.slotHeroes[i].SetActivePanel(false);
    }

    public bool EscapeClose()
    {
        statusType = StatusType.Cancel;
        Close();

        return false;
    }

    public override void Close()
    {
        statusType = StatusType.Cancel;
        Utils.SetActivePunch(m_element.panel, false, true, () => base.Close());
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    protected ElementData m_element;

    [System.Serializable]
    protected struct ElementData
    {
        public Transform panel;

        public ProfileIconCompoent profile;
        public TMP_InputField infNickname;
        public TextMeshProUGUI txtInfo;
        public TMP_InputField infDesc;

        public HeroIconComponent_UserInfo[] slotHeroes;

        public ButtonHelper btnConfirm;
        public ButtonHelper btnCopy;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");

            profile = _transform.GetComponent<ProfileIconCompoent>("Panel/FrontPanel/Slot_Profile");
            infNickname = _transform.GetComponent<TMP_InputField>("Panel/FrontPanel/inf_nickname");
            txtInfo = _transform.GetComponent<TextMeshProUGUI>("Panel/FrontPanel/txt_info");
            infDesc = _transform.GetComponent<TMP_InputField>("Panel/FrontPanel/inf_desc");

            slotHeroes = _transform.Find("Panel/Batch/Layout")?.GetComponentsInChildren<HeroIconComponent_UserInfo>();

            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/btn_confirm");
            btnCopy = _transform.GetComponent<ButtonHelper>("Panel/FrontPanel/txt_info/btn_copy");
        }
    }
    #endregion VALIDATE

}

[JsonObject(MemberSerialization.OptIn)]
public class UserInfoData
{
    [JsonProperty] public int uid;
    [JsonProperty] public string nickname;
    [JsonProperty] public RegionType region;
    [JsonProperty] string descript;
    [JsonProperty] public int profileIdx;
    [JsonProperty] public string profileSkin;

    [JsonProperty] public List<HeroInfoData> batchHeroes;
    [JsonProperty] public List<string> treasures;

    public string regionName => TableManager.stringTable.GetRegionType(region, true);
    public string desc
    {
        get => descript ?? $"안녕하세요.\n저는_[{nickname}]_입니다.";
        set => descript = value;
    }
}
