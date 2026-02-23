using System.Collections.Generic;
using UnityEngine;

public class Table_StringHero : BaseTable<string, TableStringData>
{
    public Table_StringHero(List<TableStringData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public string GetString(string _key, params string[] _args)
    {
        if (_args.Length > 0)
            return GetStringFormat(_key, _args);

        var db = Get(_key);

        if (db.isActive == false)
            return _key;

        return DataManager.option.language switch {
            LanguageType.English => db.en,
            _ => db.kr };
    }

    public string GetStringFormat(string _key, params string[] _args)
    {
        if (Exists(_key) == false)
            return _key;

        return string.Format(GetString(_key), _args);
    }
}

public struct TableStringData
{
    public string key;
    public string kr;
    public string en;

    public bool isActive => key.IsNullOrEmpty() == false;
}