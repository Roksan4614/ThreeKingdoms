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

    MUTE_SOUND_BGM,
    MUTE_SOUND_SFX,
    OFF_HAPTIC,
    OFF_SCREEN_SHAKE,

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
        get => m_data.IsOn(OptionType.AUTO_SKILL);
        set => SetOption(OptionType.AUTO_SKILL, value, false);
    }

    public bool isHideHpBar
    {
        get => m_data.IsOn(OptionType.HIDE_HP_BAR);
        set => SetOption(OptionType.HIDE_HP_BAR, value);
    }

    public bool isMute_BGM
    {
        get => m_data.IsOn(OptionType.MUTE_SOUND_BGM);
        set => SetOption(OptionType.MUTE_SOUND_BGM, value);
    }
    public bool isMute_SFX
    {
        get => m_data.IsOn(OptionType.MUTE_SOUND_SFX);
        set => SetOption(OptionType.MUTE_SOUND_SFX, value);
    }
    public bool isHaptic
    {
        // off haptic 인데.. 펀의상 이렇게 하자;;
        get => m_data.IsOn(OptionType.OFF_HAPTIC) == false;
        set => SetOption(OptionType.OFF_HAPTIC, value == false, false);
    }
    public bool isScreenShake
    {
        // 기서도 진동처럼..
        get => m_data.IsOn(OptionType.OFF_SCREEN_SHAKE) == false;
        set => SetOption(OptionType.OFF_SCREEN_SHAKE, value == false, false);
    }

    public bool IsOn(OptionType _type)
        => m_data.IsOn(_type);

    public void SetOption(OptionType _type, bool _isOn, bool _isEmit = true)
    {
        m_data.SetOption(_type, _isOn);
        SaveData_Option();
        if (_isEmit == true)
            Signal.instance.OptionUpdate.Emit(_type);
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

        public void SetOption(OptionType _type, bool _isOn)
        {
            int optionValue = _isOn ? 1 : 0;
            if (db.ContainsKey(_type) == false)
                db.Add(_type, optionValue);
            else
                db[_type] = optionValue;
        }

        public bool IsOn(OptionType _type)
            => db.ContainsKey(_type) && db[_type] == 1;
    }
}
