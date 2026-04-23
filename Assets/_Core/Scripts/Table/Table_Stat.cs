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
    [JsonProperty] float defence;
    [JsonProperty] float critical_damage;
    [JsonProperty] float critical_rate;
    [JsonProperty] float health_max;
    [JsonProperty] float move_speed;
    [JsonProperty] float attack_speed;
    [JsonProperty] float cooldown_rate;
    [JsonProperty] float life_steal;
    [JsonProperty] float boss_damage;

    public bool isActive => key.IsActive();
    public float dashCooldown { get; set; }
    public float dashCooldownRate { get; set; }
    public float health { get; set; }

    public IReadOnlyDictionary<BattleStatType, float> stat
    {
        get
        {
            Dictionary<BattleStatType, float> result = new();

            result.Add(BattleStatType.attack_power, attack_power);
            result.Add(BattleStatType.defence, defence);
            result.Add(BattleStatType.health_max, health_max);
            result.Add(BattleStatType.attack_speed, attack_speed);
            result.Add(BattleStatType.move_speed, move_speed);
            result.Add(BattleStatType.critical_damage, critical_damage == 0 ? 1.2f : critical_damage);
            result.Add(BattleStatType.critical_rate, critical_rate);
            result.Add(BattleStatType.cooldown_rate, cooldown_rate);
            result.Add(BattleStatType.life_steal, life_steal);
            result.Add(BattleStatType.boss_damage, boss_damage);

            return result;
        }
    }
    public IReadOnlyDictionary<BattleStatType, string> statString
    {
        get
        {
            Dictionary<BattleStatType, string> result = new();

            result.Add(BattleStatType.attack_power, Mathf.RoundToInt(attack_power).AmountKMBT());
            result.Add(BattleStatType.defence, Mathf.RoundToInt(defence).AmountKMBT());
            result.Add(BattleStatType.health_max, Mathf.RoundToInt(health_max).AmountKMBT());
            result.Add(BattleStatType.attack_speed, $"{attack_speed:0.0}/s");
            result.Add(BattleStatType.move_speed, $"{Mathf.RoundToInt(move_speed * 100)}");
            result.Add(BattleStatType.critical_damage, $"{Math.Truncate(critical_damage * 100)}%");
            result.Add(BattleStatType.critical_rate, $"{Math.Truncate(critical_rate * 100)}%");
            result.Add(BattleStatType.cooldown_rate, $"{Math.Truncate(cooldown_rate * 100)}%");
            result.Add(BattleStatType.life_steal, $"{Math.Truncate(life_steal * 100)}%");
            result.Add(BattleStatType.boss_damage, $"{Math.Truncate(boss_damage * 100)}%");

            return result;
        }
    }

    public IReadOnlyDictionary<BattleStatType, (float value, string message)> GetCompareResult(TableStatData _statData)
    {
        var orinData = stat;
        var nextData = _statData.stat;

        var result = new Dictionary<BattleStatType, (float value, string message)>();

        foreach (var s in orinData)
        {
            if (s.Value.Approximately(nextData[s.Key]) == false)
            {
                result.Add(s.Key, new() { value = nextData[s.Key], message = _statData.statString[s.Key] });
            }
        }

        return result;
    }

    public void SetStatData(BattleStatType _battleStatType, float _value)
    {
        switch (_battleStatType)
        {
            case BattleStatType.attack_power: attack_power = _value; break;
            case BattleStatType.defence: defence = _value; break;
            case BattleStatType.critical_damage: critical_damage = _value; break;
            case BattleStatType.critical_rate: critical_rate = _value; break;
            case BattleStatType.health_max: health_max = _value; break;
            case BattleStatType.move_speed: move_speed = _value; break;
            case BattleStatType.attack_speed: attack_speed = _value; break;
            case BattleStatType.cooldown_rate: cooldown_rate = _value; break;
            case BattleStatType.life_steal: life_steal = _value; break;
            case BattleStatType.boss_damage: boss_damage = _value; break;
        }
    }

    public void SetDefault()
    {
        attack_power = attack_power == 0 ? 100 : attack_power;
        defence = defence == 0 ? 100 : defence;
        health_max = health_max == 0 ? 2000 : health_max;
        attack_speed = attack_speed == 0 ? 1 : attack_speed;
        move_speed = move_speed == 0 ? 10 : move_speed;
        life_steal = 0;
        critical_rate = critical_rate == 0 ? 0 : critical_rate;
        cooldown_rate = 0;
        critical_damage = critical_damage == 0 ? 1.2f : critical_damage;
        boss_damage = 0;

        health = healthMax;
    }

    public void SetMulitipleStat(float _percent)
    {
        attack_power *= _percent;
        defence *= _percent;
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

    public float cooldownRate
    {
        get => cooldown_rate;
        set => cooldown_rate = value;
    }
    public float criticalDamage
    {
        get => critical_damage;
        set => critical_damage = value;
    }
}
