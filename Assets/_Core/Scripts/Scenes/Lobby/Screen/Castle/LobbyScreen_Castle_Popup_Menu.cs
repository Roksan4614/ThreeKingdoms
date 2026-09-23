using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Castle_Popup_Menu : MonoBehaviour, IValidatable
{
    public StatusType statusType { get; private set; }

    private void Awake()
    {
        m_element.dimm.onClick.AddListener(() => Close());
        m_element.dimm = null;

        m_element.btnInfo.onClick.AddListener(() => Close(StatusType.Success));
        m_element.btnSetting.onClick.AddListener(() => Close(StatusType.Failed));
        m_element.btnEtc.onClick.AddListener(() => Close(StatusType.Cancel));

        //setlocalization
        {
            m_element.btnInfo.text = TableManager.stringTable.GetString("CASTLE_MENU_INFO_S");
            m_element.btnSetting.text = TableManager.stringTable.GetString("CASTLE_MENU_SETTING_S");

            if( DataManager.option.language == LanguageType.English)
            {
                m_element.btnSetting.TMPText.characterSpacing =
                m_element.btnEtc.TMPText.characterSpacing =
                    m_element.btnInfo.TMPText.characterSpacing = 0;
            }
        }
    }

    public void Open(RectTransform _button, CastleObjectType _type)
    {
        statusType = StatusType.Wait;
        gameObject.SetActive(true);

        m_element.btnInfo.gameObject.SetActive(_type != CastleObjectType.Office);
        m_element.btnEtc.gameObject.SetActive(true);
        switch (_type)
        {
            case CastleObjectType.Office:
            case CastleObjectType.Merchant:
                m_element.btnEtc.text = TableManager.stringTable.GetString($"CASTLE_MENU_{_type.ToString().ToUpper()}_S");
                break;
            default:
                m_element.btnEtc.gameObject.SetActive(false);
                break;
        }
        m_element.btnEtc.transform.parent.ForceRebuildLayout();

        // 위치 조정
        m_element.panel.position = _button.position;
        //var anchorPos = m_element.panel.anchoredPosition;
        //anchorPos.x +=
        //    m_element.panel.rect.width * (anchorPos.x > 0 ? -.9f : .9f);

        var anchorPos = m_element.panel.anchoredPosition;
        anchorPos.x += (_button.sizeDelta.x * 0.5f + m_element.panel.rect.width * 0.6f) * (anchorPos.x > 0 ? -1f : 1f);

        m_element.panel.anchoredPosition = anchorPos;
    }

    public void Close(StatusType _result = StatusType.Invalid)
    {
        statusType = _result;
        gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Button dimm;

        public ButtonHelper btnInfo;
        public ButtonHelper btnSetting;
        public ButtonHelper btnEtc;

        public RectTransform panel;

        public void Initialize(Transform _transform)
        {
            dimm = _transform.GetComponent<Button>("Dimm");
            panel = _transform.GetComponent<RectTransform>("Panel");

            btnInfo = panel.GetComponent<ButtonHelper>("btn_info");
            btnSetting = panel.GetComponent<ButtonHelper>("btn_setting");
            btnEtc = panel.GetComponent<ButtonHelper>("btn_etc");
        }
    }
    #endregion VALIDATE
}
