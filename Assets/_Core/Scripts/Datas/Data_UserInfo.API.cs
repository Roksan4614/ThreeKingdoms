using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Data_UserInfo
{
    public async UniTask<HeroInfoData> API_ChangeTraits(string _keyHero)
    {
        var hero = m_element.myHero.Find(x => x.key == _keyHero);

        if (hero.traits == null)
            hero.traits = new();

        // 락 안걸린것들 지우기
        var unlockTraits = hero.traits.FindAll(x => x.isLock == false);

        await UniTask.NextFrame();

        // 새 특성 가져오기
        foreach (var td in unlockTraits)
        {
            td.type = TableManager.traits.list.RandomFirst().type;
            td.indexValue = TableManager.traitsValue.GetGroupRandomIndex(td.type);
            td.ResetTraitsValueData();
        }

        for (int i = hero.traits.Count; i < hero.countOpenTraits; i++)
        {
            HeroTraitsData traitData = new();
            traitData.index = i;
            traitData.type = TableManager.traits.list.RandomFirst().type;
            traitData.indexValue = TableManager.traitsValue.GetGroupRandomIndex(traitData.type);

            hero.traits.Add(traitData);
        }

        hero.ResetResultStat();
        SaveData();

        return hero.DeepClone();
    }

}
