using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class Top_Popup_Menu : MonoBehaviour, IValidatable
{
    RectTransform m_rt;
    private void Start()
    {
        m_rt = (RectTransform)transform;

        m_element.btnExit.onClick.AddListener(OnButton_Exit);

        m_element.btnExit.text = DataManager.instance.isLobby ? "종_료" : "_나가기_";
    }

    private void Update()
    {
        if (Utils.IsOutClick(m_rt) == true)
            gameObject.SetActive(false);
    }

    void OnButton_Exit()
    {
        m_element.btnExit.interactable = false;

        if (BossRaidWorker.instance.isRunning)
            BossRaidWorker.instance.ExitAsync().Forget();
        else if (DataManager.dailyDungeon.isRunning)
            DataManager.dailyDungeon.ExitAsync().Forget();
        else if (DataManager.storyMode.isRunning)
            DataManager.storyMode.ExitAsync().Forget();
        else
        {
#if UNITY_EDITOR
            Application.Quit();
#else
#endif
        }

        gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper btnExit;
        public void Initialize(Transform _transform)
        {
            btnExit = _transform.GetComponent<ButtonHelper>("btn_exit");
        }
    }
    #endregion VALIDATE

}
