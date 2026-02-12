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

    public async UniTask Initialize()
    {
        hero = new(new());

        //await AddressableManager.instance.LoadAsset<TextAsset>(_result =>
        //{
        //    hero = new(LoadList<TableHeroData>(_result, "table_hero"));

        //    foreach (var h in _result)
        //        h.Value.Release();

        //}, null, AddressableLabelType.L_TableData);
    }

    List<T> LoadList<T>(Dictionary<string, AsyncOperationHandle<TextAsset>> _data, string _key)
    {
        var addressableName = $"TableData/{_key}.json";

        if (_data.ContainsKey(addressableName) == false)
        {
            IngameLog.Add("Table: Load Failed: " + addressableName);
            return new();
        }
        else
            return Newtonsoft.Json.JsonConvert.DeserializeObject<SerializeData<T>>(_data[addressableName].Result.ToString()).Data.ToList();
    }

    [Serializable]
    public class SerializeData<T>
    {
        public T[] Data;
    }
}
