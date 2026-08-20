using System;
using System.Collections.Generic;

public class Table_Region : BaseTable<RegionType, TableRegionData>
{
    public Table_Region(List<TableRegionData> _table) : base(_table)
    {
        for (int i = 0; i < m_list.Count; i++)
        {
            var data = m_list[i];
            data.regionType = Enum.Parse<RegionType>(m_list[i].key);
            data.startHeroKey = data.startHero?.Replace(" ", "").Split(",");
            m_list[i] = data;
        }

        SetDictionary(x => x.regionType);
    }

    public string GetMaster(RegionType _regionType)
    {
        string key = Get(_regionType).master;

        if (_regionType == RegionType.WU)
        {
            if (DataManager.userInfo.GetHeroInfoData(CharacterName.SunQuan).isActive == true)
                return CharacterName.SunQuan.ToString();
            else if(DataManager.userInfo.GetHeroInfoData(CharacterName.SunCe).isActive == true)
                return CharacterName.SunCe.ToString();
        }

        return key;
    }
}

public struct TableRegionData
{
    public string key;
    public string master;
    public string startHero;
    public bool isActive;

    // CUSTOM
    public RegionType regionType;
    public string[] startHeroKey;
}
