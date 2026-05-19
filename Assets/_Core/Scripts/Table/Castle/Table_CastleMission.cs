using System.Collections.Generic;
using UnityEngine;

public class Table_CastleMission : BaseTable<string, TableCastleMissionData>
{
    public Table_CastleMission(List<TableCastleMissionData> _data) : base(_data)
    {
        m_list.Add(new()
        {
            key = "군량 수송",
            req_stat_type = CoreStatType.Leadership,
        });

        m_list.Add(new()
        {
            key = "도적 토벌",
            req_stat_type = CoreStatType.Strength,
        });

        m_list.Add(new()
        {
            key = "도서관 정리",
            req_stat_type = CoreStatType.Intellect,
        });

        m_list.Add(new()
        {
            key = "마을 갈등 중재",
            req_stat_type = CoreStatType.Politics,
        });

        m_list.Add(new()
        {
            key = "고양이 구출",
            req_stat_type = CoreStatType.Charisma,
        });

        SetDictionary(x => x.key);
    }
}

public struct TableCastleMissionData
{
    public string key;
    public CoreStatType req_stat_type;


    public bool isActive => key.IsActive();
}