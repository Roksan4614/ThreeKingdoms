using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_UserInfo
{
    public int uid;
    public RegionType region = RegionType.Shu;

    List<HeroInfoData> m_myHero = new();
    public IReadOnlyList<HeroInfoData> myHero => m_myHero;

    public async UniTask Initialize()
    {
        if (PPWorker.HasKey(PlayerPrefsType.HERO_LIST))
        {
            m_myHero = PPWorker.Get<List<HeroInfoData>>(PlayerPrefsType.HERO_LIST);

            await AddressableManager.instance.Load_HeroIcon(m_myHero.Select(x => x.skin).ToArray());
            await AddressableManager.instance.Load_HeroCharacter(m_myHero.Where(x => x.isBatch).Select(x => x.skin).ToArray());
        }
    }

    public void SaveData()
    {
        if (m_myHero.Count > 1)
            m_myHero = m_myHero.OrderByDescending(x => x.isMain).ToList();

        PPWorker.Set(PlayerPrefsType.HERO_LIST, m_myHero);
    }

    public HeroInfoData GetHeroInfoData(string _key)
        => m_myHero.Where(x => x.key.Equals(_key)).FirstOrDefault();

    public void Update(HeroInfoData _heroData)
    {
        var index = m_myHero.FindIndex(x => x.key.Equals(_heroData.key));
        m_myHero[index] = _heroData;
        SaveData();
    }

    public void UpdateAll(List<HeroInfoData> _heroList)
    {
        m_myHero = _heroList;

        SaveData();
    }

    public void SortTeamPosition(List<HeroInfoData> _heroList)
    {
        // key와 index를 매핑한 딕셔너리 생성
        var indexMap = _heroList
            .Select((_item, _idx) => new { _item.key, _idx })
            .ToDictionary(x => x.key, x => x._idx);

        m_myHero = m_myHero.OrderBy(x =>
        {
            if (indexMap.TryGetValue(x.key, out int index))
            {
                return index;
            }
            return int.MaxValue;
        }).ToList();

        SaveData();
    }
}
