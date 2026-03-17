using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class Table_String : BaseTable<string, TableStringData>
{
    public Table_String(List<TableStringData> _table) : base(_table)
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

        return db.message;
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
    public string target;

    public bool isActive => key.IsActive();

    public string message =>
        DataManager.option.language switch
        {
            LanguageType.English => en,
            _ => kr
        };

    public string[] talkArray =>
        Regex.Split(message, @"(?<=[.,?!]+\s+)").Where(x => string.IsNullOrWhiteSpace(x) == false).ToArray();
    //message.Split(new string[] { ". ", ", ", "? ", "! " }, System.StringSplitOptions.RemoveEmptyEntries);
}