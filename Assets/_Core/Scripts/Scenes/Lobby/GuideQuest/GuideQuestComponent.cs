using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class GuideQuestComponent : Singleton<GuideQuestComponent>, IValidatable
{
    float m_startPosY;

    protected override void OnAwake()
    {
        m_startPosY = m_element.rt.anchoredPosition.y;
    }

    private void Start()
    {
        m_element.button.onClick.AddListener(OnButton_Quest);

        if (TutorialManager.instance.IsCompleteGuide(GuideQuestType.DASH_USE))
            ControllerManager.instance.SetActive_GuideQuestArrow(false, GuideQuestType.DASH_USE);

        StartGuideQuest();
    }

    Tween m_tweenMovePanel;
    public void SetMoveArea(bool _isBottom, bool _isTween = true, float _duration = .2f)
    {
        m_tweenMovePanel?.Kill();
        float target = _isBottom ? 270 : m_startPosY;
        m_tweenMovePanel = m_element.rt.DOAnchorPosY(target, _duration);
    }

    public void StartGuideQuest()
    {
        var tableData = TutorialManager.data.tableData;

        m_element.textTitle.text = TutorialManager.data.name;
        UpdateStatus();

        // MARGIN
        {
            var margin = m_element.textTitle.margin;
            bool isTargetValue = tableData.targetValue > 0;
            m_element.textStatus.gameObject.SetActive(isTargetValue);
            margin.w = isTargetValue ? 35 : 0;
            m_element.textTitle.margin = margin;
        }

        // REWARD
        {
            m_element.reward.SetItemData(new()
            {
                key = tableData.reward_item,
                count = tableData.reward_count
            });
        }

        RunAsync().Forget();
    }

    public void UpdateStatus()
    {
        m_element.textStatus.text = TutorialManager.data.statusMessage;

        bool isComplete = TutorialManager.data.isComplete;
        m_element.textTitle.alpha = isComplete ? 1 : .9f;
        m_element.complete.SetActive(isComplete);
    }

    public async UniTask RunAsync()
    {
        await UniTask.WaitUntil(() => TeamManager.instance.mainHero == true);

        if (TutorialManager.data.isComplete == true)
            return;

        if (TutorialManager.data.isGuide)
        {
            var guideType = TutorialManager.data.guideType;
            switch (guideType)
            {
                case GuideQuestType.MOVE:
                    OnButton_Quest();
                    await MoveAsync();
                    break;
                case GuideQuestType.NORMAL_ATTACK:
                    OnButton_Quest();
                    ControllerManager.instance.SetActive_GuideQuestArrow(true, guideType);
                    await NormalAttackAsync();
                    ControllerManager.instance.SetActive_GuideQuestArrow(false, guideType);
                    break;
                case GuideQuestType.MAIN_SKILL_USE:
                    OnButton_Quest();
                    ControllerManager.instance.SetActive_GuideQuestArrow(true, guideType);
                    await MainSkillUseAsync();
                    ControllerManager.instance.SetActive_GuideQuestArrow(false, guideType);
                    break;
                case GuideQuestType.DASH_USE:
                    OnButton_Quest();
                    ControllerManager.instance.SetActive_GuideQuestArrow(true, guideType);
                    await DashUseAsync();
                    ControllerManager.instance.SetActive_GuideQuestArrow(false, guideType);
                    break;
                case GuideQuestType.STORYMODE_PLAY:
                    await StoryModePlayAsync();
                    break;
                case GuideQuestType.CHARACTER_DEPLOY:
                    await CharacterDeployAsync();
                    break;
            }
        }
        else
        {
            switch (TutorialManager.data.repeatType)
            {
                case GuideQuestRepeatType.ENEMY_KILL:
                    break;
            }
        }

    }

    void OnButton_Quest()
    {
        if (TutorialManager.data.isComplete)
            RewardStartAsync().Forget();
        else
        {
            var tableData = TutorialManager.data.tableData;
            if (tableData.navi.Length > 0)
            {
                switch (tableData.navi[0])
                {
                    case NavigationType.CHARACTER:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Hero);
                        break;
                    case NavigationType.CASTLE:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Castle);
                        break;
                    case NavigationType.DUNGEON:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Boss);
                        break;
                    case NavigationType.GACHA:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Summon);
                        break;
                    case NavigationType.STORY:
                        BannerComponent.instance.story.OnButtonAsync_OpenPopup().Forget();
                        break;
                    case NavigationType.RAID:
                        PopupManager.instance.OpenPopup(PopupType.LobbyBossRaid);
                        break;
                    case NavigationType.TOURNAMENT:
                        // todo
                        break;
                }
            }
            else if (TutorialManager.data.isGuide)
            {
                var talkbox = TeamManager.instance.mainHero.talkbox;
                switch (TutorialManager.data.guideType)
                {
                    case GuideQuestType.MOVE:
                        {
                            if (talkbox.isActive == false)
                                talkbox.Start_AutoClose(destroyCancellationToken,
                                    Configure.isPC ? "[W,A,S,D]를_눌러\n이동해보자." : "화면을_터치해_이동해보자.");
                        }
                        break;
                    case GuideQuestType.NORMAL_ATTACK:
                        {
                            if (talkbox.isActive == false)
                                talkbox.Start_AutoClose(destroyCancellationToken,
                                    Configure.isPC ?
                                    "[X]키를_눌러_공격해보자.\n화면을_터치해도_가능해." :
                                    "공격_버튼을_눌러보자.");
                        }
                        break;
                    case GuideQuestType.MAIN_SKILL_USE:
                        {
                            if (talkbox.isActive == false)
                                talkbox.Start_AutoClose(destroyCancellationToken,
                                    Configure.isPC ?
                                    "[C]키를_누른_후_좌클릭해봐.\n버튼을_눌러서도_가능해." :
                                    "스킬_버튼을_눌러보자.");
                        }
                        break;
                    case GuideQuestType.DASH_USE:
                        {
                            if (talkbox.isActive == false)
                                talkbox.Start_AutoClose(destroyCancellationToken,
                                    Configure.isPC ?
                                    "[SpaceBar]키를_눌러보자.\n버튼을_눌러서도_가능해." :
                                    "대쉬_버튼을_눌러보자.");
                        }
                        break;
                    case GuideQuestType.STORYMODE_PLAY:
                        BannerComponent.instance.story.OnButtonAsync_OpenPopup().Forget();
                        break;
                    case GuideQuestType.CHARACTER_DEPLOY:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Hero);
                        break;
                    case GuideQuestType.DAILY_DUNGEON_PLAY:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Boss);
                        break;
                }
            }
        }
    }

    async UniTask RewardStartAsync()
    {
        await UniTask.NextFrame();

        List<TableItemData> rewards = new();
        var tableData = TutorialManager.data.tableData;

        rewards.Add(TableManager.item.GetItemData(tableData.reward_item, tableData.reward_count));

        RewardWorker.instance.RunAsync(m_element.reward.transform.position, _itemData: rewards.ToArray()).Forget();

        TutorialManager.instance.NextOpen();
    }

    #region VALIDATE
    public void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform rt;
        public ItemComponent reward;

        public TextMeshProUGUI textTitle;
        public TextMeshProUGUI textStatus;

        public Button button;
        public GameObject complete;

        public void Initialize(Transform _transform)
        {
            rt = (RectTransform)_transform;
            button = rt.Find("Panel").GetComponent<Button>();
            complete = panel.Find("Complete").gameObject;

            reward = panel.Find("Reward").GetChild(0).GetComponent<ItemComponent>();
            textTitle = panel.GetComponent<TextMeshProUGUI>("Text");
            textStatus = panel.GetComponent<TextMeshProUGUI>("txt_status");
        }
        public Transform panel => button.transform;
    }
    #endregion VALIDATE
}
