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
    public IReadOnlyList<ElementData> element => m_element;

    Dictionary<string, Color> m_map;

    public void SetData(ColorPalette _data)
    {
        m_element = _data.m_element;
        RebuildCache();
    }

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
        if (_key.IsActive())
            return m_map.GetValueOrDefault(_key);
        else
            return default;
    }

    public void Add(ElementData _data) => m_element.Add(_data);
    public void RemoveAt(int _idx) => m_element.RemoveAt(_idx);
    public void UpdateData(int _idx, ElementData _data) => m_element[_idx] = _data;
    public void Sort() => m_element.Sort((a, b) => a.key.CompareTo(b.key));


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
