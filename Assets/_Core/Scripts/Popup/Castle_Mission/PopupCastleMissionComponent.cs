using System;
using UnityEngine;

public class PopupCastleMissionComponent : BasePopupComponent
{
    PopupCastleMissionComponent() : base(PopupType.Castle_Mission) { }

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
