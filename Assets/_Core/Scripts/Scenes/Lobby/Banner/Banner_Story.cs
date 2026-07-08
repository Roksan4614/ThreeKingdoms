using Cysharp.Threading.Tasks;
using UnityEngine;

public class Banner_Story : MonoBehaviour, IValidatable
{
    const string c_keyReddot = "pp_banner_story_reddot";
    private void Awake()
    {
        m_element.button.onClick.AddListener(() => OnButtonAsync_OpenPopup().Forget());
    }

    void Start()
    {
        var stageData = StageManager.instance.data;

        var dbStory = TableManager.storyNode.list.SortBy(x => x.order_num)
            .FindAll(x =>
                (x.chapter_key < stageData.chapterNumber ||
                (x.stage_key < stageData.stageNumber && x.chapter_key == stageData.chapterNumber)) && x.chapter_key > 0);

        if (dbStory.Count == 0)
            gameObject.SetActive(false);

        m_element.reddot.SetActive(PPWorker.HasKey(c_keyReddot));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            var stageData = StageManager.instance.data;
            DataManager.storyMode.ClearStage_AddStoryMode(stageData);
            IngameLog.Add($"스토리 해금: {stageData.chapterNumber}/{stageData.stageNumber}");

            stageData.stageNumber++;
            if (stageData.stageNumber > 10)
            {
                stageData.chapterNumber++;
                stageData.stageNumber = 1;
            }
            StageManager.instance.TestSaveLoadData(stageData);
        }
    }

    async UniTask OnButtonAsync_OpenPopup()
    {
        PPWorker.DeleteKey(c_keyReddot);

        m_element.button.interactable = false;
        await PopupManager.instance.OpenPopupAndWait(PopupType.LobbyStoryMode);
        m_element.button.interactable = true;
        m_element.reddot.SetActive(false);
    }

    public void Reddot()
    {
        gameObject.SetActive(true);

        PPWorker.Set(c_keyReddot, 1);
        m_element.reddot.SetActive(true);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper button;
        public GameObject reddot;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<ButtonHelper>();
            reddot = _transform.Find("Reddot").gameObject;
        }
    }
    #endregion VALIDATE

}
