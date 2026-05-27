using System.Collections.Generic;
using UnityEngine;

public class Table_Castle_Effect : BaseTable<int, TableCastleEffectData>
{
    public Table_Castle_Effect(List<TableCastleEffectData> _data) : base(_data)
    {
        SetDictionary(x => x.level);
    }

    public float GetAmountPerCeconds(Data_Castle.CastleData _castleData)
        => _castleData.type == CastleObjectType.Farm ?
        m_dictionary[_castleData.level].rice_per_sec_base ?? 0 :
        m_dictionary[_castleData.level].gold_per_sec_base ?? 0;
    public int GetMaxAmount(Data_Castle.CastleData _castleData)
        => _castleData.type == CastleObjectType.Farm ?
        m_dictionary[_castleData.level].rice_storage_base ?? 0 :
        m_dictionary[_castleData.level].gold_storage_base ?? 0;
}

public struct TableCastleEffectData
{
    public int level;

    // 궁성
    public int? level_cap;
    public int? time_stone_sec;
    public int? ad_reduce_min;

    // 농지
    public float? rice_per_sec_base;
    public int? rice_storage_base;

    // 상점
    public float? gold_per_sec_base;
    public int? gold_storage_base;

    // 관아
    public int? mission_count;
    public int? normal_rate;
    public int? master_rate;
    public int? legend_rate;

    // 행상
    public float? discount_rate_base;
    public float? grade_up_rate_base;
    public int? item_count;

    // 성문
    public int? npc_duration_sec;
}