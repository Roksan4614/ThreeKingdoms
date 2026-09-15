using Cysharp.Threading.Tasks;
using Rev9.Inventory;
using Rev9.Post;
using Rev9.Quest;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Top_Popup_Menu : MonoBehaviour, IValidatable
{
    enum ButtonType
    {
        NONE = -1,

        Setting,
        Pass,
        Noti,
        Inventory,
        Post,
        Quest,
        Rebirth,

        MAX
    }

    Dictionary<ButtonType, ButtonHelper> m_buttons = new();

    RectTransform m_rt;

    Dictionary<ButtonType, BasePopupComponent> m_popups = new();

    public bool isOpenMenu
    {
        get
        {
            if (gameObject.activeSelf)
                return true;
            foreach (var p in m_popups)
                if (p.Value.gameObject.activeSelf == true)
                    return true;
            return false;
        }
    }

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
        foreach (var p in m_popups)
        {
            if (p.Value != null)
                Destroy(p.Value.gameObject);
        }

        m_popups = null;
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
            case ButtonType.Setting:
                {
                    if (m_popups.ContainsKey(_type) == false)
                        m_popups.Add(_type, await PopupManager.instance.OpenPopupAsync<PopupSettingComponent>(PopupType.Setting));
                    else
                        m_popups[_type].OpenPopup();
                }
                break;
            case ButtonType.Pass:
                {
                    if (m_popups.ContainsKey(_type) == false)
                        m_popups.Add(_type, await PopupManager.instance.OpenPopupAsync<PopupPassComponent>(PopupType.Pass));
                    else
                        m_popups[_type].OpenPopup();
                }
                break;
            case ButtonType.Post:
                {
                    if (m_popups.ContainsKey(_type) == false)
                        m_popups.Add(_type, await PopupManager.instance.OpenPopupAsync<PopupPostComponent>(PopupType.Post));
                    else
                        m_popups[_type].OpenPopup();
                }
                break;
            case ButtonType.Inventory:
                {
                    if (m_popups.ContainsKey(_type) == false)
                        m_popups.Add(_type, await PopupManager.instance.OpenPopupAsync<PopupInventoryComponent>(PopupType.Inventory));
                    else
                        m_popups[_type].OpenPopup();
                }
                break;
            case ButtonType.Quest:
                {
                    if (m_popups.ContainsKey(_type) == false)
                        m_popups.Add(_type, await PopupManager.instance.OpenPopupAsync<PopupQuestComponent>(PopupType.Quest));
                    else
                        m_popups[_type].OpenPopup();
                }
                break;
            case ButtonType.Rebirth:
                {
                    if (DataManager.instance.isLobby == true)
                    {
                        if (m_popups.ContainsKey(_type) == false)
                            m_popups.Add(_type, await PopupManager.instance.OpenPopupAsync<PopupRebirthComponent>(PopupType.Rebirth));
                        else
                            m_popups[_type].OpenPopup();
                    }
                    else
                        OnButton_Exit();
                }
                break;
            default:
                PopupManager.instance.AlertShow("아직_준비중입니다.");
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

    [SerializeField]
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
