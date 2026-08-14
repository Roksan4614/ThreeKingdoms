using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournamentHistory_Popup_Revenge : MonoBehaviour, IValidatable
{
    TournamentHistoryData m_historyData;

    private void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(Close);
        transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);

        transform.GetComponent<Button>("Panel/Button/btn_start").onClick.AddListener(() => OnButtonAsync_Start().Forget());
        m_element.btnAD.onClick.AddListener(() => OnButtonAsync_AD().Forget());
    }

    public bool CloseEscape()
    {
        if (m_isDoing == true)
            return false;

        if (PopupManager.instance.IsOpenPopup(PopupType.Hero_HeroInfo))
            return false;

        if (gameObject.activeSelf == true)
        {
            Close();
            return false;
        }

        return true;
    }

    public async UniTask OpenAsync(TournamentHistoryData _historyData)
    {
        m_historyData = _historyData;

        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);

        m_element.slot.SetHistoryData(_historyData, null);
        m_element.userInfo.OpenAsync(_historyData.uid, _historyData.batchData).Forget();

        m_element.txtPointWin.text = $"승리_시_<size=120%>+{_historyData.resultPoint * -1}p</size>";
        m_element.txtPointLose.text = $"패배_시_<size=120%>{(int)(_historyData.resultPoint * UnityEngine.Random.Range(0.5f, 0.9f))}p</size>";

        SetPlayCount();

        await UniTask.NextFrame();
    }

    void Close()
    {
        //gameObject.SetActive(false);
        Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
    }

    bool m_isDoing;
    async UniTask OnButtonAsync_Start()
    {
        if (m_element.slot.isTimeourRevenge == true)
        {
            PopupManager.instance.AlertShow("시간이_초과_되었습니다.");
            return;
        }

        if (m_isDoing == true)
            return;

        m_isDoing = true;
        if (TournamentWorker.data.countPlay <= 0)
        {
            PopupManager.instance.AlertShow("플레이_가능_횟수가_초과되었습니다.");

            m_isDoing = false;
            return;
        }

        TournamentWorker.instance.EnterBattleAsync(m_historyData.uid, m_historyData.index).Forget();

        m_isDoing = false;
    }

    async UniTask OnButtonAsync_AD()
    {
        if (m_isDoing == true)
            return;

        m_isDoing = true;
        if (await TournamentWorker.instance.ShowAdsAsync() == true)
            SetPlayCount();

        m_isDoing = false;
    }

    void SetPlayCount()
    {
        m_element.txtPlayCount.text = $"일일_입장_가능_횟수: {TournamentWorker.data.countPlay}";
        m_element.btnAD.text = $"{TournamentWorker.data.countAD}/3";

        PopupManager.instance.GetPopup<PopupTournamentComponent>(PopupType.LobbyTournament).SetPlayCount();
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;

        public PopupTournamentHistory_Slot slot;
        public PopupTournament_UserInfo userInfo;

        public TextMeshProUGUI txtPointWin;
        public TextMeshProUGUI txtPointLose;
        public TextMeshProUGUI txtPlayCount;

        public ButtonHelper btnAD;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");

            slot = _transform.GetComponent<PopupTournamentHistory_Slot>("Panel/Slot");
            userInfo = _transform.GetComponent<PopupTournament_UserInfo>("Panel/UserInfo");

            txtPointWin = _transform.GetComponent<TextMeshProUGUI>("Panel/Result/txt_point_win");
            txtPointLose = _transform.GetComponent<TextMeshProUGUI>("Panel/Result/txt_point_lose");
            txtPlayCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_count");

            btnAD = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_ad");
        }

        public Transform panel => txtTitle.transform.parent;
    }
    #endregion VALIDATE

}
