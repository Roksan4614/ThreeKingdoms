using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TableManager
{
    public static TableManager instance { get; private set; } = new();

    public static Table_Hero hero { get; private set; }
    public static Table_Stat statHero { get; private set; }
    public static Table_Stat statEnemy { get; private set; }

    public static Table_Region region { get; private set; }
    public static Table_Item item { get; private set; }
    public static Table_Scenario scenario { get; private set; }

    public static Table_String stringTable { get; private set; }
    public static Table_String stringHero { get; private set; }
    public static Table_StringTalk scenarioTalk { get; private set; }
    public static Table_String stringMission { get; private set; }

    public static Table_Treasure treasure { get; private set; }
    public static Table_FriendShip friendShip { get; private set; }

    public static Table_Castle castle { get; private set; }
    public static Table_CastleRise castleRise { get; private set; }
    public static Table_CastleMission castleMisson { get; private set; }
    public static Table_CastleMission_Grade castleMissonGrade { get; private set; }
    public static Table_CastleMission_Reward castleMissonReward { get; private set; }

    public static Table_Castle_Office_Level castleOfficeLevel { get; private set; }

    public static Dictionary<CastleObjectType, Table_Castle_Effect> castleEffect { get; private set; } = new();

    public async UniTask InitializeAsync()
    {
        await AddressableManager.instance.LoadAssetAsync<TextAsset>(_result =>
        {
            hero = new(LoadList<TableHeroData>(_result, "HeroData"));
            statHero = new(LoadList<TableStatData>(_result, "HeroStatData"));
            statEnemy = new(LoadList<TableStatData>(_result, "EnemyStatData"));
            item = new(LoadList<TableItemData>(_result, "ItemData"));
            scenario = new(LoadList<TableScenarioData>(_result, "ScenarioData"));
            region = new(LoadList<TableRegionData>(_result, "RegionData"));

            stringTable = new(LoadList<TableStringData>(_result, "String"));
            stringHero = new(LoadList<TableStringData>(_result, "String_Hero"));
            scenarioTalk = new(LoadList<TableStringData>(_result, "String_ScenarioTalk"));
            stringMission = new(LoadList<TableStringData>(_result, "String_Mission"));

            // TODO
            treasure = new(LoadList<TableTreasureData>(_result, "Treasure"));
            treasure.InitializeStringTable(new Table_String(LoadList<TableStringData>(_result, "String_Treasure")));
            friendShip = new(new());

            castle = new(LoadList<TableCastleData>(_result, "Castle"));
            castleRise = new(LoadList<TableCastleRiseData>(_result, "CastleRise"));
            castleMisson = new(LoadList<TableCastleMissionData>(_result, "CastleMission"));
            castleMissonGrade = new(LoadList<TableCastleMissionGradeData>(_result, "CastleMissionGrade"));
            castleMissonReward = new(LoadList<TableCastleMissionRewardData>(_result, "CastleMissionReward"));

            castleOfficeLevel = new(LoadList<TableCastleOfficeLevelData>(_result, "CastleOfficeLevel"));

            for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
                castleEffect.Add(i, new(LoadList<TableCastleEffectData>(_result, "Castle" + i)));

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

    [Serializable]
    public class SerializeData<T>
    {
        public T[] Data;
    }
}
