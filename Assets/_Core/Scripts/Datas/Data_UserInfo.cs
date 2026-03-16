using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_UserInfo
{
    ElementData m_element;

    public int uid => m_element.uid;
    public RegionType region => m_element.region;
    public IReadOnlyList<HeroInfoData> myHero => m_element.myHero;

    public long gold => m_element.gold;
    public long rice => m_element.rice;

    public async UniTask Initialize()
    {
        if (PPWorker.HasKey(PlayerPrefsType.USER_DATA))
        {
            m_element = PPWorker.Get<ElementData>(PlayerPrefsType.USER_DATA);

            await AddressableManager.instance.Load_HeroIcon(m_element.myHero.Select(x => x.skin).ToArray());
            await AddressableManager.instance.Load_HeroCharacter(m_element.myHero.Where(x => x.isBatch).Select(x => x.skin).ToArray());
        }
        else
        {
            m_element.Default();
            SaveData();
        }
    }

    public void SaveData()
    {
        if (m_element.myHero.Count > 1)
            m_element.myHero = m_element.myHero.OrderByDescending(x => x.isMain).ToList();

        PPWorker.Set(PlayerPrefsType.USER_DATA, m_element);
    }

    public HeroInfoData GetHeroInfoData(string _key)
        => m_element.myHero.Where(x => x.key.Equals(_key)).FirstOrDefault();

    public void Update(HeroInfoData _heroData)
    {
        var index = m_element.myHero.FindIndex(x => x.key.Equals(_heroData.key));
        m_element.myHero[index] = _heroData;
        SaveData();
    }

    public void UpdateAll(List<HeroInfoData> _heroList)
    {
        m_element.myHero = _heroList;
        SaveData();
    }

    public void SortTeamPosition(List<HeroInfoData> _heroList)
    {
        // key와 index를 매핑한 딕셔너리 생성
        var indexMap = _heroList
            .Select((_item, _idx) => new { _item.key, _idx })
            .ToDictionary(x => x.key, x => x._idx);

        m_element.myHero = m_element.myHero.OrderBy(x =>
        {
            if (indexMap.TryGetValue(x.key, out int index))
            {
                return index;
            }
            return int.MaxValue;
        }).ToList();

        SaveData();
    }

    public void AddHero(string _key, bool _isBatch = false, bool _isMain = false)
        => AddHeroAsync(_key, _isBatch, _isMain).Forget();

    public async UniTask AddHeroAsync(string _key, bool _isBatch = false, bool _isMain = false)
    {
        if (m_element.myHero.Any(x => x.key == _key))
            return;

        m_element.myHero.Add(new(_key, _isMain: _isMain, _isBatch: _isBatch));

        await AddressableManager.instance.Load_HeroIcon(_key);
        if (_isBatch)
            await AddressableManager.instance.Load_HeroCharacter(m_element.myHero.Where(x => x.isBatch).Select(x => x.skin).ToArray());

        SaveData();
    }

    #region ASSETS
    public long GetAssetAmount(ItemType _itemType)
        => _itemType switch { ItemType.Gold => m_element.gold, ItemType.Rice => m_element.rice, _ => -1 };

    public void AddProvision(long _amount, bool _isUpdate = true, bool _isTween = true)
        => AddAsset(0, _amount, _isUpdate, _isTween);
    public void AddGold(long _amount, bool _isUpdate = true, bool _isTween = true)
        => AddAsset(_amount, 0, _isUpdate, _isTween);
    public void AddAsset(long _gold, long _rice, bool _isUpdate = true, bool _isTween = true)
    {
        SetAsset(
            _gold != 0 ? m_element.gold + _gold : -1,
            _rice != 0 ? m_element.rice + _rice : -1,
            _isUpdate, _isTween);
    }

    public void SetProvision(long _amount, bool _isUpdate = true, bool _isTween = true)
        => SetAsset(-1, _amount, _isUpdate, _isTween);
    public void SetGold(long _amount, bool _isUpdate = true, bool _isTween = true)
        => SetAsset(_amount, -1, _isUpdate, _isTween);
    public void SetAsset(long _gold, long _rice, bool _isUpdate = true, bool _isTween = true)
    {
        if (_gold > -1)
            m_element.gold = _gold;
        else if (_rice > -1)
            m_element.rice = _rice;

        if (_isUpdate)
            Signal.instance.UpdateAsset.Emit(_isTween);

        SaveData();
    }
    #endregion ASSETS

    struct ElementData
    {
        public int uid;
        public RegionType region;
        public List<HeroInfoData> myHero;

        public long gold;
        public long rice;

        public void Default()
        {
            region = RegionType.Shu;
            myHero = new();
        }
    }
}
