using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetLoader
{
    private static AssetLoader m_instance;

    public static AssetLoader instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new AssetLoader();
            }

            return m_instance;
        }
    }

    private Dictionary<string, object> m_dictionary = new();

    public void Initialize()
    {
        m_dictionary.Clear();
    }

    public static T Load<T>(string _path, bool _errorLog = true) where T : Object
    {
        if (instance.m_dictionary.ContainsKey(_path))
            return (T)instance.m_dictionary[_path];

        T asset = Resources.Load<T>(_path);

        if (asset == null)
        {
            if (_errorLog == true)
                IngameLog.AddError("AssetLoader: Load: FAILED: " + _path);
        }
        else
            instance.m_dictionary.Add(_path, asset);

        return asset;
    }

    public static T[] LoadAll<T>(string _path) where T : Object
    {
        if (instance.m_dictionary.ContainsKey(_path))
            return (T[])instance.m_dictionary[_path];

        T[] asset = Resources.LoadAll<T>(_path);

        if (asset.Length == 0)
        {
            asset = null;
            IngameLog.AddError("AssetLoader: LoadAll: FAILED: " + _path);
        }
        else
            instance.m_dictionary.Add(_path, asset);

        return asset;
    }
}