using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PopupUserInfoComponent : BasePopupComponent, IValidatable
{
    PopupUserInfoComponent() : base(PopupType.UserInfo) { }

    public StatusType statusType;

    public override void OpenPopup(params object[] _args)
    {
        statusType = StatusType.Wait;
        string uid = (string)_args[0];

        SetUserInfoAsync(uid).Forget();
    }

    public bool EscapeClose()
    {
        if (m_element.infDesc.isFocused == true)
        {
            return false;
        }

        statusType = StatusType.Cancel;
        Close();

        return false;
    }

    async UniTask SetUserInfoAsync(string _uid)
    {
        m_element.panel.gameObject.SetActive(false);
        await UniTask.Yield();

        Utils.SetActivePunch(m_element.panel, true);

        m_element.txtNickname.text = _uid;
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
        public TextMeshProUGUI txtNickname;

        public TMP_InputField infDesc;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            txtNickname = _transform.GetComponent<TextMeshProUGUI>("Panel/FrontPanel/Name/txt_name");

            infDesc = _transform.GetComponent<TMP_InputField>("Panel/FrontPanel/inf_desc");
        }
    }
    #endregion VALIDATE

}

public struct UserInfoData
{

}