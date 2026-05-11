using System;
using System.Threading;
using UnityEngine;

public class PopupCastleMissionComponent : BasePopupComponent
{
    PopupCastleMissionComponent() : base(PopupType.Castle_Mission) { }

    CancellationTokenSource m_cts;

    private void Start()
    {
        m_cts = new();
        Utils.WaitEscape(this, () => Close(), _token: m_cts.Token);
    }

    protected override void OnClosePopup()
    {
        ReleaseCTS();
    }

    void ReleaseCTS()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public void Initialize(Transform _transform)
        {

        }
    }
    #endregion VALIDATE
}
