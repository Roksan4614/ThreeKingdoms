using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

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

    public TableHeroData GetHeroData(string _key)
    {
        return Exists(_key) ? m_dictionary[_key] : default;
    }
}

public struct TableHeroData
{
    public string key;

    public HeroClassType classType;
    public RegionType regionType;

    public int attackPower;

    public int healthMax;
    public int health;

    public float moveSpeed;
    public float attackSpeed;

    public float skillCooltime;
    public float cooldown;

    public float percent_startCooltime; //챕터 시작하면 쿨타임 몇퍼부터 시작할지 여부

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
        percent_startCooltime = percent_startCooltime == 0 ? .8f : percent_startCooltime;
        skillCooltime = skillCooltime == 0 ? 15 : skillCooltime;

        health = healthMax;
    }

    public bool isActive => key.IsNullOrEmpty() == false;
}

public struct HeroInfoData
{
    public string key;
    public GradeType grade;
    public string skin;
    public int enchantLevel;
    public bool isBatch;
    public bool isMain;
    public bool isMine;

    public HeroInfoData(string _key, GradeType _grade = GradeType.Normal, string _skin = null, int _enchantLevel = 0, bool _isBatch = false, bool _isMain = false, bool _isMine = true)
    {
        key = _key;
        grade = _grade;
        skin = _skin.IsNullOrEmpty() ? key : _skin;
        enchantLevel = _enchantLevel;
        isBatch = _isBatch;
        isMain = _isMain;
        isMine = _isMine;
    }

    public bool isActive => key.IsNullOrEmpty() == false;
    // TODO
    public string name
        => key;

}