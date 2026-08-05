using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournament_UserInfo : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(Close);
        transform.GetComponent<Button>().onClick.AddListener(Close);

        transform.GetComponent<TextMeshProUGUI>("Panel/txt_title").text = "상세_정보";

        m_element.treasure.parent.GetComponent<TextMeshProUGUI>("Text").text = "보물_";
        m_element.panel.GetComponent<TextMeshProUGUI>("Batch/Text").text = "상대_조합";
    }

    async UniTask OpenAsync()
    {
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);



        SetBatchPosition();
        SetTreasure();

        await UniTask.WaitUntil(() => gameObject.activeSelf == false);
    }

    void SetBatchPosition()
    {

    }

    void SetTreasure()
    {

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
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Transform panel;
        public Transform treasure;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            treasure = _transform.Find("Panel/Treasure/Layout");
        }

    }
    #endregion VALIDATE

}
