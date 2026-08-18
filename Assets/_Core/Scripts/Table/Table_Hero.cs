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
        return m_list.Where(x => x.is_active_lock == false).ToList();
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
public struct TableHeroData
{
    public string key;

    public HeroClassType classType;
    public RegionType regionType;
    public bool is_active_lock;

    [JsonProperty] bool is_lock; public bool isLock => is_lock;
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

    public bool isActive => key.IsActive();
    public float percetnStartCooldown => percent_start_cooldown;
    public float skillCooltime => skill_cooltime;

    public string regionKey => $"{regionType}_{key}".ToUpper();
    public string name => TableManager.stringHero.GetString($"NAME_{regionKey}");
    public string talk => TableManager.stringHero.GetString("DESC_TALK_" + regionKey);
}

[JsonObject(MemberSerialization.OptIn)]
public struct HeroInfoData
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
    [JsonProperty] public TableStatData? statData;

    [JsonProperty] HeroClassType m_classType;
    [JsonProperty] RegionType m_regionType;
    public HeroClassType classType => m_classType;
    public RegionType regionType => m_regionType;

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

        if (db.isActive == false)
            IngameLog.Add("HERO INFO DATA FAILED: " + _key);

        m_classType = db.classType;
        m_regionType = db.regionType;
    }

    public bool isActive => key.IsActive();
    public string regionKey => $"{m_regionType}_{key}".ToUpper();
    public string name => TableManager.stringHero.GetString($"NAME_{regionKey}");
    public string gradeName => TableManager.stringTable.GetGradeType(grade);
    public string className => TableManager.stringHero.GetString($"CLASSTYPE_" + m_classType.ToString().ToUpper());
    public string gradeClass => $"{gradeName} {className}";
    public string talk => TableManager.stringHero.GetString("DESC_TALK_" + regionKey);

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

    public TableStatData resultStat
        => statData == null ? DataManager.stat.GetResultStat(this) : statData.Value;
}