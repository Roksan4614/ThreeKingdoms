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
    public static Table_Hero enemy { get; private set; }
    public static Table_Region region { get; private set; }
    public static Table_Item item { get; private set; }
    public static Table_Scenario scenario { get; private set; }
    public static Table_String stringTable { get; private set; }
    public static Table_String stringHero { get; private set; }
    public static Table_StringTalk scenarioTalk { get; private set; }

    public async UniTask InitializeAsync()
    {
        await AddressableManager.instance.LoadAssetAsync<TextAsset>(_result =>
        {
            hero = new(LoadList<TableHeroData>(_result, "HeroData"));
            enemy = new(LoadList<TableHeroData>(_result, "EnemyData"));
            item = new(LoadList<TableItemData>(_result, "ItemData"));
            scenario = new(LoadList<TableScenarioData>(_result, "ScenarioData"));
            region = new(LoadList<TableRegionData>(_result, "RegionData"));

            stringTable = new(LoadList<TableStringData>(_result, "String"));
            stringHero = new(LoadList<TableStringData>(_result, "String_Hero"));
            scenarioTalk = new(LoadList<TableStringData>(_result, "String_ScenarioTalk"));

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
            return Newtonsoft.Json.JsonConvert.DeserializeObject<SerializeData<T>>(_data[_key].Result.ToString()).Data.ToList();
    }

    [Serializable]
    public class SerializeData<T>
    {
        public T[] Data;
    }
}
