using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class AddressableManager
{
    protected override void OnDestroy()
    {
        foreach (var h in m_heroCharacter)
            h.Value.Release();
        foreach (var h in m_heroIcon)
            h.Value.Release();

        base.OnDestroy();
    }

    Dictionary<string, AsyncOperationHandle<GameObject>> m_heroIcon = new();
    Dictionary<string, AsyncOperationHandle<GameObject>> m_heroCharacter = new();

    public async UniTask Load_HeroIcon(params string[] _key)
    {
        List<string> keys = new();

        for (int i = 0; i < _key.Length; i++)
        {
            var key = $"Icon_{_key[i]}";

            if (m_heroIcon.ContainsKey(key) == false)
                keys.Add($"Hero_Icon/{key}.prefab");
        }

        await LoadAsset<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_heroIcon.ContainsKey(data.Key) == false)
                    m_heroIcon.Add(data.Key, data.Value);
            }
        }, null, keys.ToArray());
    }

    public async UniTask<GameObject> GetHeroIcon(string _key)
    {
        string key = $"Icon_{_key}";
        if (m_heroIcon.ContainsKey(key))
            return m_heroIcon[key].Result;

        await Load_HeroIcon(_key);

        return m_heroIcon.ContainsKey(key) ? m_heroIcon[key].Result : null;
    }

    public async UniTask Load_HeroCharacter(params string[] _key)
    {
        List<string> keys = new();

        for (int i = 0; i < _key.Length; i++)
        {
            if (m_heroCharacter.ContainsKey(_key[i]) == false)
                keys.Add($"Hero_Character/{_key[i]}.prefab");
        }

        await LoadAsset<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_heroCharacter.ContainsKey(data.Key) == false)
                    m_heroCharacter.Add(data.Key, data.Value);
            }
        }, null, keys.ToArray());
    }

    public async UniTask<GameObject> GetHeroCharacter(string _key)
    {
        if (m_heroCharacter.ContainsKey(_key))
            return m_heroCharacter[_key].Result;

        await Load_HeroCharacter(_key);

        return m_heroCharacter.ContainsKey(_key) ? m_heroCharacter[_key].Result : null;
    }
}
