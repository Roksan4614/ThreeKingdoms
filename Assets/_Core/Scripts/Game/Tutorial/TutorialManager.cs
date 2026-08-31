using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

public partial class TutorialManager
{
    public static TutorialManager instance { get; private set; } = new();

    GuideQuestRepeatData m_data;
    public static GuideQuestRepeatData data => instance.m_data;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        //PPWorker.DeleteKey(PlayerPrefsType.GUIDE_QUEST_DATA);
        m_data = PPWorker.Get<GuideQuestRepeatData>(PlayerPrefsType.GUIDE_QUEST_DATA);

        if (m_data == null)
        {
            m_data = new();
            m_data.SetDefault();
            SaveData();
        }
    }

    public void TestResetData()
    {
        m_data = new();
        m_data.SetDefault();
        SaveData();

        GuideQuestComponent.instance.StartGuideQuest();
    }

    public bool Update()
    {
        if (m_data.isComplete == false)
        {
            m_data.countTagetValue++;
            SaveData();

            GuideQuestComponent.instance.UpdateStatus();

            if (m_data.isComplete == true)
                PopupManager.instance.AlertShow("길잡이_퀘스트_완료");
            else
                return false;
        }
        return true;
    }

    public void NextOpen()
    {
        // 가이드 퀘스트일 경우
        if (m_data.isGuide)
        {
            if (m_data.historyGuide.Contains(m_data.guideType) == false)
                m_data.historyGuide.Add(m_data.guideType);

            var prev = TableManager.guideQuest.GetGuideData(m_data.guideType);
            m_data.guideType++;

            if (m_data.guideType == GuideQuestType.MAX)
                m_data.guideType = GuideQuestType.NONE;
            else
            {
                var next = TableManager.guideQuest.GetGuideData(m_data.guideType);
                if (next == null)
                {
                    IngameLog.Add("guideGuest cant find: " + m_data.guideType);
                    m_data.guideType = GuideQuestType.NONE;
                }
                // 오픈 스테이지가 있으면 일단 반복 퀘스트로 돌려야 해
                else if (prev.startStage.Length > 0)
                    m_data.guideType = GuideQuestType.NONE;
            }
        }
        // 반복 퀘스트일 경우
        else
        {
            m_data.repeatType++;
            var next = TableManager.guideQuestRepeat.GetRepeatData(m_data.repeatType);

            if (Enum.TryParse(next.key, out GuideQuestRepeatType type))
            {
                switch (type)
                {
                    case GuideQuestRepeatType.stage_boss_kill:
                        {
                            m_data.stageKey++;
                            if (m_data.stageKey > 10)
                            {
                                m_data.chapterKey++;
                                m_data.stageKey = 1;
                            }

                            //가이드중에 열리는 가이드가 있는지 확인해야 해.
                            var guide = TableManager.guideQuest.GetOpenStageData(m_data.chapterKey, m_data.stageKey);

                            if (guide != null)
                            {
                                if (Enum.TryParse(guide.key, out m_data.guideType) == false)
                                    IngameLog.Add("guideGuest cant find: " + guide.key);
                                else
                                    m_data.guideType = GuideQuestType.NONE;
                            }
                        }
                        break;
                    default:
                        {
                            // 다음 반복 퀘스트 찾기
                            while (next.open_guide_quest.IsActive() == true)
                            {
                                // 조건이 있는지 확인
                                if (Enum.TryParse(next.open_guide_quest, out GuideQuestType guideType))
                                {
                                    // 조건이 이미 클리어 했는지 확인
                                    if (IsCompleteGuide(guideType) == true)
                                        break;
                                }
                                else
                                    IngameLog.Add("open_guide_key cant find: " + next.open_guide_quest);

                                m_data.repeatType++;
                                next = TableManager.guideQuestRepeat.GetRepeatData(m_data.repeatType);
                            }
                        }
                        break;
                }
            }
        }

        m_data.countTagetValue = 0;
        GuideQuestComponent.instance.StartGuideQuest();
        SaveData();
    }

    public static async UniTask WaitCompleteGuide(GuideQuestType _type, CancellationToken _token)
        => await UniTask.WaitUntil(() => instance.IsCompleteGuide(_type), cancellationToken: _token);

    public bool IsCompleteGuide(GuideQuestType _type)
        => m_data.historyGuide.Contains(_type);

    void SaveData()
    {
        PPWorker.Set(PlayerPrefsType.GUIDE_QUEST_DATA, m_data);
    }

    public class GuideQuestRepeatData
    {
        public GuideQuestRepeatType repeatType;

        public int chapterKey;
        public int stageKey;

        public GuideQuestType guideType;
        public List<GuideQuestType> historyGuide;

        public int countTagetValue;

        public void SetDefault()
        {
            historyGuide = new();
            chapterKey = stageKey = 1;
        }

        public GuideQuestRepeatType nowRepeatType => isGuide ? GuideQuestRepeatType.NONE : repeatType;
        public bool isGuide => guideType > GuideQuestType.NONE;
        public string name => TableManager.guideQuestString.Get(
            $"{(isGuide ? "GUIDE_" : "REPEAT_")}{(isGuide ? guideType.ToString().ToUpper() : repeatType.ToString().ToUpper())}_TITLE"
            ).message;
        public Table_GuideQuest.TableGuideQuestData tableData =>
            isGuide ?
            TableManager.guideQuest.GetGuideData(guideType) :
            TableManager.guideQuestRepeat.GetRepeatData(repeatType);
        public bool isComplete => countTagetValue >= tableData.targetValue;
        public string statusMessage => isComplete ?
            $"_(완료)" :
            $"({countTagetValue}/{tableData.targetValue})";
    }
}

public enum NavigationType
{
    NONE = -1,

    CHARACTER,
    CASTLE,
    DUNGEON,
    SHOP,
    GACHA,
    STORY,
    RAID,
    PROFILE,
    TOURNAMENT,

    MAX
}

public enum GuideQuestType
{
    NONE = -1,

    move,                               // 이동하기
    normal_attack,                      // 기본공격 하기
    main_skill_use,                     // 스킬 사용하기
    dash_use,                           // 대쉬 사용하기
    storymode_play,                     // 스토리모드 클리어하기
    character_deploy,                   // 장수 배치하기
    daily_dungeon_play,                 // 요일 던전

    CASTLE_START,
    castle_wally,
    CASTLE_FINISHED,

    MAX
}

public enum GuideQuestRepeatType
{
    NONE = -1,

    enemy_kill,                                // 적 처치하기
    tournament_play,                           // 토너먼트 참여하기
    raid_play,                                 // 레이드 참여하기
    gacha_progress,                            // 장수 모집 진행하기
    farm_rice_earn,                            // 군량 수확하기
    market_gold_earn,                          // 금화 수확하기
    office_mission_progress,                   // 관아 파견 보내기
    daily_dungeon_play,                        // 요일던전 플레이하기
    stage_boss_kill,                           // 스테이지 보스 처치

    MAX
}
