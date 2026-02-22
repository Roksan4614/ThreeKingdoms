using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_UserInfo
{
    public int uid;

    List<HeroInfoData> m_myHero = new();
    public IReadOnlyList<HeroInfoData> myHero => m_myHero;

    public async UniTask Initialize()
    {
        if (PPWorker.HasKey(PlayerPrefsType.HERO_LIST))
            m_myHero = PPWorker.Get<List<HeroInfoData>>(PlayerPrefsType.HERO_LIST);
        else
        {
            //유비가 가장 첫번째임
            m_myHero.Add(new("LiuBei", _isBatch: true, _isMain: true));
            m_myHero.Add(new("GuanYu", _isBatch: true));
            m_myHero.Add(new("ZhugeLiang", _isBatch: true));
            m_myHero.Add(new("ZhangFei", _isBatch: true));
            m_myHero.Add(new("ZhaYun"));
            SaveHero();
        }

        await AddressableManager.instance.Load_HeroIcon(m_myHero.Select(x => x.skin).ToArray());
        await AddressableManager.instance.Load_HeroCharacter(m_myHero.Where(x => x.isBatch).Select(x => x.skin).ToArray());
    }

    public void SaveHero()
    {
        if (m_myHero.Count > 1)
            m_myHero = m_myHero.OrderByDescending(x => x.isMain).ToList();

        PPWorker.Set(PlayerPrefsType.HERO_LIST, m_myHero);
    }

    public HeroInfoData GetHeroInfoData(string _key)
        => m_myHero.Where(x => x.key.Equals(_key)).FirstOrDefault();

    public void UpdateAll(List<HeroInfoData> _heroList)
    {
        m_myHero = _heroList;
        SaveHero();
    }
}
