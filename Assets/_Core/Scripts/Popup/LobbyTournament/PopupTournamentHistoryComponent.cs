using UnityEngine;

public class PopupTournamentHistoryComponent : BasePopupComponent
{
    PopupTournamentHistoryComponent() : base(PopupType.LobbyTournament_History) { }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
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
