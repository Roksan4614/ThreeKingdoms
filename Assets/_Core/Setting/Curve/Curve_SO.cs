using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Curve_SO", menuName = "Scriptable Objects/Curve_SO")]
public class Curve_SO : ScriptableObject
{
    [Serializable]
    public struct ElementData
    {
        public string key;
        public AnimationCurve curve;
    }

    [SerializeField]
    List<ElementData> m_element = new();
    public IReadOnlyList<ElementData> element => m_element;

    Dictionary<string, AnimationCurve> m_map;
    public void RebuildCache()
    {
        if (m_map == null) m_map = new(StringComparer.OrdinalIgnoreCase);
        else m_map.Clear();

        foreach (var e in m_element)
        {
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            if (m_map.ContainsKey(e.key)) continue;
            m_map.Add(e.key, e.curve);
        }
    }

    //public Color Get(PaletteColorType _colorType)
    //    => Get(_colorType.ToString());

    public AnimationCurve Get(string _key)
    {
        if (m_map == null)
            RebuildCache();

        if (_key.IsActive())
            return m_map.GetValueOrDefault(_key);
        else
            return default;
    }
}
