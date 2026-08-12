using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournamentHistoryComponent : BasePopupComponent
{
    PopupTournamentHistoryComponent() : base(PopupType.LobbyTournament_History) { }

    private void Start() => StartAsync().Forget();

    async UniTask StartAsync()
    {
        for (int i = 0; i < m_element.parentPopup.childCount; i++)
            m_element.parentPopup.GetChild(i).gameObject.SetActive(false);

        m_element.panel.gameObject.SetActive(false);
        var historyData = await TournamentWorker.instance.API_LoadHistoryData();

        Utils.SetActivePunch(m_element.panel, true);

        m_element.scroll.Initialize<PopupTournamentHistory_Slot>(historyData.Count,
            (_slot, _idx) => _slot.SetHistoryData(historyData[_idx], OnButton_Revenge));

        transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);
        var btnConfirm = transform.GetComponent<ButtonHelper>("Panel/btn_confirm");
        btnConfirm.onClick.AddListener(Close);
    }

    public bool CloseEscape()
    {
        if (gameObject.activeSelf == true)
        {
            if (m_element.popupRevenge.CloseEscape() &&
                m_element.popupUserInfo.CloseEscape())
            {
                Close();
            }

            return false;
        }

        return true;
    }

    public void OpenPopup_Rebirth(params object[] _args)
    {
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);
        m_element.scroll.content.anchoredPosition = Vector2.zero;
    }

    void OnButton_Revenge(TournamentHistoryData _historyData)
    {
        if (_historyData.isAvailRevenge)
            m_element.popupRevenge.OpenAsync(_historyData).Forget();
        else
            m_element.popupUserInfo.OpenAsync(_historyData.uid).Forget();
    }

    public override void Close()
        => Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public LoopScrollHelper scroll;

        public PopupTournament_UserInfo popupUserInfo;
        public PopupTournamentHistory_Popup_Revenge popupRevenge;

        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<LoopScrollHelper>("Panel/Scroll");

            popupUserInfo = _transform.GetComponent<PopupTournament_UserInfo>("Popup/UserInfo");
            popupRevenge = _transform.GetComponent<PopupTournamentHistory_Popup_Revenge>("Popup/Revenge");
        }

        public Transform panel => scroll.transform.parent;
        public Transform parentPopup => popupRevenge.transform.parent;
    }
    #endregion VALIDATE

}
