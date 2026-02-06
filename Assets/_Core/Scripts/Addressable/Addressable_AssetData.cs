using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class AddressableManager
{
    Dictionary<string, AsyncOperationHandle<GameObject>> m_heroIcon = new();

    public void Load_HeroIcon(string _heroName, UnityAction<GameObject> _callback)
    {
        string key = $"Icon_{_heroName}";
        if (m_heroIcon.ContainsKey(key))
        {
            _callback(m_heroIcon[key].Result);
            return;
        }

        StartCoroutine(DoLoadAsset<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_heroIcon.ContainsKey(data.Key) == false)
                    m_heroIcon.Add(data.Key, data.Value);
            }

            if (m_heroIcon.ContainsKey(key))
                _callback(m_heroIcon[key].Result);

        }, null, $"Hero_Icon/{key}.prefab"));
    }
}
