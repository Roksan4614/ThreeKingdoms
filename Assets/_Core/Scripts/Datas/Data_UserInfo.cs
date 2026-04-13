using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_UserInfo
{
    ElementData m_element;
    HeroSortData m_sortData;
    public HeroSortData sortData => m_sortData;

    public int uid => m_element.uid;
    public RegionType region => m_element.region;
    public IReadOnlyList<HeroInfoData> myHero => m_element.myHero;

    public long gold => m_element.gold;
    public long rice => m_element.rice;

    public async UniTask InitializeAsync()
    {
        if (PPWorker.HasKey(PlayerPrefsType.USER_DATA))
        {
            m_element = PPWorker.Get<ElementData>(PlayerPrefsType.USER_DATA);

            await AddressableManager.instance.Load_HeroIconAsync(m_element.myHero.Select(x => x.skin).ToArray());
            await AddressableManager.instance.Load_HeroCharacterAsync(m_element.myHero.Where(x => x.isBatch).Select(x => x.skin).ToArray());

            if (TutorialManager.instance.IsComplete(TutorialType.START) == false)
            {
                var heros = m_element.myHero.FindAll(x => x.isMain == false && x.isBatch == true).ToList();
                if (heros.Count > 0)
                {
                    for (int i = 0; i < heros.Count; i++)
                    {
                        var h = heros[i];
                        h.isBatch = false;
                        Update(h);
                    }
                }
            }
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
            m_sortData.Default();
            SaveData_SortingData();
        }
    }

    public void SaveData()
    {
        if (m_element.myHero.Count > 1)
            m_element.myHero = m_element.myHero.OrderByDescending(x => x.isMain).ToList();

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
        var dbHero = TableManager.hero.list;
        for (int i = 0; i < dbHero.Count; i++)
        {
            var data = _data.Find(x => x.key == dbHero[i].key);

            if (data.isBatch == true)
            {
                lstBatch.Add(data);
            }
            else
            {
                if (data.isActive == false)
                {
                    if (_isWithNotMine == false)
                        continue;

                    data = new(dbHero[i].key, _isMine: false);
                }

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

        // 보유한걸 위로 올려주자
        if (_isWithNotMine == true)
            lstNotBatch = lstNotBatch.OrderByDescending(x => x.isMine).ToList();

        result.AddRange(lstNotBatch);

        lstBatch = null;
        lstNotBatch = null;
        return result.Where(x =>
        {
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
        }).ToList();
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

    public void AddHeroSoul(string _key, int _count)
    {
        var heroData = GetHeroInfoData(_key);

        if (heroData.isActive == true)
        {
            heroData.soulCount += _count;
            Update(heroData);
        }
        else
        {
            var grade = TableManager.hero.GetGradeFromSoulCount(_count);
            AddHero(_key, grade);
        }
    }

    public void AddHero(string _key, GradeType _grade = GradeType.Normal, bool _isBatch = false, bool _isMain = false)
        => AddHeroAsync(_key, _grade, _isBatch, _isMain).Forget();

    public async UniTask AddHeroAsync(string _key, GradeType _grade = GradeType.Normal, bool _isBatch = false, bool _isMain = false)
    {
        if (m_element.myHero.Any(x => x.key == _key))
            return;

        m_element.myHero.Add(new(_key, _isMain: _isMain, _isBatch: _isBatch));

        await AddressableManager.instance.Load_HeroIconAsync(_key);
        await AddressableManager.instance.Load_HeroCharacterAsync(m_element.myHero.Where(x => x.isBatch).Select(x => x.skin).ToArray());

        SaveData();
    }

    #region ASSETS
    public long GetAssetAmount(ItemType _itemType)
        => _itemType switch { ItemType.Gold => m_element.gold, ItemType.Rice => m_element.rice, _ => -1 };

    public void AddRice(long _amount, bool _isUpdate = true, bool _isTween = true)
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

    public struct HeroSortData
    {
        public HeroSortType sortType;
        public bool isDescending;

        public List<RegionType> filter_region;
        public List<HeroClassType> filter_class;
        public List<GradeType> filter_grade;

        public void Default()
        {
            sortType = HeroSortType.Grade;
            isDescending = true;

            filter_region = new();
            filter_class = new();
            filter_grade = new();

            int i = 0;
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

        public bool isAll_Region => filter_region.Count == (int)RegionType.MAX;
        public bool isAll_Class => filter_class.Count == (int)HeroClassType.MAX;
        public bool isAll_Grade => filter_grade.Count == (int)GradeType.MAX;
    }
}