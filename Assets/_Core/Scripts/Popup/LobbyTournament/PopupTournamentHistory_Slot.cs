using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupTournamentHistory_Slot : MonoBehaviour, IValidatable
{
    public void SetHistoryData(TournamentHistoryData _historyData, UnityAction<TournamentHistoryData> _callback)
    {
        bool isRevenge = _historyData.isAvailRevenge;

        m_element.objAttack.SetActive(_historyData.isAttack);
        m_element.objDefence.SetActive(_historyData.isAttack == false);
        m_element.txtType.text = _historyData.isAttack ? "_공격_" : "_방어_";

        m_element.txtResult.text = $"{(_historyData.isWin ? "WIN" : "LOSE")}\n<color=#555555><size=80%>({(_historyData.isWin ? "+" : "")}{_historyData.rewardPoint}p)</size></color>";
        if (ColorUtility.TryParseHtmlString($"#{(_historyData.isWin ? Palette.htmlString_Up : Palette.htmlString_Down)}", out Color clr))
            m_element.txtResult.color = clr;

        m_element.profile.SetProfileData(_historyData.indexProfile, _historyData.skin);

        m_element.objRevenge.gameObject.SetActive(isRevenge);

        m_element.txtNickname.text = _historyData.nickname;
        m_element.txtNickname.rt.SetAnchoredPositionY(isRevenge ? 33 : 0);

        if (isRevenge == true)
        {
            m_element.txtRevenge.text = $"복수 <color=#eeeeee><size=80%> (+{_historyData.revengePoint}p)</size></color>";
            m_element.objRevenge.transform.ForceRebuildLayout();
        }

        m_element.button.onClick.RemoveAllListeners();
        m_element.button.onClick.AddListener(() => _callback?.Invoke(_historyData));
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image imgPanel;
        public Button button;

        public GameObject objAttack;
        public GameObject objDefence;

        public TextMeshProUGUI txtType;
        public TextMeshProUGUI txtResult;
        public ProfileIconCompoent profile;
        public TextPanelHelper txtNickname;
        public TextMeshProUGUI txtRevenge;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<Button>();

            imgPanel = _transform.GetComponent<Image>("Panel");
            objAttack = _transform.Find("Panel/Attack").gameObject;
            objDefence = _transform.Find("Panel/Defence").gameObject;

            txtType = _transform.GetComponent<TextMeshProUGUI>("Panel/Type/Text");
            txtResult = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_result");
            txtNickname = _transform.GetComponent<TextPanelHelper>("Panel/txt_nickname");
            txtRevenge = _transform.GetComponent<TextMeshProUGUI>("Panel/Revenge/Text");

            profile = _transform.GetComponent<ProfileIconCompoent>("Panel/Slot_Profile");
        }

        public GameObject objRevenge => txtRevenge.transform.parent.gameObject;
    }
    #endregion VALIDATE

}
