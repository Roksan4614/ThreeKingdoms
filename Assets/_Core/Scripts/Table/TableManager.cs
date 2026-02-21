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
    public static Table_StringHero stringHero { get; private set; }

    public async UniTask Initialize()
    {
        await AddressableManager.instance.LoadAssetAsync<TextAsset>(_result =>
        {
            hero = new(LoadList<TableHeroData>(_result, "HeroData"));
            enemy = new(LoadList<TableHeroData>(_result, "EnemyData"));
            stringHero = new(LoadList<TableStringData>(_result, "String_Hero"));

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
