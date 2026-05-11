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
    }

    public void Open(Transform _button, CastleObjectType _type)
    {
        statusType = StatusType.Wait;
        gameObject.SetActive(true);

        m_element.btnInfo.gameObject.SetActive(_type != CastleObjectType.Office);
        m_element.btnEtc.gameObject.SetActive(true);
        switch (_type)
        {
            case CastleObjectType.Merchant:
                m_element.btnEtc.text = "상 점";
                break;
            case CastleObjectType.Office:
                m_element.btnEtc.text = "미 션";
                break;
            default:
                m_element.btnEtc.gameObject.SetActive(false);
                break;
        }
        m_element.btnEtc.transform.parent.ForceRebuildLayout();

        // 위치 조정
        m_element.panel.position = _button.position;
        var anchorPos = m_element.panel.anchoredPosition;
        anchorPos.x +=
            m_element.panel.rect.width * (anchorPos.x > 0 ? -.9f : .9f);
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
