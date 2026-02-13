using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public abstract class BaseTable<T,V> : MonoBehaviour
{
    protected readonly List<V> m_list;
    protected Dictionary<T, V> m_dictionary;

    public BaseTable(List<V> _table)
    {
        if (_table == null)
            return;

        m_list = _table;

        // Group
        //m_group = m_list.GroupBy(x => x.slotType).ToDictionary(x => x.Key, x => x.ToList());
        // Sort
        //Sort((a, b) => a.slotType > b.slotType ? -1 : 1);
        // 게임 타입별로 그룹핑

        // Group Sort
        // var en = m_group.GetEnumerator();
        // while (en.MoveNext())
        // {
        //     en.Current.Value.Sort((a, b) => a.slotType > b.slotType);
        // }
    }

    public IReadOnlyList<V> list => m_list;

    public bool Exists(T _id)
    {
        return m_dictionary != null && m_dictionary.ContainsKey(_id);
    }

    public V Get(T _id)
    {
        return Exists(_id) ? m_dictionary[_id] : default;
    }

    protected void SetDictionary(Func<V, T> _keySelector)
    {
        m_dictionary = m_list.ToDictionary(_keySelector, x => x);
    }
}
