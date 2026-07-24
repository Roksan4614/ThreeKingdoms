using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class Top_Popup_Menu : MonoBehaviour, IValidatable
{
    RectTransform m_rt;
    private void Start()
    {
        m_rt = (RectTransform)transform;

        m_element.btnExit.onClick.AddListener(OnButton_Exit);
        m_element.btnCheat_StorMode.onClick.AddListener(OnButton_Cheat_StoryMode);

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

    void OnButton_Cheat_StoryMode()
    {
        var db = TableManager.storyNode.list.Where(x => x.chapter_key > 0).ToList();

        for (int i = 0; i < db.Count; i++)
        {
            var node = db[i];
            if (DataManager.storyMode.IsComplete(node.node_key) == false)
            {
                // 보상이 영웅이라면
                if (node.reward_character.IsActive() == true)
                {
                    // 첫번째인데 내 국가가 아니면 군주 추가해줘야해.
                    if (node.order_num <= 3 && node.region_type != DataManager.userInfo.region)
                    {
                        //오나라는 손견을 줘야해..
                        var startHero = node.region_type == RegionType.WU
                            ? CharacterName.SunJian.ToString()
                            : TableManager.region.Get(node.region_type).master;

                        node.reward_character = $"{startHero},{node.reward_character}";
                    }

                    var rewards = node.reward_character.Replace(" ", "").Split(',');

                    //일단 보상에 넣어주자
                    //배치하다가 꺼버릴수도 있어서
                    foreach (var key in rewards)
                        DataManager.userInfo.AddHeroSoul(key, 10);
                }
                DataManager.storyMode.TestSave(node);
            }
        }

        DataManager.storyMode.lastHistory = default;

        var stageData = StageManager.instance.data;
        if (stageData.level == 1)
        {
            stageData.level = 2;
            stageData.chapterNumber = 1;
            stageData.stageNumber = 1;
            StageManager.instance.TestSaveLoadData(stageData);

            StageManager.instance.RestartStage();
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper btnExit;
        public ButtonHelper btnCheat_StorMode;

        public void Initialize(Transform _transform)
        {
            btnExit = _transform.GetComponent<ButtonHelper>("btn_exit");
            btnCheat_StorMode = _transform.GetComponent<ButtonHelper>("btn_cheat_storymode");
        }
    }
    #endregion VALIDATE

}
