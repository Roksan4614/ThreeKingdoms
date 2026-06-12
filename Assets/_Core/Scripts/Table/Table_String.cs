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

    public string GetGradeType(GradeType _gradeType, bool _isDifficult = false)
        => GetString($"GRADE_{(_isDifficult ? "DIFFICULT_" : "")}{_gradeType.ToString().ToUpper()}");

    public string GetRegionType(RegionType _regionType)
        => GetString("REGION_NAME_" + _regionType.ToString().ToUpper());

    public string GetBattleStat(BattleStatType _statType)
        => GetString("BATTLESTAT_" + _statType.ToString().ToUpper());

    public string GetHeroPositionType(HeroPositionType _positionType)
        => _positionType.ToString().ToUpper().Split("_").Last(); //GetString("HERO_POSITION_" + _positionType.ToString().ToUpper());
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