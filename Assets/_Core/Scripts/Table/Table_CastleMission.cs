using System.Collections.Generic;
using UnityEngine;

public class Table_CastleMission : BaseTable<string, TableCastleMissionData>
{
    public Table_CastleMission(List<TableCastleMissionData> _data) : base(_data)
    {
        m_list.Add(new()
        {
            key = "군량 수송",
            core_stat = CoreStatType.Leadership,
        });

        m_list.Add(new()
        {
            key = "도적 토벌",
            core_stat = CoreStatType.Strength,
        });

        m_list.Add(new()
        {
            key = "도서관 정리",
            core_stat = CoreStatType.Intellect,
        });

        m_list.Add(new()
        {
            key = "마을 갈등 중재",
            core_stat = CoreStatType.Politics,
        });

        m_list.Add(new()
        {
            key = "고양이 구출",
            core_stat = CoreStatType.Charisma,
        });

        SetDictionary(x => x.key);
    }
}

public struct TableCastleMissionData
{
    public string key;
    public CoreStatType core_stat;


    public bool isActive => key.IsActive();
}