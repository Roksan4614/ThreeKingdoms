using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerPrefsType
{
    CHAPTER_STAGE_INFO,

    MAX
}

// PlayerPrefs Worker
public class PPWorker
{
    static string PPKey(PlayerPrefsType _type, bool _isUserData = true)
        => $"{_type}" + (_isUserData ? $"_{DataManager.userInfo.uid}" : "");
    public static bool HasKey(PlayerPrefsType _type, bool _isUserData = true)
        => HasKey(PPKey(_type, _isUserData));
    public static bool HasKey(string _key)
        => PlayerPrefs.HasKey(_key);

    public static void DeleteKey(PlayerPrefsType _type)
    {
        PlayerPrefs.DeleteKey(_type.ToString());
        PlayerPrefs.Save();
    }

    public static T Get<T>(PlayerPrefsType _type, bool _isUserData = true)
    {
        string key = PPKey(_type, _isUserData);
        return Get<T>(key);
    }

    public static T Get<T>(string _key)
    {
        if (HasKey(_key) == false)
            return default;

        object result;

        if (typeof(T) == typeof(int))
            result = PlayerPrefs.GetInt(_key);
        else if (typeof(T) == typeof(float))
            result = PlayerPrefs.GetFloat(_key);
        else if (typeof(T) == typeof(string))
            result = PlayerPrefs.GetString(_key);
        else
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Get<string>(_key));

        return (T)result;
    }

    public static string GetString(PlayerPrefsType _type, bool _isUserData = true)
        => PlayerPrefs.GetString(PPKey(_type, _isUserData));
    public static int GetInt(PlayerPrefsType _type, bool _isUserData = true)
        => PlayerPrefs.GetInt(PPKey(_type, _isUserData));
    public static float GetFloat(PlayerPrefsType _type, bool _isUserData = true)
        => PlayerPrefs.GetFloat(PPKey(_type, _isUserData));
    public static void Set(PlayerPrefsType _type, object _value, bool _isUserData = true, bool _isAutoSave = true)
        => Set(PPKey(_type, _isUserData), _value, _isAutoSave);
    public static void Set(string _key, object _value, bool _isAutoSave = true)
    {
        var type = _value.GetType();

        if (type == typeof(string))
            PlayerPrefs.SetString(_key, _value.ToString());
        else if (type == typeof(int))
            PlayerPrefs.SetInt(_key, (int)_value);
        else if (type == typeof(float))
            PlayerPrefs.SetFloat(_key, (float)_value);
        else
            PlayerPrefs.SetString(_key, Newtonsoft.Json.JsonConvert.SerializeObject(_value));

        if (_isAutoSave == true)
            PlayerPrefs.Save();
    }
}
