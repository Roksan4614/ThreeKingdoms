using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PopupUserInfoMyComponent : PopupUserInfoComponent
{
    PopupUserInfoMyComponent() : base(PopupType.UserInfo_My) { }

    private readonly StringBuilder m_stringBuilder = new StringBuilder();

    bool m_isEditMode;

    protected override void Start()
    {
        base.Start();

        m_elementMy.btnEdit.onClick.AddListener(() => OnButton_Edit(true));

        m_element.infNickname.onSelect.AddListener(_txt => m_elementMy.objsEdits[0].gameObject.SetActive(false));
        m_element.infNickname.onValueChanged.AddListener(_txt =>
        {
            if (string.IsNullOrEmpty(_txt)) return;

            m_stringBuilder.Clear();
            bool hasInvalidChar = false;

            for (int i = 0; i < _txt.Length; i++)
            {
                char c = _txt[i];

                // 문장부호(Punctuation), 기호(Symbol), 공백(WhiteSpace) 제거
                if (char.IsPunctuation(c) || char.IsSymbol(c) || char.IsWhiteSpace(c))
                {
                    hasInvalidChar = true;
                    continue;
                }

                m_stringBuilder.Append(c);
            }

            if (hasInvalidChar)
                m_element.infNickname.text = m_stringBuilder.ToString();
        });
        m_element.infNickname.onEndEdit.AddListener(_txt => m_elementMy.objsEdits[0].gameObject.SetActive(true));

        m_element.infDesc.onSelect.AddListener(_txt
            => m_elementMy.objsEdits[1].gameObject.SetActive(false));
        m_element.infDesc.onEndEdit.AddListener(_txt =>
        {
            m_element.infDesc.text = $"\"{_txt.Trim('"')}\"";
            m_elementMy.objsEdits[1].gameObject.SetActive(true);
        });

        m_element.panel.gameObject.SetActive(false);

        Utils.WaitEscape(this, () =>
        {
            if (m_element.infNickname.isFocused == true ||
                m_element.infDesc.isFocused == true)
            {
                EventSystem.current.SetSelectedGameObject(null);
                ResetUserData();
                return;
            }
            OnButtonClose();
        });
    }

    public override void OpenPopup(params object[] _args)
    {
        ControllerManager.instance.SetSwitch(false);

        SetUserInfoAsync(DataManager.userInfo.userInfoData).Forget();
        m_element.panel.gameObject.SetActive(true);

        m_isEditMode = true;
        OnButton_Edit(false);

        SetRecordData();
    }

    void SetRecordData()
    {

    }
    void OnButton_Edit(bool _isEditButton)
        => OnButtonAsync_Edit(_isEditButton).Forget();
    async UniTask OnButtonAsync_Edit(bool _isEditButton)
    {
        if (_isEditButton && ThreeKingdoms.Client.Server.PrototypeContentNotice.ShowIfServer()) return;
        m_isEditMode = !m_isEditMode;

        foreach (var e in m_elementMy.objsEdits)
            e.gameObject.SetActive(m_isEditMode);

        m_element.infNickname.interactable =
        m_element.infDesc.interactable = m_isEditMode;

        m_element.btnConfirm.text = TableManager.stringTable.GetString($"BUTTON_{(m_isEditMode ? "SAVE" : "CONFIRM")}");
        m_elementMy.btnEdit.text = TableManager.stringTable.GetString($"BUTTON_{(m_isEditMode ? "SAVE" : "EDIT")}");
        m_elementMy.btnEdit.SetDrawSelect(m_isEditMode);

        if (m_isEditMode == true)
            return;

        if (_isEditButton == true)
        {
            //저장해줘야 해
            if (await DataManager.userInfo.API_SetUserData(m_element.infNickname.text, m_element.infDesc.text.Trim('"')) == false)
                return;
        }

        ResetUserData();
    }

    void ResetUserData()
    {
        var userInfo = DataManager.userInfo.userInfoData;
        //원상복구
        m_element.profile.SetProfileData(userInfo.profileIdx, userInfo.profileSkin);
        m_element.infNickname.text = userInfo.nickname;
        m_element.infDesc.text = $"\"{userInfo.desc}\"";
    }

    private void Update()
    {
        if (m_isEditMode && Input.GetKeyDown(KeyCode.Return))
        {
            if (m_element.infNickname.isFocused == true ||
                m_element.infDesc.isFocused == true)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            else
                OnButton_Edit(true);
        }
    }

    protected override void OnButtonClose()
    {
        if (m_isEditMode == true)
            OnButton_Edit(false);
        else
            base.OnButtonClose();
    }

    protected override void OnButtonCopy()
    {
        if (m_isEditMode == true)
            return;

        base.OnButtonCopy();
    }

    public override void Close()
    {
        ControllerManager.instance.SetSwitch(true);
        base.Close();
    }

    #region VALIDATE
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_elementMy.Initialize(transform);
    }

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData_My m_elementMy;

    [System.Serializable]
    struct ElementData_My
    {
        public ButtonHelper btnEdit;
        public List<GameObject> objsEdits;

        public ScrollRect scrollRecord;
        public Button btnProfile;

        public void Initialize(Transform _transform)
        {
            btnEdit = _transform.GetComponent<ButtonHelper>("Panel/btn_edit");

            objsEdits = new();
            objsEdits.Add(_transform.Find("Panel/FrontPanel/inf_nickname/Edit").gameObject);
            objsEdits.Add(_transform.Find("Panel/FrontPanel/inf_desc/Edit").gameObject);
            objsEdits.Add(_transform.Find("Panel/FrontPanel/Slot_Profile/Edit").gameObject);

            scrollRecord = _transform.GetComponent<ScrollRect>("Panel/Record");
            btnProfile = _transform.GetComponent<Button>("Panel/FrontPanel/Slot_Profile");
        }
    }
    #endregion VALIDATE

}
