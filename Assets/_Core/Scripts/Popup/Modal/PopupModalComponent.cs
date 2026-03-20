using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupModalComponent : BasePopupComponent
{
    PopupModalComponent() : base(PopupType.Modal) { }

    public StatusType statusType { get; private set; } = StatusType.Wait;

    private void Start()
    {
        m_element.btnConfirm.onClick.AddListener(() => { statusType = StatusType.Success; Close(); });
        m_element.btnCancel.onClick.AddListener(Close);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            m_element.btnConfirm.onClick.Invoke();
    }

    public override void OpenPopup(params object[] _args)
    {
        m_element.fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        ModalPopupData popupData = (ModalPopupData)_args[0];

        m_element.txtContent.text = popupData.content;
        m_element.btnConfirm.text = popupData.confirm ?? "_확인";
        m_element.btnCancel.text = popupData.cancel ?? "_취소";

        m_element.rt.ForceRebuildLayout();

        var size = m_element.rt.sizeDelta;
        if (Screen.width * 0.8f < size.x)
        {
            m_element.fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            size.x = Screen.width * 0.8f;
            m_element.rt.sizeDelta = size;

            m_element.rt.ForceRebuildLayout();
        }

        m_element.rt.position = CameraManager.instance.GetMousePosition();

        var hw = m_element.rt.rect.width * 0.5f;
        var anchPos = m_element.rt.anchoredPosition;
        if (anchPos.x < Screen.width * -0.5f + hw)
            anchPos.x = Screen.width * -0.5f + hw;
        else if (anchPos.x > Screen.width * 0.5f - hw)
            anchPos.x = Screen.width * 0.5f - hw;

        if (anchPos.y > Screen.safeArea.height * 0.5f - m_element.rt.rect.height)
            anchPos.y = Screen.safeArea.height * 0.5f - m_element.rt.rect.height;

        m_element.rt.anchoredPosition = anchPos;
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform rt;

        public TextMeshProUGUI txtContent;

        public ButtonHelper btnConfirm;
        public ButtonHelper btnCancel;

        public ContentSizeFitter fitter;

        public void Initialize(Transform _transform)
        {
            rt = (RectTransform)_transform.Find("Panel");

            txtContent = rt.GetComponent<TextMeshProUGUI>("txt_content");
            btnConfirm = rt.GetComponent<ButtonHelper>("Buttons/btn_confirm");
            btnCancel = rt.GetComponent<ButtonHelper>("Buttons/btn_cancel");

            fitter = rt.GetComponent<ContentSizeFitter>();
        }
    }
    #endregion VALIDATA


    public struct ModalPopupData
    {
        public string content;
        public string confirm;
        public string cancel;
    }
}
