using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum OptionType
{
    NONE = -1,

    LANGUEGE,
    MAIN_TEAMPOSITION_TYPE,
    SCENARIO_SKIP,

    AUTO_SKILL,

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

    public bool isScenarioSkip
    {
        get => m_data.db[OptionType.SCENARIO_SKIP] == 1;
        set
        {
            m_data.db[OptionType.SCENARIO_SKIP] = value ? 1 : 0;
            SaveData_Option();
        }
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

    public bool isAutoSkill
    {
        get => m_data.db.ContainsKey(OptionType.AUTO_SKILL) && m_data.db[OptionType.AUTO_SKILL] == 1;
        set
        {
            if (m_data.db.ContainsKey(OptionType.AUTO_SKILL) == false)
                m_data.db.Add(OptionType.AUTO_SKILL, value ? 1 : 0);
            else
                m_data.db[OptionType.AUTO_SKILL] = value ? 1 : 0;
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
