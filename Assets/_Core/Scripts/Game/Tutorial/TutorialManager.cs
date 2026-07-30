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

        if (m_data.isActive == false)
        {
            m_data.SetDefault();
            SaveData();
        }
    }

    public void TestResetData()
    {
        m_data = default;
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
                if (next.isActive == false)
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
                    case GuideQuestRepeatType.STAGE_BOSS_KILL:
                        {
                            m_data.stageKey++;
                            if (m_data.stageKey > 10)
                            {
                                m_data.chapterKey++;
                                m_data.stageKey = 1;
                            }

                            //가이드중에 열리는 가이드가 있는지 확인해야 해.
                            var guide = TableManager.guideQuest.GetOpenStageData(m_data.chapterKey, m_data.stageKey);

                            if (guide.isActive == true)
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

    public struct GuideQuestRepeatData
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
        public bool isActive => historyGuide != null;
        public bool isGuide => guideType > GuideQuestType.NONE;
        public string name => TableManager.guideQuestString.Get(
            $"{(isGuide ? "GUIDE_" : "REPEAT_")}{(isGuide ? guideType : repeatType)}_TITLE"
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

    MOVE,                               // 이동하기
    NORMAL_ATTACK,                      // 기본공격 하기
    MAIN_SKILL_USE,                     // 스킬 사용하기
    DASH_USE,                           // 대쉬 사용하기
    STORYMODE_PLAY,                     // 스토리모드 클리어하기
    CHARACTER_DEPLOY,                   // 장수 배치하기
    DAILY_DUNGEON_PLAY,                 // 요일 던전

    CASTLE_START,
    CASTLE_WALLY,
    CASTLE_FINISHED,

    MAX
}

public enum GuideQuestRepeatType
{
    NONE = -1,

    ENEMY_KILL,                                // 적 처치하기
    TOURNAMENT_PLAY,                           // 토너먼트 참여하기
    RAID_PLAY,                                 // 레이드 참여하기
    GACHA_PROGRESS,                            // 장수 모집 진행하기
    FARM_RICE_EARN,                            // 군량 수확하기
    MARKET_GOLD_EARN,                          // 금화 수확하기
    OFFICE_MISSION_PROGRESS,                   // 관아 파견 보내기
    DAILY_DUNGEON_PLAY,                        // 요일던전 플레이하기
    STAGE_BOSS_KILL,                           // 스테이지 보스 처치

    MAX
}
