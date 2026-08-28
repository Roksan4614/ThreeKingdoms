using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        => _data == null ? null : GetStatData(_data.key, _data.grade, _data.enchantLevel);

    public TableStatData GetStatData(string _key, GradeType _grade = GradeType.Normal, int _encahntLevel = 0)
    {
        if (Exists(_key) == false)
            return default;

        var data = m_dictionary[_key].DeepClone();

        if (_grade > GradeType.Normal || _encahntLevel > 0)
        {
            float percent = Mathf.Pow(2, (int)_grade);
            percent *= Mathf.Pow(2, _encahntLevel * 0.0926f);

            data.SetMulitipleStat(percent);
        }

        return data;
    }
}

[Serializable]
public class TableStatData
{
    public string key;
    [JsonProperty, SerializeField] float attack_power;
    [JsonProperty, SerializeField] float defence;
    [JsonProperty, SerializeField] float critical_damage;
    [JsonProperty, SerializeField] float critical_rate;
    [JsonProperty, SerializeField] long health_max;
    [JsonProperty, SerializeField] float move_speed;
    [JsonProperty, SerializeField] float attack_speed;
    [JsonProperty, SerializeField] float cooldown_rate;
    [JsonProperty, SerializeField] float life_steal;
    [JsonProperty, SerializeField] float boss_damage;

    public float dashCooldown { get; set; }
    public float dashCooldownRate { get; set; }
    public long health { get; set; }

    public IReadOnlyList<BattleStatData> battleStat
    {
        get
        {
            List<BattleStatData> result = new();

            result.Add(new() { statType = BattleStatType.attack_power, value = attack_power });
            result.Add(new() { statType = BattleStatType.defence, value = defence });
            result.Add(new() { statType = BattleStatType.health_max, value = health_max });
            result.Add(new() { statType = BattleStatType.attack_speed, value = attack_speed });
            result.Add(new() { statType = BattleStatType.move_speed, value = move_speed });
            result.Add(new() { statType = BattleStatType.critical_damage, value = critical_damage });
            result.Add(new() { statType = BattleStatType.critical_rate, value = critical_rate });
            result.Add(new() { statType = BattleStatType.cooldown_rate, value = cooldown_rate });
            result.Add(new() { statType = BattleStatType.life_steal, value = life_steal });
            result.Add(new() { statType = BattleStatType.boss_damage, value = boss_damage });

            return result;
        }
    }

    public IReadOnlyDictionary<BattleStatType, BattleStatData> GetCompareResult(TableStatData _statData)
    {
        var orinData = battleStat;
        var nextData = _statData.battleStat.ToDictionary(x => x.statType, x => x);

        var result = new Dictionary<BattleStatType, BattleStatData>();

        foreach (var s in orinData)
        {
            var nd = nextData[s.statType];
            if (s.value.Approximately(nd.value) == false)
            {
                result.Add(s.statType, nd);
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
            case BattleStatType.health_max: health = health_max = (long)_value; break;
            case BattleStatType.move_speed: move_speed = _value; break;
            case BattleStatType.attack_speed: attack_speed = _value; break;
            case BattleStatType.cooldown_rate: cooldown_rate = _value; break;
            case BattleStatType.life_steal: life_steal = _value; break;
            case BattleStatType.boss_damage: boss_damage = _value; break;
        }
    }

    public float GetStatData(BattleStatType _battleStatType)
    {
        switch (_battleStatType)
        {
            case BattleStatType.attack_power: return attack_power;
            case BattleStatType.defence: return defence;
            case BattleStatType.critical_damage: return critical_damage;
            case BattleStatType.critical_rate: return critical_rate;
            case BattleStatType.health_max: return health_max;
            case BattleStatType.move_speed: return move_speed;
            case BattleStatType.attack_speed: return attack_speed;
            case BattleStatType.cooldown_rate: return cooldown_rate;
            case BattleStatType.life_steal: return life_steal;
            case BattleStatType.boss_damage: return boss_damage;
        }
        return 0;
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
        health = health_max = (long)(health_max * _percent);
    }

    // 어디서 가져다 쓰는지 확인하기 위해
    public float attackPower
    {
        get => attack_power;
        set => attack_power = Math.Max(1, value);
    }

    public float defenceValue
    {
        get => defence;
        set => defence = value;
    }

    public float attackSpeed
    {
        get => attack_speed;
        set => attack_speed = value;
    }

    public long healthMax
    {
        get => (long)health_max;
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
    public float criticalRate
    {
        get => critical_rate;
        set => critical_rate = value;
    }
    public float lifeSteel
    {
        get => life_steal;
        set => life_steal = value;
    }

    public float percentHP => (float)(health / (double)healthMax);
}
