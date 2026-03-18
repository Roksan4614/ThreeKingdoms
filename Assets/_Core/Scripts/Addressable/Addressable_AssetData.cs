using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class AddressableManager
{
    protected override void OnDestroy()
    {
        foreach (var h in m_heroCharacter)
            Release(h.Value);
        foreach (var h in m_heroIcon)
            Release(h.Value);
        foreach (var h in m_loadedAtlas)
            Release(h.Value);
        foreach (var h in m_itemIcon)
            Release(h.Value);

        base.OnDestroy();
    }

    Dictionary<string, AsyncOperationHandle<GameObject>> m_heroIcon = new();
    Dictionary<string, AsyncOperationHandle<GameObject>> m_itemIcon = new();
    Dictionary<string, AsyncOperationHandle<GameObject>> m_heroCharacter = new();

    public async UniTask<GameObject> GetIconAsync(string _key, bool _isHero)
    {
        if (_isHero)
            return await GetHeroIconAsync(_key);
        else
            return await GetItemIconAsync(_key);
    }

    public async UniTask Load_HeroIconAsync(params string[] _key)
    {
        List<string> keys = new();
        List<string> paths = new();

        for (int i = 0; i < _key.Length; i++)
        {
            var key = $"Icon_{_key[i]}";
            if (m_heroIcon.ContainsKey(key) == false && keys.Contains(key) == false)
            {
                keys.Add(key);
                paths.Add($"Hero_Icon/{key}.prefab");
            }
        }

        if (keys.Count == 0)
            return;

        await LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_heroIcon.ContainsKey(data.Key) == false)
                {
                    m_heroIcon.Add(data.Key, data.Value);
                    keys.Remove(data.Key);
                }
                else
                    data.Value.Release();
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (m_heroIcon.ContainsKey(keys[i]) == false)
                    m_heroIcon.Add(keys[i], default);
            }
        }, null, paths.ToArray());
    }

    public async UniTask<GameObject> GetHeroIconAsync(string _key)
    {
        string key = $"Icon_{_key}";
        if (m_heroIcon.ContainsKey(key))
            return m_heroIcon[key].IsValid() ? m_heroIcon[key].Result : null;

        await Load_HeroIconAsync(_key);

        return m_heroIcon.ContainsKey(key) ? m_heroIcon[key].Result : null;
    }

    public async UniTask Load_ItemIconAsync(params string[] _key)
    {
        List<string> keys = new();
        List<string> paths = new();

        for (int i = 0; i < _key.Length; i++)
        {
            var key = $"Icon_{_key[i]}";
            if (m_itemIcon.ContainsKey(key) == false && keys.Contains(key) == false)
            {
                keys.Add(key);
                paths.Add($"Item_Icon/{key}.prefab");
            }
        }

        if (keys.Count == 0)
            return;

        await LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_itemIcon.ContainsKey(data.Key) == false)
                    m_itemIcon.Add(data.Key, data.Value);
                else
                    data.Value.Release();
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (m_itemIcon.ContainsKey(keys[i]) == false)
                    m_itemIcon.Add(keys[i], default);
            }

        }, null, paths.ToArray());
    }

    public async UniTask<GameObject> GetItemIconAsync(string _key)
    {
        string key = $"Icon_{_key}";
        if (m_itemIcon.ContainsKey(key))
            return m_itemIcon[key].IsValid() ? m_itemIcon[key].Result : null;

        await Load_ItemIconAsync(_key);

        return m_itemIcon.ContainsKey(key) ? m_itemIcon[key].Result : null;
    }

    public async UniTask Load_HeroCharacterAsync(params string[] _key)
    {
        List<string> keys = new();
        List<string> paths = new();

        for (int i = 0; i < _key.Length; i++)
        {
            if (m_heroCharacter.ContainsKey(_key[i]) == false && keys.Contains(_key[i]) == false)
            {
                paths.Add($"Hero_Character/{_key[i]}.prefab");
                keys.Add(_key[i]);
            }
        }

        if (keys.Count == 0)
            return;

        await LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_heroCharacter.ContainsKey(data.Key) == false)
                {
                    m_heroCharacter.Add(data.Key, data.Value);
                    keys.Remove(data.Key);
                }
                else
                    data.Value.Release();
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (m_heroCharacter.ContainsKey(keys[i]) == false)
                    m_heroCharacter.Add(keys[i], default);
            }
        }, null, paths.ToArray());
    }

    public async UniTask<GameObject> GetHeroCharacterAsync(string _key)
    {
        if (m_heroCharacter.ContainsKey(_key))
            return m_heroCharacter[_key].IsValid() ? m_heroCharacter[_key].Result : null;

        await Load_HeroCharacterAsync(_key);

        return m_heroCharacter.ContainsKey(_key) ? m_heroCharacter[_key].Result : null;
    }
}
