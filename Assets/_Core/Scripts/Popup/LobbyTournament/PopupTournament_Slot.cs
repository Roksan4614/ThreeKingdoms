using TMPro;
using UnityEngine;

public class PopupTournament_Slot : MonoBehaviour, IValidatable
{
    public void ResetData()
    {
        m_element.profile.SetActivePanel(false);

        m_element.txtNickname.text = "";
        m_element.txtPower.text = "";
        m_element.txtPoint.text = "";
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ProfileIconCompoent profile;

        public TextMeshProUGUI txtNickname;
        public TextMeshProUGUI txtPoint;
        public TextMeshProUGUI txtPower;

        public ButtonHelper btnConfirm;

        public void Initialize(Transform _transform)
        {
            profile = _transform.GetComponent<ProfileIconCompoent>("Panel/Slot_Profile");

            txtNickname = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_nickname");
            txtPoint= _transform.GetComponent<TextMeshProUGUI>("Panel/TierPoint/Text");
            txtPower= _transform.GetComponent<TextMeshProUGUI>("Panel/Power/Text");

            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/Power/Text");
        }
    }
    #endregion VALIDATE

}
