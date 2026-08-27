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
    CharacterComponent m_guide;

    protected override void OnAwake()
    {
        m_startPosY = m_element.rt.anchoredPosition.y;

        m_guide = transform.GetComponent<CharacterComponent>("Talkbox/Host/Guide");
    }

    private void Start()
    {
        m_guide?.gameObject.SetActive(false);
        m_element.img_circle.gameObject.SetActive(false);

        m_element.button.onClick.AddListener(OnButton_Quest);

        if (TutorialManager.instance.IsCompleteGuide(GuideQuestType.DASH_USE))
            ControllerManager.instance.SetActive_GuideQuestArrow(false, GuideQuestType.DASH_USE);

        StartGuideQuest(true);
    }

    Tween m_tweenMovePanel;
    public void SetMoveArea(bool _isBottom, bool _isTween = true, float _duration = .2f)
    {
        m_tweenMovePanel?.Kill();
        float target = _isBottom ? 270 : m_startPosY;
        m_tweenMovePanel = m_element.rt.DOAnchorPosY(target, _duration);
    }

    public void StartGuideQuest(bool _isInitialized = false)
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

        if (_isInitialized == false)
            RunAsync().Forget();
    }

    public void UpdateStatus()
    {
        m_element.textStatus.text = TutorialManager.data.statusMessage;

        bool isComplete = TutorialManager.data.isComplete;
        m_element.textTitle.alpha = isComplete ? 1 : .9f;
        m_element.complete.SetActive(isComplete);

        if (isComplete == true)
            HostOutAsync().Forget();
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
                    BannerComponent.instance.SetActive_GuideArrow(true, guideType);
                    HostTalkboxStart("스토리_모드를_진행해서\n동료를_얻자!", true);
                    await StoryModePlayAsync();
                    BannerComponent.instance.SetActive_GuideArrow(false);
                    break;
                case GuideQuestType.CHARACTER_DEPLOY:
                    if (TeamManager.instance.members.Count == 1)
                        HostTalkboxStart("얻은 장수를 ", true);
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
                            HostTalkboxStart(Configure.isPC ?
                                "[W,A,S,D]를_눌러\n이동해보자." :
                                "화면을_터치해_이동해보자.");
                        }
                        break;
                    case GuideQuestType.NORMAL_ATTACK:
                        {
                            HostTalkboxStart(Configure.isPC ?
                                    "[X]키를_눌러_공격해보자.\n화면을_터치해도_가능해." :
                                    "공격_버튼을_눌러보자.");
                        }
                        break;
                    case GuideQuestType.MAIN_SKILL_USE:
                        {
                            HostTalkboxStart(Configure.isPC ?
                                    "[C]키를_누른_후_좌클릭해봐.\n버튼을_눌러서도_가능해." :
                                    "스킬_버튼을_눌러보자.");
                        }
                        break;
                    case GuideQuestType.DASH_USE:
                        {
                            HostTalkboxStart(Configure.isPC ?
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

    void HostTalkboxStart(string _message, bool _isLoop = false)
        => HostTalkboxStartAsync(_message, _isLoop).Forget();
    async UniTask HostTalkboxStartAsync(string _message, bool _isLoop = false)
    {
        if (m_guide.gameObject.activeSelf == false)
        {
            m_element.img_circle.gameObject.SetActive(true);
            var targetAlpha = m_element.img_circle.color.a;
            m_element.img_circle.Alpha(0);
            Utils.AfterSecond(() => m_element.img_circle.DOFade(targetAlpha, 0.2f).Forget(), 0.1f);

            m_guide.gameObject.SetActive(true);

            m_guide.transform.localPosition = new Vector3(-5f, 0);

            m_guide.move.SetFlip(true);
            m_guide.anim.Play(CharacterAnimType.Dash);
            await m_guide.transform.DOLocalMoveX(0, 0.2f);
            m_guide.anim.Play(CharacterAnimType.Idle);

            m_element.img_circle.transform.DORotate(new Vector3(0f, 0f, 360f), 20f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear).Forget();

            await UniTask.WaitForSeconds(.5f);
        }

        if (m_guide.talkbox.isActive == false)
        {
            m_guide.talkbox.rt.pivot = new Vector2(0, .3f);
            m_guide.talkbox.rt.SetAnchoredPosition(0, 190);

            m_guide.anim.Play("Talk", 1);
            if (_isLoop == true)
                m_guide.talkbox.Start(destroyCancellationToken, _message);
            else
                m_guide.talkbox.Start_AutoClose(destroyCancellationToken, _message);

            await m_guide.talkbox.WaitFinishTyping();

            m_guide.anim.Play("NONE", 1);
        }
    }

    async UniTask HostOutAsync()
    {
        if (m_guide.gameObject.activeSelf == false)
            return;

        m_element.img_circle.transform.DOKill();
        m_element.img_circle.transform.rotation = Quaternion.Euler(Vector3.zero);
        m_element.img_circle.gameObject.SetActive(false);

        m_guide.talkbox.SetActive(false);
        m_guide.move.SetFlip(false);
        m_guide.anim.Play(CharacterAnimType.Dash);
        EffectWorker.instance.Dash(m_guide, false);
        await m_guide.transform.DOLocalMoveX(-5f, 0.2f);

        m_guide.gameObject.SetActive(false);
    }

    async UniTask RewardStartAsync()
    {
        await UniTask.NextFrame();

        List<ItemData> rewards = new();
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

        public Image img_circle;

        public void Initialize(Transform _transform)
        {
            rt = (RectTransform)_transform;
            button = rt.Find("Panel").GetComponent<Button>();
            complete = panel.Find("Complete").gameObject;

            reward = panel.Find("Reward").GetChild(0).GetComponent<ItemComponent>();
            textTitle = panel.GetComponent<TextMeshProUGUI>("Text");
            textStatus = panel.GetComponent<TextMeshProUGUI>("txt_status");

            img_circle = _transform.GetComponent<Image>("Talkbox/img_bg");
        }
        public Transform panel => button.transform;
    }
    #endregion VALIDATE
}
