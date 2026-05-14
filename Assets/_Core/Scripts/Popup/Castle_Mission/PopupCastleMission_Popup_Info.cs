using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using CastleMissionData = Data_Castle_Mission.CastleMissionData;

public class PopupCastleMission_Popup_Info : BasePopupComponent
{
    PopupCastleMission_Popup_Info() : base(PopupType.NONE) { }

    public StatusType resultType { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        m_element.btnStart.onClick.AddListener(() =>
        {
            resultType = StatusType.Success;
            Close();
        });
    }

    public void Open(CastleMissionData _mission)
    {
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);
        resultType = StatusType.Wait;

    }

    public override void Close()
        => CloseAsync().Forget();

    async UniTask CloseAsync()
    {
        if (resultType == StatusType.Wait)
            resultType = StatusType.Cancel;

        await Utils.SetActivePunchAsync(m_element.panel, false);
        gameObject.SetActive(false);
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtInfo;

        public GaugeHelper gauge;

        public Transform pHeroIcon;

        public ButtonHelper btnStart;

        public void Initialize(Transform _transform)
        {
            btnStart = _transform.GetComponent<ButtonHelper>("Panel/btn_start");
        }

        public Transform panel => btnStart.transform.parent;
    }
    #endregion VALIDATE
}
