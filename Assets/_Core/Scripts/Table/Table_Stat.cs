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
    [JsonProperty] float life_steal;

    public bool isActive => key.IsActive();
    public float dashCooldown { get; set; }
    public float dashCooldownRate { get; set; }
    public float health { get; set; }

    IReadOnlyDictionary<StatType, float> stat
    {
        get {
            Dictionary<StatType, float> result = new();

            result.Add(StatType.attack_power, attack_power);
            result.Add(StatType.move_speed, move_speed);
            result.Add(StatType.attack_speed, attack_speed);
            result.Add(StatType.critical_damage, critical_damage == 0 ? 1.2f : critical_damage);
            result.Add(StatType.critical_rate, critical_rate);
            result.Add(StatType.health_max, health_max);
            result.Add(StatType.skill_cooldown_rate, skill_cooldown_rate);
            result.Add(StatType.life_steal, life_steal);
            result.Add(StatType.defence, defence_value);

            return result;
        }
    }

    public void SetDefault()
    {
        attack_power = attack_power == 0 ? 100 : attack_power;
        move_speed = move_speed == 0 ? 10 : move_speed;
        attack_speed = attack_speed == 0 ? 1 : attack_speed;
        critical_damage = critical_damage == 0 ? 1.2f : critical_damage;
        critical_rate = critical_rate == 0 ? 0 : critical_rate;
        health_max = health_max == 0 ? 2000 : health_max;
        skill_cooldown_rate = 0;
        life_steal = 0;
        defence_value = defence_value == 0 ? 100 : defence_value;

        health = healthMax;
    }

    public void SetMulitipleStat(float _percent)
    {
        attack_power *= _percent;
        health = health_max = health_max * _percent;
    }

    // 어디서 가져다 쓰는지 확인하기 위해
    public float attackPower
    {
        get => attack_power;
        set => attack_power = Math.Max(1, value);
    }

    public float attackSpeed
    {
        get => attack_speed;
        set => attack_speed = value;
    }

    public float healthMax
    {
        get => health_max;
        set => health_max = value;
    }

    public float moveSpeed
    {
        get => move_speed;
        set => move_speed = value;
    }

    public float skillCooldownRate
    {
        get => skill_cooldown_rate;
        set => skill_cooldown_rate = value;
    }
    public float criticalDamage
    {
        get => critical_damage;
        set => critical_damage = value;
    }
}
