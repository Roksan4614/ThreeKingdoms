using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Table_Hero : BaseTable<string, TableHeroData>
{
    public Table_Hero(List<TableHeroData> _table) : base(_table)
    {
        // TODO
        TableHeroData d = new()
        {
            key = "Liubei",
            classType = HeroClassType.Commander
        };
        d.SetDefault();
        m_list.Add(d);

        d = new()
        {
            key = "Guanyu",
            classType = HeroClassType.Champion
        }; d.SetDefault();
        m_list.Add(d);

        d = new()
        {
            key = "Zhangfei",
            classType = HeroClassType.Vanguard
        }; d.SetDefault();
        m_list.Add(d);
        d = new()
        {
            key = "Zhayun",
            classType = HeroClassType.Sentinel
        }; d.SetDefault();
        m_list.Add(d);
        d = new()
        {
            key = "Zhugeliang",
            classType = HeroClassType.Strategist
        }; d.SetDefault();
        m_list.Add(d);

        SetDictionary(x => x.key);

        string key = "Zhugeliang";
        var data = m_dictionary[key];
        data.attackPower += 50;
        data.health = data.healthMax = 800;
        data.attackSpeed = 1.3f;
        m_dictionary[key] = data;

        key = "Guanyu";
        data = m_dictionary[key];
        data.health = data.healthMax = 3000;
        m_dictionary[key] = data;

        key = "Zhayun";
        data = m_dictionary[key];
        data.attackSpeed = 0.8f;
        m_dictionary[key] = data;

        key = "Zhangfei";
        data = m_dictionary[key];
        data.attackPower += 10;
        data.health = data.healthMax = 2500;
        m_dictionary[key] = data;
    }

    public TableHeroData GetHeroData(string _key)
    {
        if (Exists(_key))
            return m_dictionary[_key];

        // TODO: test
        TableHeroData newData = new();
        newData.SetDefault();
        newData.key = _key;

        m_dictionary.Add(_key, newData);

        return GetHeroData(_key);
    }
}

public struct TableHeroData
{
    public string key;

    public HeroClassType classType;

    public int attackPower;

    public int healthMax;
    public int health;

    public float moveSpeed;
    public float attackSpeed;

    public float cooltime_skill;

    public float duration_respawn; //사망 후 부활까지 시간

    public float percent_startCooltime; //챕터 시작하면 쿨타임 몇퍼부터 시작할지 여부

    public void SetDefault()
    {
        attackPower = 100;

        attackSpeed = 1;
        moveSpeed = 10;

        health = healthMax = 2000;
        cooltime_skill = 10f;

        duration_respawn = 15;
        percent_startCooltime = 0.8f;
    }
}

public struct HeroInfoData
{
    public string key;
    public HeroGradeType grade;
    public string skin;
    public int enchantLevel;
    public bool isBatch;
    public bool isMain;

    public HeroInfoData(string _key, HeroGradeType _grade = HeroGradeType.Normal, string _skin = null, int _enchantLevel = 0, bool _isBatch = false, bool _isMain = false)
    {

        key = _key;
        grade = _grade;
        skin = _skin.IsNullOrEmpty() ? key : _skin;
        enchantLevel = _enchantLevel;
        isBatch = _isBatch;
        isMain = _isMain;
    }
}