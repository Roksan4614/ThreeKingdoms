using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class AddressableManager
{
    Dictionary<string, AsyncOperationHandle<GameObject>> m_heroIcon = new();

    public async UniTask<GameObject> GetHeroIcon(string _heroName)
    {
        string key = $"Icon_{_heroName}";
        if (m_heroIcon.ContainsKey(key))
            return m_heroIcon[key].Result;

        await LoadAsset<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_heroIcon.ContainsKey(data.Key) == false)
                    m_heroIcon.Add(data.Key, data.Value);
            }
        }, null, $"Hero_Icon/{key}.prefab");

        return m_heroIcon[key].Result ?? null;

    }
}
