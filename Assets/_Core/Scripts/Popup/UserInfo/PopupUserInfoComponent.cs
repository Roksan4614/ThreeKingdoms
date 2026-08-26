using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupUserInfoComponent : BasePopupComponent, IValidatable
{
    PopupUserInfoComponent() : base(PopupType.UserInfo) { }

    public StatusType statusType;

    protected override void Awake()
    {
        base.Awake();

        m_element.btnConfirm.onClick.AddListener(Close);
    }

    public override void OpenPopup(params object[] _args)
    {
        statusType = StatusType.Wait;
        var userInfo = (UserInfoData)_args[0];

        SetUserInfoAsync(userInfo).Forget();
    }

    async UniTask SetUserInfoAsync(UserInfoData _userInfo)
    {
        m_element.panel.gameObject.SetActive(false);
        await UniTask.Yield();

        Utils.SetActivePunch(m_element.panel, true);

        m_element.profile.SetProfileData(_userInfo.profileIdx, _userInfo.batchHeroes[0].key);
        m_element.txtNickname.text = _userInfo.nickname;
        m_element.txtInfo.text = $"UID : {_userInfo.uid}\n¼Ò¼Ó_: {_userInfo.regionName}";
        m_element.txtDesc.text = $"\"{(_userInfo.descript ?? "......")}\"";

        for (int i = 0; i < m_element.slotHeroes.Length; i++)
            m_element.slotHeroes[i].SetHeroData_UserInfoAsync(_userInfo.batchHeroes[i]).Forget();
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

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Transform panel;

        public ProfileIconCompoent profile;
        public TextMeshProUGUI txtNickname;
        public TextMeshProUGUI txtInfo;
        public TextMeshProUGUI txtDesc;

        public HeroIconComponent_UserInfo[] slotHeroes;

        public ButtonHelper btnConfirm;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");

            profile = _transform.GetComponent<ProfileIconCompoent>("Panel/FrontPanel/Slot_Profile");
            txtNickname = _transform.GetComponent<TextMeshProUGUI>("Panel/FrontPanel/Name/txt_name");
            txtInfo = _transform.GetComponent<TextMeshProUGUI>("Panel/FrontPanel/txt_info");
            txtDesc = _transform.GetComponent<TextMeshProUGUI>("Panel/FrontPanel/txt_desc");

            slotHeroes = _transform.Find("Panel/Batch/Layout").GetComponentsInChildren<HeroIconComponent_UserInfo>();

            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/btn_confirm");
        }
    }
    #endregion VALIDATE

}

public class UserInfoData
{
    public int uid;
    public string nickname;
    public RegionType region;
    public string descript;
    public int profileIdx;

    public List<HeroInfoData> batchHeroes;
    public List<string> treasures;

    public string regionName => TableManager.stringTable.GetRegionType(region, true);
}
