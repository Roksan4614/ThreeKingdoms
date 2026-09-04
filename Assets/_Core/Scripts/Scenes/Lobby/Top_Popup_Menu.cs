using Cysharp.Threading.Tasks;
using Rev9.Inventory;
using Rev9.Post;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Top_Popup_Menu : MonoBehaviour, IValidatable
{
    enum ButtonType
    {
        NONE = -1,

        Setting,
        Noti,
        Inventory,
        Post,
        Quest,
        Rebirth,

        MAX
    }

    Dictionary<ButtonType, ButtonHelper> m_buttons = new();

    RectTransform m_rt;

    PopupInventoryComponent m_inventory;
    PopupPostComponent m_post;

    public bool isOpenMenu => gameObject.activeSelf ||
        (m_inventory?.gameObject.activeSelf ?? false) ||
        (m_post?.gameObject.activeSelf ?? false);

    private void Start()
    {
        m_rt = (RectTransform)transform;
        m_buttons = m_element.buttons.ToDictionary(x => (ButtonType)m_element.buttons.FindIndex(b => b == x), x => x);

#if SERVICE_DEV
        {
            var btn = Instantiate(m_buttons[ButtonType.Rebirth], transform);
            btn.text = "길잡이 초기화";
            btn.onClick.AddListener(() =>
            {
                TutorialManager.instance.TestResetData();
                gameObject.SetActive(false);
            });
        }
        {
            var btn = Instantiate(m_buttons[ButtonType.Rebirth], transform);
            btn.text = "스토리 해금";
            btn.onClick.AddListener(OnButton_Cheat_StoryMode);
        }
#endif

        var btnRebirth = m_buttons[ButtonType.Rebirth];
        btnRebirth.text = DataManager.instance.isLobby ? "_회귀" : "_나가기";

        foreach (var b in m_buttons)
            b.Value.onClick.AddListener(() => OnButtonAsync(b.Key).Forget());
    }

    void OnDestroy()
    {
        if (m_inventory != null)
            Destroy(m_inventory.gameObject);
        if (m_post != null)
            Destroy(m_post.gameObject);
    }

    public void SetActive(bool _isActive) => gameObject.SetActive(_isActive);

    async UniTask OnButtonAsync(ButtonType _type)
    {
        Close();

        await UniTask.WaitForSeconds(.1f);

        var btn = m_buttons[_type];
        btn.interactable = false;
        switch (_type)
        {
            //    case ButtonType.Setting:
            //        btn.onClick.AddListener(() => PopupManager.instance.OpenPopupAsync<PopupSettingComponent>(PopupType.Setting).Forget());
            //        break;
            //    case ButtonType.Noti:
            //        btn.onClick.AddListener(() => PopupManager.instance.OpenPopupAsync<PopupNotiComponent>(PopupType.Noti).Forget());
            //        break;
            case ButtonType.Post:
                if (m_post == null)
                    m_post = await PopupManager.instance.OpenPopupAsync<PopupPostComponent>(PopupType.Post);
                else
                    m_post.OpenPopup();
                break;
            case ButtonType.Inventory:
                if (m_inventory == null)
                    m_inventory = await PopupManager.instance.OpenPopupAsync<PopupInventoryComponent>(PopupType.Inventory);
                else
                    m_inventory.OpenPopup();
                break;
            //    case ButtonType.Post:
            //        btn.onClick.AddListener(() => PopupManager.instance.OpenPopupAsync<PopupPostComponent>(PopupType.Post).Forget());
            //        break;
            //    case ButtonType.Quest:
            //        btn.onClick.AddListener(() => PopupManager.instance.OpenPopupAsync<PopupQuestComponent>(PopupType.Quest).Forget());
            //        break;
            case ButtonType.Rebirth:
                OnButton_Exit();
                break;
        }

        btn.interactable = true;
    }

    void Close()
    {
        Utils.SetActivePunch(transform, false, _scaleValue: .7f);
    }

    private void Update()
    {
        if (Utils.IsOutClick(m_rt) == true || Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    void OnButton_Exit()
    {
        var btnRebirth = m_buttons[ButtonType.Rebirth];
        btnRebirth.interactable = false;

        if (BossRaidWorker.instance.isRunning)
            BossRaidWorker.instance.ExitAsync().Forget();
        else if (DataManager.dailyDungeon.isRunning)
            DataManager.dailyDungeon.ExitAsync().Forget();
        else if (DataManager.storyMode.isRunning)
        {
            (SceneBase.instance as Scene_StoryMode).OnButtonAsync_Skip(
                _result => btnRebirth.interactable = _result != StatusType.Success).Forget();
        }
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
                    {
                        if (DataManager.userInfo.HasHero(key) == false)
                            DataManager.userInfo.AddHero(key);
                    }
                }
                DataManager.storyMode.TestSave(node);
            }
        }
        DataManager.storyMode.nodeKeyNewClear = null;
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

        gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public List<ButtonHelper> buttons;

        public void Initialize(Transform _transform)
        {
            buttons = _transform.GetComponentsInChildren<ButtonHelper>().ToList();
        }


    }
    #endregion VALIDATE

}
