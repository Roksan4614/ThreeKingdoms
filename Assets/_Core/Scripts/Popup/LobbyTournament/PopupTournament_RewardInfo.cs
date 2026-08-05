using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupTournament_RewardInfo : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(Close);
        transform.GetComponent<Button>("Panel/btn_confirm").onClick.AddListener(Close);
        transform.GetComponent<Button>().onClick.AddListener(Close);

        transform.GetComponent<TextMeshProUGUI>("Panel/txt_title").text = "보상_정보";
    }

    public async UniTask OpenAsync()
    {
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);

        await UniTask.WaitUntil(() => gameObject.activeSelf == false);
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
        public ScrollRect scroll;

        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
        }

        public Transform panel => scroll.transform.parent;
    }
    #endregion VALIDATE

}
