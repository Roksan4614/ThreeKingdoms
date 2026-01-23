using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public static class IngameLog
{
    public static void Add(params object[] _log)
    {
        for (int i = 0; i < _log.Length; i++)
        {
            string msg = _log[i].ToString();
            if (msg.Length == 0)
                continue;

#if UNITY_EDITOR
            Debug.Log(msg);
#else
#endif
        }
    }

    public static void Add(int _color, params object[] _log)
    {
        if (_log.Length == 0)
        {
            Add(_color.ToString());
            return;
        }

        string message = "";
        for (int i = 0; i < _log.Length; i++)
        {
            var msg = _log[i].ToString();
            if (msg.Length == 0)
                continue;

#if UNITY_EDITOR
            message = $"<color=#{_color.ToString("X6")}>{_log[i]}</color>";
            Debug.Log(message);
#else
#endif
        }
    }

    public static void AddError(string _message)
    {
        Debug.LogError(_message);
    }
}
