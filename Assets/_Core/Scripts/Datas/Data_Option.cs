using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum OptionType
{
    NONE = -1,

    LANGUEGE,
    MAIN_TEAMPOSITION_TYPE,

    MAX
}

public enum LanguageType
{
    Korean,
    English,
}

public class Data_Option
{
    OptionData m_data;
    public void Initialize()
    {
        m_data = PPWorker.Get<OptionData>(PlayerPrefsType.OPTION, false);

        if (m_data.db == null)
        {
            m_data.SetDefault();
            SaveData_Option();
        }
    }

    public void SaveData_Option()
    {
        PPWorker.Set(PlayerPrefsType.OPTION, m_data, false);
    }

    public TeamPositionType mainTeamPosition
    {
        get => (TeamPositionType)m_data.db[OptionType.MAIN_TEAMPOSITION_TYPE];
        set
        {
            m_data.db[OptionType.MAIN_TEAMPOSITION_TYPE] = (int)value;
            SaveData_Option();
        }
    }

    public LanguageType language
    {
        get => (LanguageType)m_data.db[OptionType.LANGUEGE];
        set
        {
            m_data.db[OptionType.LANGUEGE] = (int)value;
            SaveData_Option();
        }
    }

    [Serializable]
    public struct OptionData
    {
        public Dictionary<OptionType, int> db;

        public void SetDefault()
        {
            db = new();
            for (var e = OptionType.NONE + 1; e < OptionType.MAX; e++)
                db.Add(e, 0);
        }
    }
}
