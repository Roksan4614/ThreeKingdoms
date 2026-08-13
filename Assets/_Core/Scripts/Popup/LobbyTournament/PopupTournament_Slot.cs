using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupTournament_Slot : MonoBehaviour, IValidatable
{
    UnityAction<TournamentRankerUserData> m_onStart;
    UnityAction<TournamentRankerUserData> m_onOpenInfo;
    TournamentRankerUserData m_rankerUserData;

    private void Awake()
    {
        var btnConfirm = transform.GetComponent<ButtonHelper>("Panel/btn_confirm");
        btnConfirm.onClick.AddListener(() => m_onStart(m_rankerUserData));

        transform.GetComponent<Button>("Panel/btn_info").onClick.AddListener(() => m_onOpenInfo(m_rankerUserData));
    }

    public void ResetData()
    {
        m_element.profile.SetActivePanel(false);

        m_element.txtNickname.text = "";
        m_element.txtPower.text = "";
        m_element.txtPoint.text = "";
    }

    public void SetUserData(TournamentRankerUserData _rankerUserData, UnityAction<TournamentRankerUserData> _onStart, UnityAction<TournamentRankerUserData> _onOpenInfo)
    {
        m_onStart = _onStart;
        m_onOpenInfo = _onOpenInfo;
        m_rankerUserData = _rankerUserData;

        m_element.txtNickname.text = _rankerUserData.info.nickname;
        m_element.txtPower.text = _rankerUserData.info.power.AmountKMBT(_isMBT:true);
        m_element.txtPoint.text = _rankerUserData.info.point.AmountKMBT(_isMBT: true);

        m_element.profile.SetActivePanel(true);
        m_element.profile.SetProfileData(_rankerUserData.info.indexProfile, _rankerUserData.info.skin);
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

        public void Initialize(Transform _transform)
        {
            profile = _transform.GetComponent<ProfileIconCompoent>("Panel/Slot_Profile");

            txtNickname = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_nickname");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/TierPoint/Text");
            txtPower = _transform.GetComponent<TextMeshProUGUI>("Panel/Power/Text");
        }
    }
    #endregion VALIDATE

}
