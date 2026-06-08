using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyBossRaid_PopupRanking : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        m_element.imgPanel.enabled = true;

        m_element.baseRanker.transform.SetParent(m_element.scroll.viewport);
        m_element.baseRanker.gameObject.SetActive(false);

        m_element.btnClose.onClick.AddListener(OnButton_Close);
    }

    public void OpenPopup()
    {
        Utils.SetActivePunch(transform, true);
    }

    void OnButton_Close()
    {
        Utils.SetActivePunch(transform, false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image imgPanel;

        public Button btnClose;
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtPoint;

        public ButtonHelper[] tabs;

        public ScrollRect scroll;

        public PopupLobbyBossRaid_PopupRanking_PodiumItem[] podiums;
        public PopupLobbyBossRaid_PopupRanking_Item baseRanker;
        public PopupLobbyBossRaid_PopupRanking_Item myRankInfo;

        public void Initialize(Transform _transform)
        {
            imgPanel = _transform.GetComponent<Image>();

            btnClose = _transform.GetComponent<Button>("Top/btn_back");
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Top/txt_title");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("txt_point");

            tabs = _transform.Find("Tab").GetComponentsInChildren<ButtonHelper>();

            scroll = _transform.GetComponent<ScrollRect>("Scroll");
            podiums = _transform.Find("Podium").GetComponentsInChildren<PopupLobbyBossRaid_PopupRanking_PodiumItem>();
            baseRanker = scroll.content.GetChild(0).GetComponent<PopupLobbyBossRaid_PopupRanking_Item>();
            myRankInfo = scroll.viewport.GetComponent<PopupLobbyBossRaid_PopupRanking_Item>("MyInfo");
        }
    }
    #endregion VALIDATE

}
