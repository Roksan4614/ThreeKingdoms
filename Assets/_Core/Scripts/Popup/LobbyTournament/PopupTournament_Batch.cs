using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournament_Batch : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        transform.GetComponent<Button>("Panel/Top/btn_back").onClick.AddListener(Close);
        transform.GetComponent<TextMeshProUGUI>("Panel/Top/txt_title").text = "장수_편성";
    }

    public async UniTask OpenAsync()
    {
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);

        await UniTask.WaitUntil(() => gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);
    }

    public bool CloseEscape()
    {
        if (gameObject.activeSelf == true)
        {
            Close();
            return false;
        }

        return true;
    }

    void Close()
        => Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));

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
