using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

public class Table_Traits : BaseTable<TraitsType, TableTraitsData>
{
    public Table_Traits(List<TableTraitsData> _table) : base(_table)
    {
        SetDictionary(x => x.type);
    }
}

public class Table_TraitsValue : BaseTable<TraitsType, TableTraitsValueData>
{
    Dictionary<TraitsType, List<TableTraitsValueData>> m_group;

    public Table_TraitsValue(List<TableTraitsValueData> _table) : base(_table)
    {
        m_group = _table.GroupBy(x => x.type).ToDictionary(x => x.Key, x => x.ToList());
    }

    public TableTraitsValueData GetTraitsValueData(TraitsType _type, int _index)
        => m_group[_type][_index];

    public int GetGroupRandomIndex(TraitsType _type)
    {
        var group = m_group[_type];
        return UnityEngine.Random.Range(0, group.Count - 1);
    }

    public BattleStatData GetStatData(TraitsType _type, int _index)
    {
        var data = m_group[_type][_index];

        BattleStatData result = new();

        switch (_type)
        {
            case TraitsType.attack_power: result.statType = BattleStatType.attack_power; break;
            case TraitsType.defence: result.statType = BattleStatType.defence; break;
            case TraitsType.attack_speed: result.statType = BattleStatType.attack_speed; break;
            case TraitsType.health_max: result.statType = BattleStatType.health_max; break;
            case TraitsType.move_speed: result.statType = BattleStatType.move_speed; break;
            case TraitsType.life_steal: result.statType = BattleStatType.life_steal; break;
            case TraitsType.critical_rate: result.statType = BattleStatType.critical_rate; break;
            case TraitsType.cooldown_rate: result.statType = BattleStatType.cooldown_rate; break;
            case TraitsType.critical_damage: result.statType = BattleStatType.critical_damage; break;
            case TraitsType.boss_damage: result.statType = BattleStatType.boss_damage; break;
            default: return result;
        }

        result.value = data.values[0];

        return result;
    }
    public (CoreStatType coreStat, int value) GetCoreStatData(TraitsType _type, int _index)
    {
        var data = m_group[_type][_index];
        (CoreStatType coreStat, int value) result = new();

        switch (_type)
        {
            case TraitsType.leadership:
                result.coreStat = CoreStatType.Leadership;
                break;
            case TraitsType.strength:
                result.coreStat = CoreStatType.Strength;
                break;
            case TraitsType.intellect:
                result.coreStat = CoreStatType.Intellect;
                break;
            case TraitsType.politics:
                result.coreStat = CoreStatType.Politics;
                break;
            case TraitsType.charisma:
                result.coreStat = CoreStatType.Charisma;
                break;
            default:
                return result;
        }

        result.value = data.values[0];
        return result;
    }
}

public enum TraitsType
{
    NONE,

    leadership,                                      // 통솔
    strength,                                        // 무력
    intellect,                                       // 지력
    politics,                                        // 정치
    charisma,                                        // 매력
    attack_power,                                    // 공격력
    defence,                                         // 방어력
    attack_speed,                                    // 공격속도
    health_max,                                      // 체력
    move_speed,                                      // 이동속도
    life_steal,                                      // 체력흡수
    critical_rate,                                   // 치명타확률
    cooldown_rate,                                   // 쿨타임감소
    critical_damage,                                 // 치명타위력
    boss_damage,                                     // 보스피해량
    combat_status_buff,                              // 전체 전투 능력치 +{0}% 증가
    atk_effect_lightning,                            // 공격 시 {0}%확률로 {1}피해를 주는 번개 발동
    atk_effect_bleeding,                             // 공격 시 {0}%확률로 초당 {1} 피해를 주는 출혈 상태 부여 (3초간 유지)
    atk_effect_burn,                                 // 공격 시 {0}%확률로 초당 {1} 피해를 주는 화상 상태 부여 (3초간 유지)
    atk_effect_poisoning,                            // 공격 시 {0}%확률로 초당 {1} 피해를 주는 중독 상태 부여 (3초간 유지)
    atk_effect_stun,                                 // 공격 시 {0}%확률로 스턴 상태 부여 (2초간 유지)
    atk_effect_silence,                              // 공격 시 {0}%확률로 침묵 상태 부여 (2초간 유지)
    atk_effect_aoe,                                  // 공격시 {0}%확률로 범위 공격 발동
    def_effect_shield,                               // 피격 시 {0}%확률로 {1}피해 감소 보호막 발동 (3초간 유지)
    def_effect_damage_taken_reduced,                 // 피격 시 {0}%확률로 받는 피해량 {1}% 감소
}

public class TableTraitsData
{
    public string key;
    [JsonProperty] int? is_legend;

    // custom
    TraitsType m_type;
    public TraitsType type
    {
        get
        {
            if (m_type == TraitsType.NONE)
                m_type = Enum.Parse<TraitsType>(key);
            return m_type;
        }
    }

    public bool isLegend => is_legend == 1;
}
public class TableTraitsValueData
{
    public string key;
    [JsonProperty] string value;
    public GradeType grade;

    //custom
    TraitsType m_type;
    public TraitsType type
    {
        get
        {
            if (m_type == TraitsType.NONE)
                m_type = Enum.Parse<TraitsType>(key);
            return m_type;
        }
    }

    int[] m_values;
    public int[] values
    {
        get
        {
            if (m_values == null)
                m_values = value.Replace(" ", "").Split(",").Select(x => int.Parse(x)).ToArray();
            return m_values;
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class HeroTraitsData
{
    [JsonProperty] public int index;
    [JsonProperty] public TraitsType type;
    [JsonProperty] public int indexValue;
    [JsonProperty] public bool isLock;

    TableTraitsValueData m_traitsValueData;
    public TableTraitsValueData traitsValueData
    {
        get
        {
            if (m_traitsValueData == null)
                m_traitsValueData = TableManager.traitsValue.GetTraitsValueData(type, indexValue);
            return m_traitsValueData;
        }
    }

    public string stringValue
    {
        get
        {
            string result = $"+{traitsValueData.values[0]}";

            switch (type)
            {
                case TraitsType.leadership:
                case TraitsType.strength:
                case TraitsType.intellect:
                case TraitsType.politics:
                case TraitsType.charisma:
                    break;
                default:
                    result += "%";
                    break;
            }

            return result;
        }
    }

    public void ResetTraitsValueData()
        => m_traitsValueData = null;
}
