using UnityEngine;

public class PopupBuyComponent : BasePopupComponent
{
    protected PopupBuyComponent() : base(PopupType.Buy) { }


    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public void Initialize(Transform _transform)
        {
        }
    }
    #endregion VALIDATE

}
