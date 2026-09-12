using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Enums;
using ThreeKingdoms.Shared.Types;
using UnityEngine;

public partial class TutorialManager
{
    public GuideQuestLobbyDto ServerLobby { get; private set; }
    public GuideQuestIssueDto ServerIssue => ServerLobby?.Issue;
    public Table_GuideQuest.TableGuideQuestData ServerTableData { get; private set; }
    public bool ServerClaiming { get; private set; }
    public event Action ServerIssueChanged;
    long m_serverUid;
    string m_serverRevision;
    GuideProgressCache m_serverProgress;
    bool m_serverEventsAttached;
    public bool HasServerIssue => GameServer.IsLoggedIn && m_serverUid == GameServer.Uid && ServerIssue != null;
    public bool ServerContentReady => HasServerIssue && !UnavailableKeys.Contains(ServerIssue.QuestKey);
    public bool ServerProgressComplete => ServerContentReady && m_data.countTagetValue >= ServerIssue.TargetValue;
    public string ServerStatus => !HasServerIssue ? "지역 선택 후 안내합니다" : !ServerContentReady ? "콘텐츠 준비 중"
        : ServerClaiming ? "보상 받는 중" : ServerProgressComplete ? "보상 받기" : $"({m_data.countTagetValue}/{ServerIssue.TargetValue})";

    static readonly HashSet<string> UnavailableKeys = new HashSet<string>
    { "storymode_play", "match", "gacha_progress", "merchant_shop_list_refresh", "return" };
    static readonly Dictionary<string, string> Titles = new Dictionary<string, string>
    {
        ["move"] = "이동하기", ["normal_attack"] = "일반 공격하기", ["main_skill_use"] = "주장 스킬 사용하기",
        ["dash_use"] = "대시 사용하기", ["storymode_play"] = "스토리 플레이하기", ["character_deploy"] = "무장 배치하기",
        ["match"] = "무장 조합하기", ["sub_skill_use"] = "부장 스킬 사용하기", ["auto_play_active"] = "자동 스킬 켜기",
        ["change_main"] = "주장 변경하기", ["change_position"] = "직책 장착하기", ["gacha_progress"] = "소환하기",
        ["castle"] = "영지 방문하기", ["palace_deploy"] = "궁전에 무장 배치하기", ["farm_deploy"] = "농지에 무장 배치하기",
        ["market_deploy"] = "시장에 무장 배치하기", ["thief_catch"] = "도둑 잡기", ["gate_deploy"] = "성문에 무장 배치하기",
        ["office_mission_refresh"] = "관아 임무 갱신하기", ["office_mission_progress"] = "관아 임무 시작하기",
        ["merchant_deploy"] = "상단에 무장 배치하기", ["merchant_shop_list_refresh"] = "상단 상품 갱신하기",
        ["farm_rice_earn"] = "농지 군량 받기", ["market_gold_earn"] = "시장 금화 받기", ["daily_dungeon_play"] = "요일던전 참가하기",
        ["tournament_play"] = "토너먼트 참가하기", ["raid_play"] = "레이드 참가하기", ["equip_treasure"] = "보물 장착하기",
        ["return"] = "회귀하기", ["relic_enhance"] = "유물 강화하기", ["enemy_kill"] = "적 처치하기", ["stage_boss_kill"] = "스테이지 보스 처치하기"
    };

    public string ServerTitle
    {
        get
        {
            if (!HasServerIssue) return "가이드 퀘스트";
            var key = $"{(ServerIssue.Kind == GuideQuestKind.Learning ? "GUIDE" : "REPEAT")}_{ServerIssue.QuestKey.ToUpperInvariant()}_TITLE";
            var title = TableManager.guideQuestString?.GetString(key);
            return !string.IsNullOrEmpty(title) && title != key ? title
                : Titles.TryGetValue(ServerIssue.QuestKey, out var fallback) ? fallback : "가이드 퀘스트";
        }
    }

    public async UniTask ServerRefreshAsync()
    {
        if (!GameServer.Enabled) return;
        if (!m_serverEventsAttached)
        {
            GameServer.RequestSucceeded += OnServerRequestSucceeded;
            m_serverEventsAttached = true;
        }
        if (m_data == null) { m_data = new GuideQuestRepeatData(); m_data.SetDefault(); }
        if (m_serverUid != GameServer.Uid)
        {
            ServerLobby = null; ServerTableData = null; m_serverRevision = null; m_serverProgress = null;
            m_data = new GuideQuestRepeatData(); m_data.SetDefault();
            m_data.guideType = GuideQuestType.NONE; m_data.repeatType = GuideQuestRepeatType.NONE;
            m_serverUid = GameServer.Uid;
        }
        if (!GameServer.IsLoggedIn || GameServer.Login?.RegionSelected != true) { NotifyServerIssueChanged(); return; }
        var uid = GameServer.Uid;
        var result = (await GameServer.GuideQuest.LobbyAsync(new GuideQuestLobbyReq(), GameServer.Options())).Data;
        if (uid == GameServer.Uid) ApplyServerLobby(result);
    }

    public void ResetForBootstrap()
    {
        if (m_serverEventsAttached) GameServer.RequestSucceeded -= OnServerRequestSucceeded;
        m_serverEventsAttached = false;
        ServerLobby = null; ServerTableData = null; m_serverProgress = null;
        m_serverUid = 0; m_serverRevision = null; ServerClaiming = false;
        m_data = new GuideQuestRepeatData(); m_data.SetDefault();
        m_data.guideType = GuideQuestType.NONE; m_data.repeatType = GuideQuestRepeatType.NONE;
    }

    void ApplyServerLobby(GuideQuestLobbyDto lobby)
    {
        if (lobby?.Issue == null) throw new InvalidOperationException("GUIDE_ISSUE_MISSING");
        if (m_serverRevision != null && (lobby.Revision.Length < m_serverRevision.Length
            || (lobby.Revision.Length == m_serverRevision.Length && string.CompareOrdinal(lobby.Revision, m_serverRevision) < 0))) return;
        var changed = ServerIssue?.IssueId != lobby.Issue.IssueId;
        ServerLobby = lobby; m_serverRevision = lobby.Revision;
        m_data.historyGuide = new List<GuideQuestType>();
        var completed = new HashSet<long>(lobby.CompletedLearningQuestIds ?? new List<long>());
        foreach (var row in GameServer.TableRows("s_guide_quest").OfType<JObject>())
            if (completed.Contains((long)row["idx"]) && Enum.TryParse((string)row["key"], true, out GuideQuestType type)) m_data.historyGuide.Add(type);
        m_data.guideType = lobby.Issue.Kind == GuideQuestKind.Learning && Enum.TryParse(lobby.Issue.QuestKey, true, out GuideQuestType guide)
            ? guide : GuideQuestType.NONE;
        m_data.repeatType = lobby.Issue.Kind == GuideQuestKind.Repeat && Enum.TryParse(lobby.Issue.QuestKey, true, out GuideQuestRepeatType repeat)
            ? repeat : GuideQuestRepeatType.NONE;
        var reward = ServerState.ToItem(lobby.Issue.RewardItemId, lobby.Issue.RewardAmount);
        ServerTableData = Table_GuideQuest.TableGuideQuestData.FromIssue(lobby.Issue.QuestKey, lobby.Issue.TargetValue, lobby.Issue.Navigation, reward);
        if (changed || m_serverProgress == null)
            m_serverProgress = PPWorker.Get<GuideProgressCache>(ProgressKey()) ?? new GuideProgressCache();
        m_serverProgress.RequestIds ??= new List<string>();
        m_data.countTagetValue = (int)Math.Min(lobby.Issue.TargetValue, Math.Max(0, m_serverProgress.Count));
        NotifyServerIssueChanged();
        Debug.Log($"[GUIDE_ISSUE] uid={GameServer.Uid} issue={lobby.Issue.IssueId} key={lobby.Issue.QuestKey} completed={completed.Count}");
    }

    string ProgressKey() => $"PP_SERVER_GUIDE_{m_serverUid}_{ServerIssue.IssueId}";
    void SaveServerProgress()
    {
        if (!HasServerIssue || m_serverProgress == null) return;
        m_serverProgress.Count = m_data.countTagetValue;
        PPWorker.Set(ProgressKey(), m_serverProgress);
    }
    void NotifyServerIssueChanged()
    {
        if (ServerIssueChanged == null) return;
        foreach (Action handler in ServerIssueChanged.GetInvocationList())
            try { handler(); } catch (Exception error) { Debug.LogWarning("[GUIDE_UI] " + error.Message); }
    }
    public bool ServerRecordProgress(string key, string requestId = null, string issueId = null)
    {
        if (!ServerContentReady || ServerClaiming || ServerIssue.QuestKey != key
            || (issueId != null && ServerIssue.IssueId != issueId) || ServerProgressComplete) return false;
        if (requestId != null)
        {
            if (m_serverProgress.RequestIds.Contains(requestId)) return false;
            m_serverProgress.RequestIds.Add(requestId);
        }
        m_data.countTagetValue = (int)Math.Min(ServerIssue.TargetValue, m_data.countTagetValue + 1L);
        SaveServerProgress(); NotifyServerIssueChanged();
        return ServerProgressComplete;
    }
    public async UniTask<GuideQuestClaimRes> ServerClaimAsync()
    {
        if (!ServerProgressComplete || ServerClaiming) return null;
        ServerClaiming = true; NotifyServerIssueChanged();
        var uid = GameServer.Uid;
        var issueId = ServerIssue.IssueId;
        if (string.IsNullOrEmpty(m_serverProgress.ClaimRequestId))
        {
            m_serverProgress.ClaimRequestId = Guid.NewGuid().ToString();
            var stage = StageManager.instance?.recordData;
            m_serverProgress.ReachedStage = $"{Math.Max(1, stage?.chapterNumber ?? 1)}-{Math.Max(1, stage?.stageNumber ?? 1)}";
            SaveServerProgress();
        }
        var options = GameServer.Options(); options.RequestId = m_serverProgress.ClaimRequestId;
        try
        {
            var result = (await GameServer.GuideQuest.ClaimAsync(new GuideQuestClaimReq
            { IssueId = issueId, ReachedStage = m_serverProgress.ReachedStage }, options)).Data;
            if (uid != GameServer.Uid) return null;
            ServerState.ApplyAsset(result.Asset); ServerState.ApplyItems(result.ItemUpdates);
            await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
            ApplyServerLobby(result.GuideQuest);
            Debug.Log($"[GUIDE_CLAIM] uid={uid} issue={issueId} event={result.EventId}");
            return result;
        }
        catch (GameServerException error)
        {
            if (error.Code == "GUIDE_QUEST_STATE_CHANGED") await ServerRefreshAsync();
            throw;
        }
        finally { ServerClaiming = false; NotifyServerIssueChanged(); }
    }

    void OnServerRequestSucceeded(string path, object request, JToken response, string requestId)
    {
        if (!HasServerIssue || ServerClaiming || path.Contains("guide-quest")) return;
        string key = null;
        if (path == "/client/private/daily-dungeon/enter") key = "daily_dungeon_play";
        else if (path == "/client/private/tournament/start" || path == "/client/private/tournament/revenge") key = "tournament_play";
        else if (path == "/client/private/raid/join" && ((string)response?["round"]?["phase"] == "normal" || (string)response?["round"]?["phase"] == "jin")) key = "raid_play";
        else if (path == "/client/private/character/set-position") key = "change_position";
        else if (path == "/client/private/character/relic/enhance") key = "relic_enhance";
        else if (path == "/client/private/character/treasure/set-equipped" && JObject.FromObject(request)["equipped_treasure_ids"] is JArray treasures && treasures.Count > 0) key = "equip_treasure";
        else if (path == "/client/private/castle/thief/capture") key = "thief_catch";
        else if (path == "/client/private/castle/office/refresh") key = "office_mission_refresh";
        else if (path == "/client/private/castle/office/start") key = "office_mission_progress";
        else if (path == "/client/private/castle/building/set-characters" || path == "/client/private/castle/building/collect")
        {
            var body = JObject.FromObject(request);
            var id = (long?)body["building_id"];
            var building = GameServer.TableRows("s_building").OfType<JObject>().FirstOrDefault(row => (long?)row["idx"] == id);
            var buildingKey = (string)building?["key"];
            if (path.EndsWith("set-characters", StringComparison.Ordinal) && body["character_ids"] is JArray characters && characters.Count > 0) key = buildingKey + "_deploy";
            else if (path.EndsWith("collect", StringComparison.Ordinal)) key = buildingKey == "farm" ? "farm_rice_earn" : buildingKey == "market" ? "market_gold_earn" : null;
        }
        if (key != null) ServerRecordProgress(key, requestId);
    }

    [Serializable]
    public sealed class GuideProgressCache
    {
        public int Count;
        public List<string> RequestIds = new List<string>();
        public string ClaimRequestId;
        public string ReachedStage;
    }
}
