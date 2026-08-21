using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournamentResult_Popup_TierUp : MonoBehaviour, IValidatable
{
    bool m_isClose = false;

    private void Awake()
    {
        m_element.dimm.onClick.AddListener(Close);

        // setlocalization
        {
            m_element.txtDesc.text = "빈_곳을_눌러_닫습니다.";
        }
    }

    public void Close() => m_isClose = true;

    public async UniTask OpenAsync(RankerUserData _userData)
    {
        m_isClose = false;

        m_element.dimm.interactable = false;
        m_element.txtDesc.gameObject.SetActive(false);

        gameObject.SetActive(true);

        Utils.SetActivePunch(m_element.panel, true);
        var title = transform.Find("Title");
        Utils.SetActivePunch(title, true);

        m_element.txtTierName.text = $"제_{_userData.tierTournament}_군단장";
        await m_element.tierPoint.SetRankInfoAsync(_userData);

        Utils.SetActivePunch(m_element.txtTierName.transform, true);

        var nextIcon = m_element.tierPoint.GetTierIcon(_userData.tierTournament);
        await Utils.SetActivePunchAsync(nextIcon.transform, true);

        await UniTask.WaitForSeconds(1f);

        m_element.dimm.interactable = true;
        m_element.txtDesc.gameObject.SetActive(true);

        await UniTask.WaitUntil(() => m_isClose == true);

        Utils.SetActivePunch(title, false);
        await Utils.SetActivePunchAsync(m_element.panel, false);
        gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Button dimm;
        public UITierPointHelper tierPoint;
        public TextMeshProUGUI txtTierName;
        public TextMeshProUGUI txtDesc;

        public void Initialize(Transform _transform)
        {
            dimm = _transform.GetComponent<Button>("Dimm");
            tierPoint = _transform.GetComponent<UITierPointHelper>("Panel/HelperTierPoint");
            txtTierName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_tier");
            txtDesc = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_desc");
        }

        public Transform panel => tierPoint.transform.parent;
    }
    #endregion VALIDATE

}
