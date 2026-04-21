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

    public TableStatData GetStatData(HeroInfoData _data)
        => GetStatData(_data.key, _data.grade, _data.enchantLevel);

    public TableStatData GetStatData(string _key, GradeType _grade = GradeType.Normal, int _encahntLevel = 0)
    {
        if (Exists(_key) == false)
            return default;

        var data = m_dictionary[_key];

        if (_grade > GradeType.Normal || _encahntLevel > 0)
        {
            float percent = Mathf.Pow(2, (int)_grade);
            percent += (_encahntLevel) * 0.1f;

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
    [JsonProperty] float boss_bonus_damage;

    public bool isActive => key.IsActive();
    public float dashCooldown { get; set; }
    public float dashCooldownRate { get; set; }
    public float health { get; set; }

    public IReadOnlyDictionary<StatType, float> stat
    {
        get
        {
            Dictionary<StatType, float> result = new();

            result.Add(StatType.attack_power, attack_power);
            result.Add(StatType.defence, defence_value);
            result.Add(StatType.health_max, health_max);
            result.Add(StatType.attack_speed, attack_speed);
            result.Add(StatType.move_speed, move_speed);
            result.Add(StatType.critical_damage, critical_damage == 0 ? 1.2f : critical_damage);
            result.Add(StatType.critical_rate, critical_rate);
            result.Add(StatType.skill_cooldown_rate, skill_cooldown_rate);
            result.Add(StatType.life_steal, life_steal);
            result.Add(StatType.boss_bonus_damage, boss_bonus_damage);

            return result;
        }
    }
    public IReadOnlyDictionary<StatType, string> statString
    {
        get
        {
            Dictionary<StatType, string> result = new();

            result.Add(StatType.attack_power, ((int)attack_power).AmountKMBT());
            result.Add(StatType.defence, ((int)defence_value).AmountKMBT());
            result.Add(StatType.health_max, ((int)health_max).AmountKMBT());
            result.Add(StatType.attack_speed, $"{attack_speed:0.0}/s");
            result.Add(StatType.move_speed, $"{(int)(move_speed * 100)}");
            result.Add(StatType.critical_damage, $"{Math.Truncate(critical_damage * 100)}%");
            result.Add(StatType.critical_rate, $"{Math.Truncate(critical_rate * 100)}%");
            result.Add(StatType.skill_cooldown_rate, $"{Math.Truncate(skill_cooldown_rate * 100)}%");
            result.Add(StatType.life_steal, $"{Math.Truncate(life_steal * 100)}%");
            result.Add(StatType.boss_bonus_damage, $"{Math.Truncate(boss_bonus_damage * 100)}%");

            return result;
        }
    }

    public IReadOnlyDictionary<StatType, (float value, string message)> GetCompareResult(TableStatData _statData)
    {
        var orinData = stat;
        var nextData = _statData.stat;

        var result = new Dictionary<StatType, (float value, string message)>();

        foreach (var s in orinData)
        {
            if (s.Value.Approximately(nextData[s.Key]) == false)
            {
                result.Add(s.Key, new() { value = nextData[s.Key], message = _statData.statString[s.Key] });
            }
        }

        return result;
    }

    public void SetStatData(StatType _statType, float _value)
    {
        switch (_statType)
        {
            case StatType.attack_power: attack_power = _value; break;
            case StatType.defence: defence_value = _value; break;
            case StatType.critical_damage: critical_damage = _value; break;
            case StatType.critical_rate: critical_rate = _value; break;
            case StatType.health_max: health_max = _value; break;
            case StatType.move_speed: move_speed = _value; break;
            case StatType.attack_speed: attack_speed = _value; break;
            case StatType.skill_cooldown_rate: skill_cooldown_rate = _value; break;
            case StatType.life_steal: life_steal = _value; break;
            case StatType.boss_bonus_damage: boss_bonus_damage = _value; break;
        }
    }

    public void SetDefault()
    {
        attack_power = attack_power == 0 ? 100 : attack_power;
        defence_value = defence_value == 0 ? 100 : defence_value;
        health_max = health_max == 0 ? 2000 : health_max;
        attack_speed = attack_speed == 0 ? 1 : attack_speed;
        move_speed = move_speed == 0 ? 10 : move_speed;
        life_steal = 0;
        critical_rate = critical_rate == 0 ? 0 : critical_rate;
        skill_cooldown_rate = 0;
        critical_damage = critical_damage == 0 ? 1.2f : critical_damage;
        boss_bonus_damage = 0;

        health = healthMax;
    }

    public void SetMulitipleStat(float _percent)
    {
        attack_power *= _percent;
        defence_value *= _percent;
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
