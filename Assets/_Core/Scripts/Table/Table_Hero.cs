using System;
using System.Collections.Generic;
using UnityEngine;

public class Table_Hero : BaseTable<string, TableHeroData>
{
    public Table_Hero(List<TableHeroData> _table) : base(_table)
    {
        m_list.RemoveAt(0);
        for (int i = 0; i < m_list.Count; i++)
        {
            var data = m_list[i];
            data.SetDefault();
            m_list[i] = data;
        }

        SetDictionary(x => x.key);
    }

    public TableHeroData GetHeroData(string _key, GradeType _grade = GradeType.Normal, int _encahntLevel = 0)
    {
        if (Exists(_key) == false)
            return default;

        var data = m_dictionary[_key];

        if (_grade > GradeType.Normal || _encahntLevel > 0)
        {
            float percent = (float)(_grade);
            percent += (_encahntLevel) * 0.3f;

            data.attackPower = (int)(data.attackPower * percent);
            data.health = data.healthMax = (int)(data.healthMax * percent);
        }

        return data;
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

    public int attackPower;

    public float criticalDamage;

    public int healthMax;
    public int health;

    public float moveSpeed;
    public float attackSpeed;

    public float skillCooldown;
    public float skillCooldownRate;

    public float dashCooldown;
    public float dashCooldownRate;

    public float percent_startCooldownRate; //챕터 시작하면 쿨타임 몇퍼부터 시작할지 여부

    public int LEA;
    public int POW;
    public int INT;
    public int POL;
    public int CHA;

    private List<int> m_stat;
    public List<int> stat
    {
        get
        {
            if (m_stat == null)
                m_stat = new() { LEA, POW, INT, POL, CHA };
            return m_stat;
        }
    }

    public void SetDefault()
    {
        moveSpeed = moveSpeed == 0 ? 10 : moveSpeed;
        attackSpeed = attackSpeed == 0 ? 1 : attackSpeed;
        percent_startCooldownRate = percent_startCooldownRate == 0 ? .8f : percent_startCooldownRate;

        skillCooldown = skillCooldown == 0 ? 15 : skillCooldown;
        skillCooldownRate = 1;

        dashCooldown = dashCooldown == 0 ? 5 : dashCooldown;
        dashCooldownRate = 1;

        criticalDamage = criticalDamage == 0 ? 1.2f : criticalDamage;

        health = healthMax;
    }

    public bool isActive => key.IsActive();
    public string name => TableManager.stringHero.GetString($"NAME_{regionKey}");
    public string regionKey => $"{regionType}_{key}".ToUpper();
    public string talk => TableManager.stringHero.GetString("DESC_TALK_" + regionKey);
}

public struct HeroInfoData
{
    public string key;
    public string skin;
    public GradeType grade;
    public int soulCount;
    public int enchantLevel;
    public bool isBatch;
    public bool isMain;
    public bool isMine;

    public HeroInfoData(string _key, GradeType _grade = GradeType.Normal, string _skin = null,
        int _soulCount = 0, int _enchantLevel = 0, bool _isBatch = false, bool _isMain = false, bool _isMine = true)
    {
        key = _key;
        grade = _grade;
        skin = _skin.IsActive() ? _skin : key;
        soulCount = _soulCount;
        enchantLevel = _enchantLevel;
        isBatch = _isBatch;
        isMain = _isMain;
        isMine = _isMine;
    }

    public bool isActive => key.IsActive();
    public string regionKey => $"{TableManager.hero.Get(key).regionType}_{key}".ToUpper();
    public string name => TableManager.stringHero.GetString($"NAME_{regionKey}");
    public string gradeName => TableManager.stringHero.GetString($"GRADE_" + grade.ToString().ToUpper());
}