using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Data_UserInfo
{
    ElementData m_element;
    HeroSortData m_sortData;
    public HeroSortData sortData => m_sortData;

    public int uid => m_element.uid;
    public string nickname => m_element.nickname;
    public RegionType region => m_element.region;
    public IReadOnlyList<HeroInfoData> myHero => m_element.myHero.DeepClone();
    public int profileIdx => m_element.profileIdx;
    public string profileSkin => m_element.profileSkin;

    public long gold => m_element.gold;
    public long rice => m_element.rice;

    public async UniTask InitializeAsync()
    {
        if (PPWorker.HasKey(PlayerPrefsType.USER_DATA))
        {
            m_element = PPWorker.Get<ElementData>(PlayerPrefsType.USER_DATA);

            await AddressableManager.instance.Load_HeroIconAsync(m_element.myHero.Select(x => x.skin).ToArray());
            await AddressableManager.instance.Load_HeroCharacterAsync(m_element.myHero.FindAll(x => x.isBatch).Select(x => x.skin).ToArray());

            //if (TutorialManager.instance.IsComplete(GuideQuestType.START) == false)
            //{
            //    var heroes = m_element.myHero.FindAll(x => x.isMain == false && x.isBatch == true).ToList();
            //    if (heroes.Count > 0)
            //    {
            //        for (int i = 0; i < heroes.Count; i++)
            //        {
            //            var h = heroes[i];
            //            h.isBatch = false;
            //            Update(h);
            //        }
            //    }
            //}
        }
        else
        {
            m_element.Default();
            SaveData();
        }

        if (PPWorker.HasKey(PlayerPrefsType.HERO_SORTING_DATA))
            m_sortData = PPWorker.Get<HeroSortData>(PlayerPrefsType.HERO_SORTING_DATA);
        else
        {
            m_sortData = new();
            m_sortData.Default();
            SaveData_SortingData();
        }
    }

    public void SaveData()
    {
        if (m_element.myHero.Count > 1)
            m_element.myHero = m_element.myHero.SortByDescending(x => x.isMain);

        PPWorker.Set(PlayerPrefsType.USER_DATA, m_element);
    }

    public void SaveData_SortingData()
    {
        PPWorker.Set(PlayerPrefsType.HERO_SORTING_DATA, m_sortData);
    }

    public void SetFilterData(List<RegionType> _region, List<HeroClassType> _class, List<GradeType> _grade)
    {
        m_sortData.filter_region = _region;
        m_sortData.filter_class = _class;
        m_sortData.filter_grade = _grade;

        SaveData_SortingData();
    }

    public void SetSortingData(HeroSortType _sortType, bool _isDescending)
    {
        m_sortData.isDescending = _isDescending;
        m_sortData.sortType = _sortType;

        SaveData_SortingData();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="_isWithNotMine">내꺼 외에도 모든 영웅 표기를 할거야?</param>
    /// <returns></returns>
    public List<HeroInfoData> GetHeroSortData(List<HeroInfoData> _data = null, bool _isWithNotMine = true)
    {
        List<HeroInfoData> result = new();
        List<HeroInfoData> lstBatch = new();
        List<HeroInfoData> lstNotBatch = new();

        if (_data == null)
        {
            _data = new();
            _data.AddRange(m_element.myHero);
        }

        //일단 리스트 다 넣어주자.

        for (int i = 0; i < _data.Count; i++)
        {
            var data = _data[i];

            if (data.isBatch == true)
                lstBatch.Add(data);
            else if (_isWithNotMine == true || data.isMine == true)
                lstNotBatch.Add(data);
        }

        var dbHero = TableManager.hero.GetHeroList();

        if (_isWithNotMine == true && dbHero.Count > _data.Count)
        {
            var keys = _data.Select(x => x.key).ToHashSet();
            dbHero = dbHero.Where(x => keys.Contains(x.key) == false).ToList();

            for (int i = 0; i < dbHero.Count; i++)
            {
                HeroInfoData data = new(dbHero[i].key, _isMine: false);
                lstNotBatch.Add(data);
            }
        }

        // 배치중인걸 먼저 넣어주자
        {
            // 메인이 전방이 아니면 후방으로 빼주자
            if (DataManager.option.mainTeamPosition != TeamPositionType.Front)
            {
                lstBatch.Add(lstBatch[0]);
                lstBatch.RemoveAt(0);
            }

            result.AddRange(lstBatch);
        }

        lstNotBatch.Sort((x, y) => { return SortCompare(x, y, true); });

        result.AddRange(lstNotBatch);

        lstBatch = null;
        lstNotBatch = null;

        result = GetFilter(result);

        return result;
    }

    public List<HeroInfoData> GetFilter(List<HeroInfoData> _origin)
    {
        return _origin.FindAll(x =>
          {
              if (x.isBatch == true)
                  return true;

              if (m_sortData.isAll_Region == false &&
                  m_sortData.filter_region.Contains(x.regionType) == false)
              {
                  return false;
              }

              if (m_sortData.isAll_Grade == false &&
                  m_sortData.filter_grade.Contains(x.grade) == false)
              {
                  return false;
              }

              if (m_sortData.isAll_Class == false &&
                  m_sortData.filter_class.Contains(x.classType) == false)
              {
                  return false;
              }

              return true;
          });
    }


    public bool HasHero(CharacterName _name)
        => HasHero(_name.ToString());
    public bool HasHero(string _key)
        => m_element.myHero.FindIndex(x => x.key == _key) > -1;
    public HeroInfoData GetHeroInfoData(CharacterName _characterName)
        => GetHeroInfoData(_characterName.ToString());
    public HeroInfoData GetHeroInfoData(string _key)
    {
        for (int i = 0; i < m_element.myHero.Count; i++)
        {
            var heroData = m_element.myHero[i];
            if (heroData.key.IsEquals(_key))
                return heroData.DeepClone();
        }
        return null;
    }

    public void Update(HeroInfoData _heroData)
    {
        var index = m_element.myHero.FindIndex(x => x.key.Equals(_heroData.key));
        m_element.myHero[index] = _heroData;
        SaveData();
    }

    public void UpdateUpgrade(HeroInfoData _heroData)
    {
        var hero = m_element.myHero.Find(x => x.key.Equals(_heroData.key));

        if (hero != null)
        {
            hero.enchantLevel = _heroData.enchantLevel;
            hero.grade = _heroData.grade;
            SaveData();
        }
    }

    public void ResetResultStat(params string[] _heroKey)
    {
        if (_heroKey.Length == 0)
        {
            foreach (var h in m_element.myHero)
                h.ResetResultStat();

            Signal.instance.UpdateHeroStat.Emit(null);
        }
        else
        {
            foreach (var k in _heroKey)
            {
                m_element.myHero.Find(x => x.key == k)?.ResetResultStat();
                Signal.instance.UpdateHeroStat.Emit(k);
            }
        }

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

        m_element.myHero = m_element.myHero.SortBy(x =>
        {
            if (indexMap.TryGetValue(x.key, out int index))
            {
                return index;
            }
            return int.MaxValue;
        });

        SaveData();
    }

    public void AddHeroSoul(string _key, int _count)
    {
        var heroData = m_element.myHero.Find(x => x.key.Equals(_key));

        if (heroData == null || heroData.isMine == false)
        {
            var grade = TableManager.hero.GetGradeFromSoulCount(_count);
            AddHero(_key, grade);
        }
        else
        {
            heroData.soulCount += _count;
            SaveData();
        }
    }

    public void AddHero(string _key, GradeType _grade = GradeType.Normal, bool _isBatch = false, bool _isMain = false)
        => AddHeroAsync(_key, _grade, _isBatch, _isMain).Forget();

    public async UniTask AddHeroAsync(string _key, GradeType _grade = GradeType.Normal, bool _isBatch = false, bool _isMain = false)
    {
        if (m_element.myHero.Any(x => x.key == _key))
            return;

        m_element.myHero.Add(new(_key, _isMain: _isMain, _isBatch: _isBatch));
        DataManager.stat.friendShip.Reload();

        await AddressableManager.instance.Load_HeroIconAsync(_key);
        await AddressableManager.instance.Load_HeroCharacterAsync(m_element.myHero.FindAll(x => x.isBatch).Select(x => x.skin).ToArray());

        SaveData();

        Signal.instance.UpdateHeroStat.Emit("");
    }
    public void SetRegion(RegionType _region)
    {
        m_element.region = _region;
        m_element.profileSkin = TableManager.region.Get(_region).master;
        SaveData();
    }

    #region ASSETS
    public long GetAssetAmount(ItemType _itemType)
        => _itemType switch { ItemType.Gold => m_element.gold, ItemType.Rice => m_element.rice, _ => -1 };

    public void AddItem(ItemType _itemType, int _count, bool _isUpdate = true, bool _isTween = true, bool _isAction = true, Vector3 _actionPosition = default)
    {
        AddItem(_isUpdate, _isTween, _isAction, _actionPosition, TableManager.item.GetItemData(_itemType, _count));
    }

    public void AddItem(bool _isUpdate = true, bool _isTween = true, bool _isAction = true, Vector3 _actionPosition = default, params ItemData[] _itemData)
    {
        if (_isAction)
            RewardWorker.instance.RunAsync(_actionPosition, _itemData: _itemData).Forget();
        else
        {
            foreach (var item in _itemData)
            {
                switch (item.key)
                {
                    case ItemType.Rice:
                    case ItemType.Gold:
                        AddAsset(item.key, item.count, _isUpdate, _isTween);
                        break;
                    case ItemType.Dedicated_Soul_Stone:
                        AddHeroSoul(item.value, (int)item.count);
                        break;
                }
            }
        }
    }

    public void AddAsset(long _gold, long _rice, bool _isUpdate = true, bool _isTween = true)
    {
        SetAsset(
            _gold != 0 ? m_element.gold + _gold : -1,
            _rice != 0 ? m_element.rice + _rice : -1,
            _isUpdate, _isTween);
    }
    public void AddAsset(ItemType _itemType, long _amount, bool _isUpdate = true, bool _isTween = true)
        => AddAsset(_itemType == ItemType.Gold ? _amount : 0, _itemType == ItemType.Rice ? _amount : 0, _isUpdate, _isTween);

    //public void SetProvision(long _amount, bool _isUpdate = true, bool _isTween = true)
    //    => SetAsset(-1, _amount, _isUpdate, _isTween);
    //public void SetGold(long _amount, bool _isUpdate = true, bool _isTween = true)
    //    => SetAsset(_amount, -1, _isUpdate, _isTween);
    public void SetAsset(long _gold, long _rice, bool _isUpdate = true, bool _isTween = true)
    {
        ItemType itemType = ItemType.Gold;

        if (_gold > -1 && _rice > -1)
        {
            m_element.gold = _gold;
            m_element.rice = _rice;
            itemType = ItemType.NONE;
        }
        else if (_gold > -1)
        {
            m_element.gold = _gold;
            itemType = ItemType.Gold;
        }
        else if (_rice > -1)
        {
            m_element.rice = _rice;
            itemType = ItemType.Rice;
        }

        if (_isUpdate)
            Signal.instance.UpdateAsset.Emit((_isTween, itemType));

        SaveData();
    }
    #endregion ASSETS

    #region SORT
    public int SortCompare(HeroInfoData x, HeroInfoData y, bool _isFirstMine)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int result = 0;

        if (_isFirstMine)
        {
            if (x.isMine != y.isMine)
                return x.isMine ? -1 : 1;
        }

        switch (m_sortData.sortType)
        {
            case HeroSortType.GRADE:
                result = CompareGrade(x, y) * (m_sortData.isDescending ? -1 : 1);
                if (result != 0) return result;
                result = CompareEnchantLevel(x, y) * (m_sortData.isDescending ? -1 : 1);
                if (result != 0) return result;
                result = CompareRegion(x, y);
                if (result != 0) return result;
                result = CompareClass(x, y);
                break;

            case HeroSortType.LEVEL:
                result = CompareEnchantLevel(x, y) * (m_sortData.isDescending ? -1 : 1);
                if (result != 0) return result;
                result = CompareGrade(x, y) * (m_sortData.isDescending ? -1 : 1);
                if (result != 0) return result;
                result = CompareRegion(x, y);
                if (result != 0) return result;
                result = CompareClass(x, y);
                break;
        }

        if (result != 0) return result;

        result = string.Compare(x.name, y.name, System.StringComparison.Ordinal);
        if (result != 0) return result;

        return 0;
    }

    private int CompareRegion(HeroInfoData x, HeroInfoData y)
    {
        bool isX = x.regionType == DataManager.userInfo.region;
        bool isY = y.regionType == DataManager.userInfo.region;

        if (isX == isY)
            return x.regionType.CompareTo(y.regionType);

        return isX ? -1 : 1;
    }
    private int CompareClass(HeroInfoData x, HeroInfoData y) => x.classType.CompareTo(y.classType);
    // -1은.. 오름차순이라도 등급이 높은애가 위로 가야 할것 같아
    private int CompareGrade(HeroInfoData x, HeroInfoData y) => x.grade.CompareTo(y.grade) * -1;
    private int CompareEnchantLevel(HeroInfoData x, HeroInfoData y) => x.enchantLevel.CompareTo(y.enchantLevel) * -1;
    #endregion SORT

    #region TRAITS
    #endregion

    struct ElementData
    {
        public int uid;
        public string nickname;
        public RegionType region;
        public List<HeroInfoData> myHero;

        public int profileIdx;
        public string profileSkin;

        public long gold;
        public long rice;

        public void Default()
        {
            region = RegionType.SHU;
            nickname = "록산";
            myHero = new();

            profileSkin = "LiuBei";
        }
    }

    public class HeroSortData
    {
        public HeroSortType sortType;
        public bool isDescending;

        public List<RegionType> filter_region;
        public List<HeroClassType> filter_class;
        public List<GradeType> filter_grade;

        public void Default()
        {
            sortType = HeroSortType.GRADE;

            filter_region = new();
            filter_class = new();
            filter_grade = new();

            int i = -1;
            while (true)
            {
                var rt = (RegionType)i;
                if (rt < RegionType.MAX)
                    filter_region.Add(rt);

                var ct = (HeroClassType)i;
                if (ct < HeroClassType.MAX)
                    filter_class.Add(ct);

                var gt = (GradeType)i;
                if (gt < GradeType.MAX)
                    filter_grade.Add(gt);

                if (rt >= RegionType.MAX &&
                    ct >= HeroClassType.MAX &&
                    gt >= GradeType.MAX)
                    break;

                i++;
            }
        }

        public bool isAll_Region => filter_region.Contains(RegionType.MAX);
        public bool isAll_Class => filter_class.Contains(HeroClassType.MAX);
        public bool isAll_Grade => filter_grade.Contains(GradeType.MAX);
    }
}