using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Table_Stat : BaseTable<string, TableStatData>
{
    public Table_Stat(List<TableStatData> _table) : base(_table)
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

    public TableStatData GetStatData(string _key, GradeType _grade = GradeType.Normal, int _encahntLevel = 0)
    {
        if (Exists(_key) == false)
            return default;

        var data = m_dictionary[_key];

        if (_grade > GradeType.Normal || _encahntLevel > 0)
        {
            float percent = (float)(_grade);
            percent += (_encahntLevel) * 0.3f;

            data.SetMulitipleStat(percent);
        }

        return data;
    }
}

[Serializable]
public struct TableStatData
{
    public string key;
    [JsonProperty] float attack_power;
    [JsonProperty] float defence_value;
    [JsonProperty] float critical_damage;
    [JsonProperty] float critical_rate;
    [JsonProperty] float health_max;
    [JsonProperty] float move_speed;
    [JsonProperty] float attack_speed;
    [JsonProperty] float skill_cooldown_rate;

    public bool isActive => key.IsActive();
    public float dashCooldown { get; set; }
    public float dashCooldownRate { get; set; }
    public float health { get; set; }

    Dictionary<StatType, float> m_stat;
    IReadOnlyDictionary<StatType, float> stat => m_stat;

    public void SetDefault()
    {
        m_stat = new();

        m_stat.Add(StatType.attack_power, attack_power == 0 ? 100 : attack_power);
        m_stat.Add(StatType.move_speed, move_speed == 0 ? 10 : move_speed);
        m_stat.Add(StatType.attack_speed, attack_speed == 0 ? 1 : attack_speed);
        m_stat.Add(StatType.critical_damage, critical_damage == 0 ? 1.2f : critical_damage);
        m_stat.Add(StatType.critical_rate, critical_rate == 0 ? 0 : critical_rate);
        m_stat.Add(StatType.health_max, health_max == 0 ? 2000 : health_max);
        m_stat.Add(StatType.skill_cooldown_rate, 0);
        m_stat.Add(StatType.life_steal, 0);
        m_stat.Add(StatType.defence, defence_value == 0 ? 100 : defence_value);

        health = healthMax;
    }

    public void SetMulitipleStat(float _percent)
    {
        attackPower *= _percent;
        health = healthMax = health_max * _percent;
    }

    public float attackPower
    {
        get => m_stat[StatType.attack_power];
        set => m_stat[StatType.attack_power] = value;
    }

    public float attackSpeed
    {
        get => m_stat[StatType.attack_speed];
        set => m_stat[StatType.attack_speed] = value;
    }

    public float healthMax
    {
        get => m_stat[StatType.health_max];
        set => m_stat[StatType.health_max] = value;
    }

    public float moveSpeed
    {
        get => m_stat[StatType.move_speed];
        set => m_stat[StatType.move_speed] = value;
    }

    public float skillCooldownRate
    {
        get => m_stat[StatType.skill_cooldown_rate];
        set => m_stat[StatType.skill_cooldown_rate] = value;
    }
    public float criticalDamage
    {
        get => m_stat[StatType.critical_damage];
        set => m_stat[StatType.critical_damage] = value;
    }
}
