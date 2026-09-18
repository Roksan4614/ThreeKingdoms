using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Rev9.Pass;
using Rev9.Tournament;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TableManager
{
    public static TableManager instance { get; private set; } = new();

    public static Table_Hero hero => instance.m_hero;
    Table_Hero m_hero;
    public static Table_Hero_Position heroPosition => instance.m_heroPosition;
    Table_Hero_Position m_heroPosition;

    public static Table_Stat statHero => instance.m_statHero;
    Table_Stat m_statHero;
    public static Table_Stat statEnemy => instance.m_statEnemy;
    Table_Stat m_statEnemy;

    public static Table_Region region => instance.m_region;
    Table_Region m_region;

    public static Table_Item item => instance.m_item;
    Table_Item m_item;
    public static Table_String_Item stringItem => instance.m_stringItem;
    Table_String_Item m_stringItem;

    public static Table_String stringTable => instance.m_stringTable;
    Table_String m_stringTable;
    public static Table_String alertString => instance.m_alertString;
    Table_String m_alertString;
    public static Table_String_Hero stringHero => instance.m_stringHero;
    Table_String_Hero m_stringHero;
    public static Table_String_Talk scenarioTalk => instance.m_scenarioTalk;
    Table_String_Talk m_scenarioTalk;
    public static Table_String_Base stringMission => instance.m_stringMission;
    Table_String_Base m_stringMission;
    public static Table_String_Base stringTraits => instance.m_stringTraits;
    Table_String_Base m_stringTraits;

    public static Table_Treasure treasure => instance.m_treasure;
    Table_Treasure m_treasure;

    public static Table_FriendShip friendShip => instance.m_friendShip;
    Table_FriendShip m_friendShip;


    public static Table_Castle castle => instance.m_castle;
    Table_Castle m_castle;
    public static Table_CastleRise castleRise => instance.m_castleRise;
    Table_CastleRise m_castleRise;
    public static Table_CastleMission castleMission => instance.m_castleMission;
    Table_CastleMission m_castleMission;
    public static Table_CastleMission_Grade castleMissionGrade => instance.m_castleMissionGrade;
    Table_CastleMission_Grade m_castleMissionGrade;
    public static Table_CastleMission_Reward castleMissionReward => instance.m_castleMissionReward;
    Table_CastleMission_Reward m_castleMissionReward;
    public static Table_Castle_Office_Level castleOfficeLevel => instance.m_castleOfficeLevel;
    Table_Castle_Office_Level m_castleOfficeLevel;

    public static Table_DailyDungeon_Grade dailyDungeonGrade => instance.m_dailyDungeonGrade;
    Table_DailyDungeon_Grade m_dailyDungeonGrade;
    public static Table_DailyDungeon_Boss dailyDungeonBoss => instance.m_dailyDungeonBoss;
    Table_DailyDungeon_Boss m_dailyDungeonBoss;


    public static Table_StoryMode_Node storyNode => instance.m_storyNode;
    Table_StoryMode_Node m_storyNode;
    public static Table_StoryMode_Unlock storyUnlock => instance.m_storyUnlock;
    Table_StoryMode_Unlock m_storyUnlock;
    public static Table_StoryMode_Choice storyChoice => instance.m_storyChoice;
    Table_StoryMode_Choice m_storyChoice;
    public static Table_String_Story storyString => instance.m_storyString;
    Table_String_Story m_storyString;

    public static Table_GuideQuest guideQuest => instance.m_guideQuest;
    Table_GuideQuest m_guideQuest;
    public static Table_GuideQuest_Repeat guideQuestRepeat => instance.m_guideQuestRepeat;
    Table_GuideQuest_Repeat m_guideQuestRepeat;
    public static Table_String_GuideQuest guideQuestString => instance.m_guideQuestString;
    Table_String_GuideQuest m_guideQuestString;

    public static Table_Quest quest => instance.m_quest;
    Table_Quest m_quest;
    public static Table_QuestReward questReward => instance.m_questReward;
    Table_QuestReward m_questReward;
    public static Table_String_Base questString => instance.m_questString;
    Table_String_Base m_questString;

    public static Table_PassReward passReward => instance.m_passReward;
    Table_PassReward m_passReward;
    public static Table_PassQuest passQuest => instance.m_passQuest;
    Table_PassQuest m_passQuest;

    public static Table_TournamentReward tournamentReward => instance.m_tournamentReward;
    Table_TournamentReward m_tournamentReward;

    public static Table_Traits traits => instance.m_traits;
    Table_Traits m_traits;
    public static Table_TraitsValue traitsValue => instance.m_traitsValue;
    Table_TraitsValue m_traitsValue;

    Dictionary<CastleObjectType, Table_Castle_Effect> m_castleEffect;
    public static Dictionary<CastleObjectType, Table_Castle_Effect> castleEffect => instance.m_castleEffect;

    public async UniTask InitializeAsync()
    {
        await InitializeAsync_CDN();

        await AddressableManager.instance.LoadAssetAsync<TextAsset>(true, _result =>
        {
            m_hero = new(LoadList<TableHeroData>(_result, "s_character"));
            m_heroPosition = new(LoadList<TableHeroPositionData>(_result, "s_position"));
            m_statHero = new(LoadList<TableStatData>(_result, "s_character_stat_data"));
            m_statEnemy = new(LoadList<TableStatData>(_result, "s_enemy_stat_data"));
            m_traits = new(LoadList<TableTraitsData>(_result, "s_traits_pool"));
            m_traitsValue = new(LoadList<TableTraitsValueData>(_result, "s_traits_value_pool"));

            m_item = new(LoadList<TableItemData>(_result, "s_item"));
            m_region = new(LoadList<TableRegionData>(_result, "RegionData"));

            m_stringTable = new(LoadList<TableStringData>(_result, "String"));
            m_alertString = new(LoadList<TableStringData>(_result, "String_Alert"));
            m_stringHero = new(LoadList<TableStringData>(_result, "String_Hero"));
            m_scenarioTalk = new(LoadList<TableStringData>(_result, "String_ScenarioTalk"));
            m_stringMission = new(LoadList<TableStringData>(_result, "String_Mission"));
            m_stringTraits = new(LoadList<TableStringData>(_result, "String_Traits"));
            m_stringItem = new(LoadList<TableStringData>(_result, "String_Item"));

            // TODO
            m_treasure = new(LoadList<TableTreasureData>(_result, "s_treasure"));
            m_treasure.InitializeStringTable(new Table_String(LoadList<TableStringData>(_result, "String_Treasure")));
            m_friendShip = new(new());

            m_castle = new(LoadList<TableCastleData>(_result, "s_building"));
            m_castleRise = new(LoadList<TableCastleRiseData>(_result, "s_building_level"));
            m_castleMission = new(LoadList<TableCastleMissionData>(_result, "s_office_mission"));
            m_castleMissionGrade = new(LoadList<TableCastleMissionGradeData>(_result, "s_office_mission_grade"));
            m_castleMissionReward = new(LoadList<TableCastleMissionRewardData>(_result, "s_office_mission_reward_pool"));
            m_castleOfficeLevel = new(LoadList<TableCastleOfficeLevelData>(_result, "s_office_level"));
            m_castleEffect = new();
            for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
                m_castleEffect.Add(i, new(LoadList<TableCastleEffectData>(_result, $"s_{i.ToString().ToLower()}_effect")));

            m_dailyDungeonGrade = new(LoadList<TableDailyDungeonGradeData>(_result, "s_daily_dungeon_grade"));
            m_dailyDungeonBoss = new(LoadList<TableDailyDungeonBossData>(_result, "s_daily_dungeon_boss"));

            m_storyNode = new(LoadList<Table_StoryMode_Node.TableStoryModeNodeData>(_result, "s_story_node"));
            m_storyUnlock = new(LoadList<Table_StoryMode_Unlock.TableStoryModeUnlockData>(_result, "s_story_node_unlock"));
            m_storyChoice = new(LoadList<Table_StoryMode_Choice.TableStoryModeChoiceData>(_result, "s_story_node_choice"));
            m_storyString = new(LoadList<TableStringData>(_result, "String_Story"));

            m_guideQuest = new(LoadList<Table_GuideQuest.TableGuideQuestData>(_result, "s_guide_quest"));
            m_guideQuestRepeat = new(LoadList<Table_GuideQuest.TableGuideQuestData>(_result, "s_guide_quest_repeat"));
            m_guideQuestString = new(LoadList<TableStringData>(_result, "String_GuideQuest"));

            m_quest = new(LoadList<TableQuestData>(_result, "s_quest"));
            m_questReward = new(LoadList<TableQuestData>(_result, "s_quest_reward"));
            m_questString = new(LoadList<TableStringData>(_result, "String_Quest"));

            m_passReward = new(LoadList<TablePassRewardData>(_result, "s_pass_reward"));
            m_passQuest = new(LoadList<TablePassQuestData>(_result, "s_pass_quest"));

            m_tournamentReward = new(new());

            foreach (var h in _result)
                h.Value.Release();

        }, null, AddressableLabelType.L_TableData);
    }

    List<T> LoadList<T>(Dictionary<string, AsyncOperationHandle<TextAsset>> _data, string _key)
    {
        if (_data.ContainsKey(_key) == false)
        {
            IngameLog.Add("Table: Load Failed: " + _key);
            return new();
        }
        else
        {
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializeData<T>>(_data[_key].Result.ToString()).Data.ToList();
            return result;
        }
    }

    List<T> LoadList<T>(string _key)
    {
        var jsonData = GetTable(_key);
        if (jsonData.IsActive())
        {
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(jsonData);
            return result;
        }
        return new();
    }

    static string m_baseUrl = "https://dev-static.kingz.app/table_data/json/";
    public static void SetBaseURL(string _url) => m_baseUrl = _url;
    public string GetJsonURL(string _fileName) => $"{m_baseUrl}{_fileName}.json";

    string m_pathCache;

    async UniTask<bool> InitializeAsync_CDN()
    {
        if (m_baseUrl.IsActive() == false)
            return false;

        await LoadCategoryAsync();

        if (m_category.isActive == false)
            return false;

        m_pathCache = Path.Combine(Application.persistentDataPath, "TableCache");

        ReadLocalHashCache();

        bool isUpdated = false;
        List<UniTask> tasks = new();
        foreach (var remoteTable in m_category.tables)
            tasks.Add(DownloadTablesAsync(remoteTable.Key, remoteTable.Value, () => isUpdated = true));

        await UniTask.WhenAll(tasks);

        if (isUpdated)
            SaveLocalHashCache();

        return true;
    }

    CategoryHashData m_category;
    CategoryHashData m_localCache;

    async UniTask LoadCategoryAsync()
    {
        if (m_category.isActive == false)
        {
            UnityWebRequest req = UnityWebRequest.Get(GetJsonURL("table_hash"));
            await req.SendWebRequest().ToUniTask();

            if (req.result == UnityWebRequest.Result.Success)
            {
                m_category = JsonConvert.DeserializeObject<CategoryHashData>(req.downloadHandler.text);

                if (m_category.tables == null || m_category.tables.Count == 0)
                    IngameLog.AddError("GetCategoryAsync: FILE NULL");
            }
            else
                IngameLog.AddError($"GetCategoryAsync: FAIL DOWN - {req.url}");
        }
    }

    async UniTask DownloadTablesAsync(string _key, string _hash, Action _onUpdated)
    {
        // 해시가 같다면 넘어가자
        if (m_localCache.tables.ContainsKey(_key) && m_localCache.tables[_key] == _hash)
            return;

        UnityWebRequest req = UnityWebRequest.Get($"{GetJsonURL(_key)}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        await req.SendWebRequest().ToUniTask();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string cachePath = Path.Combine(m_pathCache, $"{_key}.json");
            File.WriteAllText(cachePath, req.downloadHandler.text);

            m_localCache.tables[_key] = _hash;
            _onUpdated();
        }
        else
            IngameLog.Add($"DownloadTablesAsync: FAILED: {_key}: {req.error}");
    }

    void ReadLocalHashCache()
    {
        if (Directory.Exists(m_pathCache) == false)
            Directory.CreateDirectory(m_pathCache);

        string hashCachePath = Path.Combine(m_pathCache, "hash_cache.json");
        if (File.Exists(hashCachePath))
        {
            string hashJson = File.ReadAllText(hashCachePath);
            m_localCache.tables = JsonConvert.DeserializeObject<Dictionary<string, string>>(hashJson);
        }

        if (m_localCache.tables == null)
            m_localCache.tables = new();
    }

    void SaveLocalHashCache()
    {
        string hashCachePath = Path.Combine(m_pathCache, "hash_cache.json");
        string hashJson = JsonConvert.SerializeObject(m_localCache.tables, Formatting.Indented);
        File.WriteAllText(hashCachePath, hashJson);
    }

    string GetTable(string _key)
    {
        string cachePath = Path.Combine(m_pathCache, $"{_key}.json");

        if (File.Exists(cachePath))
            return File.ReadAllText(cachePath);

        IngameLog.Add("GetTable: Not Founded: " + _key);
        return null;
    }

    [Serializable]
    public class SerializeData<T>
    {
        public T[] Data;
    }

    [Serializable]
    public struct CategoryHashData
    {
        public string total_hash;
        public Dictionary<string, string> tables;

        public bool isActive => total_hash.IsActive();
    }
}
