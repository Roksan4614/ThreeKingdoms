using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

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
        foreach (var h in m_lobbyScreen)
            Release(h.Value);

        base.OnDestroy();
    }

    Dictionary<string, AsyncOperationHandle<GameObject>> m_heroIcon = new();
    Dictionary<string, AsyncOperationHandle<GameObject>> m_itemIcon = new();
    Dictionary<string, AsyncOperationHandle<GameObject>> m_heroCharacter = new();
    Dictionary<string, AsyncOperationHandle<GameObject>> m_lobbyScreen = new();

    public async UniTask<GameObject> GetIconAsync(string _key, bool _isHero)
    {
        if (_isHero)
            return await GetHeroIconAsync(_key);
        else
            return await GetItemIconAsync(_key);
    }

    #region HERO_ICON
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

        //m_isLogSwitch = true;
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
    #endregion HERO_ICON

    #region ITEM_ICON
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

        return await GetItemIconAsync(_key);
    }
    #endregion ITEM_ICON

    #region HERO_CHARACTER
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
    #endregion HERO_CHARACTER

    #region RELIC_ICON
    public async UniTask<GameObject> GetRelicIconAsync(string _heroKey)
    {
        //일단 영웅 아이콘으로. 유물 아이콘 만들면 바꿔주자
        string key = $"Icon_{_heroKey}";
        //string key = $"Relic_{_heroKey}";

        if (m_heroIcon.ContainsKey(key))
            return m_heroIcon[key].IsValid() ? m_heroIcon[key].Result : null;

        //await Load_RelicIconAsync(key);
        await Load_HeroIconAsync(_heroKey);

        return m_heroIcon.ContainsKey(key) ? m_heroIcon[key].Result : null;
    }
    public async UniTask Load_RelicIconAsync(params string[] _key)
    {
        List<string> keys = new();
        List<string> paths = new();

        for (int i = 0; i < _key.Length; i++)
        {
            if (m_heroIcon.ContainsKey(_key[i]) == false && keys.Contains(_key[i]) == false)
            {
                paths.Add($"Relic_Icon/{_key[i]}.prefab");
                keys.Add(_key[i]);
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
    #endregion RELIC_ICON

    public async UniTask<Sprite[]> GetAtlasAsync_CastleNPC(int _index)
    {
        var tag = "Castle_NPC";
        if (m_loadedAtlas.ContainsKey(tag) == false)
        {
            string key = $"Atlas/{tag}.spriteatlasv2";
            await LoadAssetAsync<SpriteAtlas>(_result =>
            {
                foreach (var s in _result)
                {
                    if (m_loadedAtlas.ContainsKey(tag) == false)
                        m_loadedAtlas.Add(tag, s.Value);
                }
            }, null, key);

            return await GetAtlasAsync_CastleNPC(_index);
        }

        var atlas = m_loadedAtlas[tag].Result;

        var body = atlas.GetSprite($"Castle_NPC_Body{_index:00}_0");
        var head = atlas.GetSprite($"Castle_NPC_Head{_index:00}_0");

        return new Sprite[] { body, head };
    }

    public async UniTask<GameObject> GetLobbyScreen(LobbyScreenType _screenType)
    {
        var key = _screenType.ToString();
        if (m_lobbyScreen.ContainsKey(key))
            return m_lobbyScreen[key].IsValid() ? m_lobbyScreen[key].Result : null;

        await Load_LobbyScreenAsync(_screenType);

        return m_lobbyScreen.ContainsKey(key) ? m_lobbyScreen[key].Result : null;
    }

    async UniTask Load_LobbyScreenAsync(LobbyScreenType _screenType)
    {
        string key = $"LobbyScreen/{_screenType}.prefab";

        await LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_lobbyScreen.ContainsKey(data.Key) == false)
                    m_lobbyScreen.Add(data.Key, data.Value);
                else
                    data.Value.Release();
            }

            if (m_lobbyScreen.ContainsKey(key) == false)
                m_lobbyScreen.Add(key, default);
        }, null, key);
    }
}
