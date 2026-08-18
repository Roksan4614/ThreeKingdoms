using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournament_UserInfo : MonoBehaviour, IValidatable
{
    TournamentBatchData m_batchData;

    [SerializeField] bool isOpenPunch = true;

    private void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(Close);
        transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);

        transform.GetComponent<TextMeshProUGUI>("Panel/txt_title").text = "_상세정보_";

        m_element.treasure.parent.GetComponent<TextMeshProUGUI>("Text").text = "보물_";
        m_element.panel.GetComponent<TextMeshProUGUI>("Batch/Text").text = "상대조합_";

        PPWorker.DeleteKey(PlayerPrefsType.TOURNAMENT_IS_ON_BATCH_INFO);
        m_element.toggleInfo.onClick.AddListener(() => { m_element.toggleInfo.OnButtonToggle(); SetInfo(); });
    }

    public async UniTask OpenAsync(int _uid, TournamentBatchData _batchData = default)
    {
        gameObject.SetActive(true);

        if (isOpenPunch)
            Utils.SetActivePunch(m_element.panel, true);

        m_element.toggleInfo.isOn = PPWorker.GetInt(PlayerPrefsType.TOURNAMENT_IS_ON_BATCH_INFO, false) == 1;

        m_batchData = _batchData.isActive ? _batchData : await TournamentWorker.instance.API_LoadUserInfoData(_uid);
        await m_element.panelBatch.SetBatchDataAsync(m_batchData);

        SetTreasureAsync().Forget();
        SetInfo();

        if (m_element.btnStart != null)
        {
            m_element.btnStart.onClick.RemoveAllListeners();
            m_element.btnStart.onClick.AddListener(() => TournamentWorker.instance.EnterBattleAsync(_uid).Forget());
        }

        await UniTask.WaitUntil(() => gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);
    }

    async UniTask SetTreasureAsync()
    {
        var batchData = await TournamentWorker.instance.API_LoadUserInfoData(m_batchData.uid);

        for (int i = 0; i < batchData.treasure.Count; i++)
        {
            var t = batchData.treasure[i];
            m_element.treasure.GetChild(i).GetComponent<TreasureIconComponent>().SetTreasureDataAsync(t.key).Forget();
        }
    }

    void SetInfo()
    {
        bool isOn = m_element.toggleInfo.isOn;
        var parentInfo = m_element.parentInfo;

        m_element.parentInfo.gameObject.SetActive(isOn);
        long totalPower = 0;
        int i = 0;

        for (; i < m_batchData.heroes.Count; i++)
        {
            var heroInfo = m_batchData.heroes[i];
            var power = heroInfo.power;
            if (isOn == true)
            {
                var slot = i < parentInfo.childCount ? parentInfo.GetChild(i) : Instantiate(m_element.baseInfoSlot, parentInfo).transform;
                slot.gameObject.SetActive(true);
                slot.position = m_element.panelBatch.GetSlot(m_batchData.heroes[i].sortIdx).position;

                slot.GetComponent<TextMeshProUGUI>("Text").text =
                    $"[{heroInfo.gradeName}] +{heroInfo.enchantLevel}\n<color=#ffffff><size=120%>{power.AmountKMBT(_isMBT: true)}</size></color>";

                var button = slot.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OpenHeroInfoAsync(heroInfo).Forget());
            }

            totalPower += power;
        }

        if (isOn)
        {
            for (; i < parentInfo.childCount; i++)
                parentInfo.GetChild(i).gameObject.SetActive(false);
        }

        m_element.power.text = totalPower.AmountKMBT(_isMBT: true);

        PPWorker.Set(PlayerPrefsType.TOURNAMENT_IS_ON_BATCH_INFO, isOn ? 1 : 0, false);
    }


    bool m_isOpenInfo = false;
    async UniTask OpenHeroInfoAsync(HeroInfoData _heroinfoData)
    {
        if (m_isOpenInfo == true)
            return;

        m_isOpenInfo = true;
        await PopupManager.instance.OpenPopupAndWait(PopupType.Hero_HeroInfo, _heroinfoData);
        m_isOpenInfo = false;
    }

    public bool CloseEscape()
    {
        if (PopupManager.instance.IsOpenPopup(PopupType.Hero_HeroInfo))
            return false;

        if (gameObject.activeSelf == true)
        {
            Close();
            return false;
        }

        return true;
    }

    void Close()
    {
        if (isOpenPunch)
            Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
        else
            gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public UIPowerHelper power;
        public Transform treasure;

        public PopupTournament_Batch_Panel panelBatch;

        public ToggleHelper toggleInfo;
        public GameObject baseInfoSlot;

        public ButtonHelper btnStart;

        public void Initialize(Transform _transform)
        {
            power = _transform.GetComponent<UIPowerHelper>("Panel/Batch/Power");
            treasure = _transform.Find("Panel/Treasure/Layout");

            panelBatch = _transform.GetComponent<PopupTournament_Batch_Panel>("Panel/Batch/Batch");

            toggleInfo = _transform.GetComponent<ToggleHelper>("Panel/Toggle");
            baseInfoSlot = _transform.Find("Panel/Batch/Info/Slot").gameObject;

            btnStart = _transform.GetComponent<ButtonHelper>("Panel/btn_confirm");
        }

        public Transform panel => toggleInfo.transform.parent;

        public Transform parentInfo => baseInfoSlot.transform.parent;

    }
    #endregion VALIDATE

}
