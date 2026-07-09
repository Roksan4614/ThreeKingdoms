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
    HIDE_HP_BAR,

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

    bool m_isSkipSave;
    public void SetSkipSave() => m_isSkipSave = true;

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
        if (m_isSkipSave == true)
        {
            m_isSkipSave = false;
            return;
        }
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
        get => m_data.db[OptionType.AUTO_SKILL] == 1;
        set
        {
            m_data.db[OptionType.AUTO_SKILL] = value ? 1 : 0;
            SaveData_Option();
        }
    }

    public bool isHideHpBar
    {
        get => m_data.db[OptionType.HIDE_HP_BAR] == 1;
        set
        {
            m_data.db[OptionType.HIDE_HP_BAR] = value ? 1 : 0;
            SaveData_Option();

            Signal.instance.OptionUpdate.Emit(OptionType.HIDE_HP_BAR);
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
