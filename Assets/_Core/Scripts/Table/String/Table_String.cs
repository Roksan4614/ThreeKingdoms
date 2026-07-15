using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class Table_String : Table_String_Base
{
    public Table_String(List<TableStringData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public string GetGradeType(GradeType _gradeType, bool _isDifficult = false, bool _isColor = false)
        => _isColor
        ? $"<color=#{Palette.GetHexa_GradeText(_gradeType)}>{GetGradeType(_gradeType, _isDifficult)}</color>"
        : $"{GetString($"GRADE_{(_isDifficult ? "DIFFICULT_" : "")}{_gradeType.ToString().ToUpper()}")}";

    public string GetRegionType(RegionType _regionType, bool _isFull)
        => GetString(_regionType == RegionType.NONE ? "TAB_ALL" : $"REGION_NAME_{(_isFull ? "FULL_" : "")}{_regionType}");

    public string GetBattleStat(BattleStatType _statType)
        => GetString("BATTLESTAT_" + _statType.ToString().ToUpper());

    public string GetHeroPositionType(HeroPositionType _positionType)
        => _positionType.ToString().ToUpper().Split("_").Last(); //GetString("HERO_POSITION_" + _positionType.ToString().ToUpper());
}

public class Table_String_Hero : Table_String_Base
{
    public Table_String_Hero(List<TableStringData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public string GetGradeType(GradeType _gradeType, bool _isDifficult = false, bool _isColor = false)
        => _isColor
        ? $"<color=#{Palette.GetHexa_GradeText(_gradeType)}>{GetGradeType(_gradeType, _isDifficult)}</color>"
        : $"{GetString($"GRADE_{(_isDifficult ? "DIFFICULT_" : "")}{_gradeType.ToString().ToUpper()}")}";

    public string GetHeroName(CharacterName _name)
        => GetHeroName(_name.ToString());

    public string GetHeroName(string _key)
        => TableManager.hero.Get(_key).name;

    public string GetHeroPositionType(HeroPositionType _positionType)
        => _positionType.ToString().ToUpper().Split("_").Last(); //GetString("HERO_POSITION_" + _positionType.ToString().ToUpper());
}
