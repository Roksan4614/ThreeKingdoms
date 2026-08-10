using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournament_Batch : MonoBehaviour, IValidatable
{
    enum TabType
    {
        Hero, Relic
    }

    private void Awake()
    {
        transform.GetComponent<Button>("Panel/Top/btn_back").onClick.AddListener(Close);
        transform.GetComponent<TextMeshProUGUI>("Panel/Top/txt_title").text = "장수_편성";

        m_element.tabRelic.onClick.AddListener(() => OnButton_Tab(TabType.Relic));
        m_element.tabHero.onClick.AddListener(() => OnButton_Tab(TabType.Hero));
    }

    public async UniTask<bool> OpenAsync()
    {
        m_isCloseStart = false;
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);

        OnButton_Tab(TabType.Hero);

        await UniTask.WaitUntil(() => m_isCloseStart == true, cancellationToken: destroyCancellationToken);

        return m_element.panelHero.isUpdated;
    }

    void OnButton_Tab(TabType _tabType)
    {
        m_element.panelRelic.gameObject.SetActive(_tabType == TabType.Relic);
        m_element.panelHero.gameObject.SetActive(_tabType == TabType.Hero);

        m_element.tabHero.SetDrawSelect(_tabType == TabType.Hero);
        m_element.tabRelic.SetDrawSelect(_tabType == TabType.Relic);
    }

    public bool CloseEscape()
    {
        if (gameObject.activeSelf == true)
        {
            if (PopupManager.instance.IsOpenPopup(PopupType.Hero_HeroInfo) == true)
                return false;

            Close();
            return false;
        }

        return true;
    }

    bool m_isCloseStart;
    void Close()
    {
        if (m_element.panelHero.gameObject.activeSelf == true)
            m_element.panelHero.CloseAsync(() =>
            {
                m_isCloseStart = true;
                Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
            }).Forget();
        else
        {
            m_isCloseStart = true;
            Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper btnAuto;
        public ButtonHelper btnBatch;

        public ButtonHelper tabHero;
        public ButtonHelper tabRelic;

        public PopupTournament_Batch_Hero panelHero;
        public PopupTournament_Batch_Relic panelRelic;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");

            btnAuto = panel.GetComponent<ButtonHelper>("Button/btn_auto");
            btnBatch = panel.GetComponent<ButtonHelper>("Button/btn_batch");

            tabHero = panel.GetComponent<ButtonHelper>("Tab/btn_hero");
            tabRelic = panel.GetComponent<ButtonHelper>("Tab/btn_relic");

            panelHero = panel.GetComponent<PopupTournament_Batch_Hero>("Hero");
            panelRelic = panel.GetComponent<PopupTournament_Batch_Relic>("Relic");
        }

        public Transform panel => panelHero.transform.parent;
    }
    #endregion VALIDATE

}
