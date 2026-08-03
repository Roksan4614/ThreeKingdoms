using UnityEngine;
using UnityEngine.UI;

public class PopupTournamentComponent : BasePopupComponent
{
    PopupTournamentComponent() : base(PopupType.LobbyTournament) { }

    private void Start()
    {
        m_element.imgTemp.enabled = true;
    }

    public override void OpenPopup(params object[] _args)
    {
        Utils.SetActivePunch(m_element.panel, true);
    }

    public override void Close()
        => Utils.SetActivePunch(m_element.panel, false, _callback: base.Close);

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image imgTemp;

        public Transform panel;

        public void Initialize(Transform _transform)
        {
            imgTemp = _transform.GetComponent<Image>();
            panel = _transform.Find("Panel");
        }
    }
    #endregion VALIDATE

}
