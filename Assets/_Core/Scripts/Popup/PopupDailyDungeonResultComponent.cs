using TMPro;
using UnityEngine;

public class PopupDailyDungeonResultComponent : BasePopupComponent
{
    PopupDailyDungeonResultComponent() : base(PopupType.DailyDungeonResult) { }

    public StatusType result { get; private set; } = StatusType.Wait;

    protected override void Awake()
    {
        m_element.btnConfirm.onClick.AddListener(() =>
        {
            result = StatusType.Success;
            Close();
        });
        m_element.btnRetry.onClick.AddListener(() => Close());
    }

    public override void OpenPopup(params object[] _args)
    {

    }

    public override void Close()
        => Utils.SetActivePunch(m_element.panel, false, _callback: () => base.Close());

    #region VALIDATE
    public override void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtResult;
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtPercent;
        public Transform pReward;
        public TextMeshProUGUI txtCount;

        public ButtonHelper btnConfirm;
        public ButtonHelper btnRetry;

        public Transform panel => txtTitle.transform.parent;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");

            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_confirm");
            btnRetry = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_retry");
        }
    }
    #endregion VALIDATE

}
