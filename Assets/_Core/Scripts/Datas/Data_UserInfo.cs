using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_UserInfo
{
    public int uid;

    List<HeroInfoData> m_dbHero = new();
    public IReadOnlyList<HeroInfoData> dbHero => m_dbHero;

    public async UniTask LoadData()
    {
        if (PPWorker.HasKey(PlayerPrefsType.HERO_LIST))
            m_dbHero = PPWorker.Get<List<HeroInfoData>>(PlayerPrefsType.HERO_LIST);
        else
        {
            //유비가 가장 첫번째임
            m_dbHero.Add(new("Liubei", _isBatch: true, _isMain: true));
            m_dbHero.Add(new("Guanyu", _isBatch: true));
            m_dbHero.Add(new("Zhangfei", _isBatch: true));
            m_dbHero.Add(new("Zhayun", _isBatch: true));
            m_dbHero.Add(new("Zhugeliang"));
            SaveHero();
        }

        await AddressableManager.instance.Load_HeroIcon(m_dbHero.Select(x => x.skin).ToArray());
        await AddressableManager.instance.Load_HeroCharacter(m_dbHero.Where(x => x.isBatch).Select(x => x.skin).ToArray());
    }

    public void SaveHero()
    {
        if (m_dbHero.Count > 1)
            m_dbHero = m_dbHero.OrderByDescending(x => x.isMain).ToList();

        PPWorker.Set(PlayerPrefsType.HERO_LIST, m_dbHero);
    }
}
