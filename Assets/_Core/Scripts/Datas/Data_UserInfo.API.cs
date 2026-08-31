using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Data_UserInfo
{
    public async UniTask<HeroInfoData> API_TraitsChange(string _keyHero)
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
            td.type = TableManager.traits.GetTraitRandom(td.index == 2).type;
            td.indexValue = TableManager.traitsValue.GetGroupRandomIndex(td.type);
            td.ResetTraitsValueData();
        }

        for (int i = hero.traits.Count; i < hero.countOpenTraits; i++)
        {
            HeroTraitsData traitData = new();
            traitData.index = i;
            traitData.type = TableManager.traits.GetTraitRandom(i == 2).type;
            traitData.indexValue = TableManager.traitsValue.GetGroupRandomIndex(traitData.type);

            hero.traits.Add(traitData);
        }

        hero.ResetResultStat();
        SaveData();

        return hero.DeepClone();
    }

    public async UniTask<bool> API_TraitsLock(string _keyHero, int _index)
    {
        var trait = m_element.myHero.Find(x => x.key == _keyHero)?.traits.Find(x => x.index == _index);

        if (trait == null)
        {
            PopupManager.instance.AlertShow("특성을_찾을_수_없습니다.");
            return false;
        }

        trait.isLock = !trait.isLock;
        SaveData();

        await UniTask.NextFrame();
        return true;
    }
}
