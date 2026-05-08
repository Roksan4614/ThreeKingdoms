using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_CastleRise : BaseTable<string, TableCastleRiseData>
{

    Dictionary<CastleObjectType, List<TableCastleRiseData>> m_db = new();

    public IReadOnlyDictionary<CastleObjectType, List<TableCastleRiseData>> db => m_db;

    public Table_CastleRise(List<TableCastleRiseData> _data) : base(_data)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var d = list[i];
            d.Initialize();
            m_list[i] = d;
        }

        m_db = m_list.GroupBy(x => x.type)
            .ToDictionary(x => x.Key, x => x.OrderBy(x => x.level).ToList());
    }

    public IReadOnlyList<TableCastleRiseData> GetTable(CastleObjectType _obejctType)
    {
        if (m_db.ContainsKey(_obejctType))
            return m_db[_obejctType];

        return m_db[CastleObjectType.NONE];
    }

    public TableCastleRiseData GetRiseData(CastleObjectType _obejctType, int _nowLevel)
    {
        return GetTable(_obejctType).FirstOrDefault(x => x.level == _nowLevel);
    }
}

public struct TableCastleRiseData
{
    public string key;
    public int level;
    public int value_01;        // 요구치
    public int value_02;        // 요구치
    public int count_batch;     // 업그레이드 당 배치 수
    public int upgrade_duration;// 업그레이드 소요 시간 (초)
    public int max_amount;      // 최대 보유량
    public int rate_per_second; // 초당 획득량

    // CUSTOM
    public bool isActive => key.IsActive();
    public CastleObjectType type;
    public void Initialize()
    {
        type = System.Enum.Parse<CastleObjectType>(key);

        m_maxCoreStat = new[] { value_01, value_02 };
    }

    int[] m_maxCoreStat;
    public int[] maxCoreStat => m_maxCoreStat;
}
