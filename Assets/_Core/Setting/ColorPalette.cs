using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

enum PaletteColorType
{
    NONE = -1,

    hero_icon_empty,
    hero_icon_empty2,
    
    MAX
}

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Scriptable Objects/ColorPalette")]
[Serializable]
public class ColorPalette : ScriptableObject
{
    [Serializable]
    public struct ElementData
    {
        public string key;
        public Color color;
    }

    [SerializeField]
    List<ElementData> m_element = new();

    Dictionary<string, Color> m_map;



    public void RebuildCache()
    {
        if (m_map == null) m_map = new(StringComparer.OrdinalIgnoreCase);
        else m_map.Clear();

        foreach (var e in m_element)
        {
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            if (m_map.ContainsKey(e.key)) continue;
            m_map.Add(e.key, e.color);
        }
    }

    public Color Get(string _key)
    {
        if (m_map == null) RebuildCache();

        return m_map.GetValueOrDefault(_key);
    }

    //private void OnValidate()
    //{

    //    for (var i = PaletteColorType.NONE + 1; i < PaletteColorType.MAX; ++i)
    //    {
    //        var key = i.ToString();
    //        if (m_element.Any(x => x.key.Equals(key)) == false)
    //            m_element.Add(new ElementData() { key = key, color = Color.white });
    //    }
    //}
}
