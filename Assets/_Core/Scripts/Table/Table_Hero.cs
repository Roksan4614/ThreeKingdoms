using DG.Tweening.Plugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_Hero : BaseTable<string, TableHeroData>
{
    public List<TableHeroData> GetHeroList()
    {
        return m_list.Where(x => x.is_lock_active == false).ToList();
    }

    public Table_Hero(List<TableHeroData> _table) : base(_table)
    {
        for (int i = 0; i < m_list.Count; i++)
        {
            var data = m_list[i];
            data.SetDefault();
            m_list[i] = data;
        }

        SetDictionary(x => x.key);
    }

    public int GetNeedSoulNextGrade(GradeType _nowGrade)
    {
        if (_nowGrade == GradeType.Legend)
            return 0;

        GradeType next = _nowGrade + 1;

        return GetNeedSoul(_nowGrade + 1) - GetNeedSoul(_nowGrade);
    }

    public int GetNeedSoul(GradeType _grade)
        => _grade switch
        {
            GradeType.Legend => 2560,
            GradeType.Hero => 640,
            GradeType.General => 160,
            GradeType.Elite => 40,
            GradeType.Normal => 10,
            _ => 0
        };

    public GradeType GetGradeFromSoulCount(long _count)
    {
        if (_count >= 2560) return GradeType.Legend;
        if (_count >= 640) return GradeType.Hero;
        if (_count >= 160) return GradeType.General;
        if (_count >= 40) return GradeType.Elite;
        if (_count >= 10) return GradeType.Normal;
        return GradeType.NONE;
    }
}

[Serializable]
public class TableHeroData
{
    public string key;
    
    public bool is_lock_active;

    [JsonProperty] HeroClassType character_class; public HeroClassType classType => character_class;
    [JsonProperty] RegionType country; public RegionType regionType => country;

    [JsonProperty] bool is_lock_summon; public bool isLockSummon => is_lock_summon;
    [JsonProperty] float percent_start_cooldown;
    [JsonProperty] float skill_cooltime;

    [JsonProperty] int LEA;
    [JsonProperty] int STR;
    [JsonProperty] int INT;
    [JsonProperty] int POL;
    [JsonProperty] int CHA;

    private List<int> m_coreStatPoint;
    public List<int> coreStatPoint
    {
        get
        {
            if (m_coreStatPoint == null)
                m_coreStatPoint = new() { LEA, STR, INT, POL, CHA };
            return m_coreStatPoint;
        }
    }

    public void SetDefault()
    {
        skill_cooltime = skill_cooltime == 0 ? 15 : skill_cooltime;
        percent_start_cooldown = percent_start_cooldown == 0 ? .8f : percent_start_cooldown;
    }

    public float percentStartCooldown => percent_start_cooldown;
    public float skillCooltime => skill_cooltime;

    public string regionKey => $"{regionType}_{key}".ToUpper();
    public string name => TableManager.stringHero.GetString($"NAME_{regionKey}");
    public string talk => TableManager.stringHero.GetString("DESC_TALK_" + regionKey);
}

[JsonObject(MemberSerialization.OptIn)]
public class HeroInfoData
{
    [JsonProperty] public string key;
    [JsonProperty] public string skin;
    [JsonProperty] public GradeType grade;
    [JsonProperty] public HeroPositionType positionType;
    [JsonProperty] public int soulCount;
    [JsonProperty] public int enchantLevel;
    [JsonProperty] public int relicLevel;
    [JsonProperty] public bool isBatch;
    [JsonProperty] public bool isMain;
    [JsonProperty] public bool isMine;
    [JsonProperty] public TableStatData statData;
    [JsonProperty] public List<HeroTraitsData> traits;

    [JsonProperty] public bool isTournament;
    [JsonProperty] public bool isTournament_Attack;

    [JsonProperty] HeroClassType m_classType;
    [JsonProperty] RegionType m_regionType;
    public HeroClassType classType => m_classType;
    public RegionType regionType => m_regionType;

    public HeroInfoData() { }
    public HeroInfoData(string _key, GradeType _grade = GradeType.Normal, HeroPositionType _heroPositionType = HeroPositionType.NONE, string _skin = null,
        int _soulCount = 0, int _enchantLevel = 0, int _relicLevel = 0, bool _isBatch = false, bool _isMain = false, bool _isMine = true, int _sortIdx = 0, TableStatData? _statData = null)
    {
        key = _key;
        grade = _grade;
        positionType = _heroPositionType;
        skin = _skin.IsActive() ? _skin : key;
        soulCount = _soulCount;
        enchantLevel = _enchantLevel;
        relicLevel = _relicLevel;
        isBatch = _isBatch;
        isMain = _isMain;
        isMine = _isMine;
        sortIdx = _sortIdx;
        statData = _statData;

        var db = TableManager.hero.Get(_key);

        if (db == null)
        {
            IngameLog.Add("HERO INFO DATA FAILED: " + _key);
        }
        else
        {
            m_classType = db.classType;
            m_regionType = db.regionType;
        }
    }

    public string regionKey => $"{m_regionType}_{key}".ToUpper();
    public string name => TableManager.stringHero.GetString($"NAME_{regionKey}");
    public string gradeName => TableManager.stringTable.GetGradeType(grade);
    public string className => TableManager.stringHero.GetString($"CLASSTYPE_" + m_classType.ToString().ToUpper());
    public string gradeClass => $"{gradeName} {className}";
    public string talk => TableManager.stringHero.GetString("DESC_TALK_" + regionKey);
    public string fullNameGradeLevel => $"[{gradeName}] {name}{(enchantLevel == 0 ? "" : $"+{enchantLevel}")}";

    [JsonProperty] public int sortIdx { get; set; }

    public Dictionary<CoreStatType, int> resultCoreStat
    {
        get
        {
            Dictionary<CoreStatType, int> result = new();

            var heroData = TableManager.hero.Get(key);
            for (int i = 0; i < heroData.coreStatPoint.Count; i++)
            {
                var statType = (CoreStatType)i;
                result[statType] = heroData.coreStatPoint[i] + (int)(grade) * 10 + enchantLevel;
            }

            if (traits != null)
            {
                foreach (var t in traits)
                {
                    var traitData = TableManager.traitsValue.GetCoreStatData(t.type, t.indexValue);

                    if (result.ContainsKey(traitData.coreStat))
                        result[traitData.coreStat] += traitData.value;
                }
            }

            return result;
        }
    }

    public long power
    {
        get
        {
            long result = 0;

            var stat = resultStat;

            int attackPoint = (int)(stat.attackPower * stat.attackSpeed);
            attackPoint = attackPoint + (int)(attackPoint * (stat.criticalRate + stat.cooldownRate + stat.lifeSteel));

            int defencePoint = (int)(stat.defenceValue + stat.healthMax + stat.moveSpeed);
            //defencePoint = defencePoint + (int)(defencePoint * stat.moveSpeed);

            result = attackPoint + defencePoint;

            return result;
        }
    }

    TableStatData m_resultStat;
    public TableStatData resultStat
    {
        get
        {
            if (isMine == true)
            {
                if (m_resultStat == null)
                    m_resultStat = DataManager.stat.GetResultStat(this);
                return m_resultStat;
            }
            else if (statData == null)
                statData = TableManager.statHero.GetStatData(this);
            return statData;
        }
    }

    public void ResetResultStat() => m_resultStat = null;

    public int countOpenTraits => grade < GradeType.General ? 0 : 3 - (GradeType.Legend - grade);
}