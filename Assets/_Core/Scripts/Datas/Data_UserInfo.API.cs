using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Data_UserInfo
{
    public async UniTask API_Login()
    {
        await UniTask.NextFrame();

        if (PPWorker.HasKey(PlayerPrefsType.USER_DATA))
        {
            m_element = PPWorker.Get<ElementData>(PlayerPrefsType.USER_DATA);
        }
        else
        {
            m_element.Default();
            SaveData();
        }

        if (PPWorker.HasKey(PlayerPrefsType.HERO_DATA_SORTING, false))
            m_sortData = PPWorker.Get<HeroSortData>(PlayerPrefsType.HERO_DATA_SORTING, false);
        else
        {
            m_sortData = new();
            m_sortData.Default();
            SaveData_SortingData();
        }

        if (PPWorker.HasKey(PlayerPrefsType.USER_DATA_IDLE_REWARD))
            idleRewardData = PPWorker.Get<IdleRewardData>(PlayerPrefsType.USER_DATA_IDLE_REWARD);
        else
        {
            idleRewardData = new();
            idleRewardData.Default();
            SaveData_IdleReward();
        }
    }

    public async UniTask<List<ItemData>> API_ReceiveIdleReward()
    {
        await API_RefreshIdleReward(true);

        List<ItemData> result = new(idleRewardData.rewards);

        idleRewardData.rewards.Clear();
        idleRewardData.tickReceive = Utils.GetUTC().Ticks;
        SaveData_IdleReward();

        return result;
    }

    public async UniTask API_RefreshIdleReward(bool _isForce = false)
    {
        if (_isForce == true || idleRewardData.tsRefresh.TotalMinutes > 1)
        {
            await UniTask.NextFrame();

            idleRewardData.tickRefresh = Utils.GetUTC().Ticks;

            //int count = (int)idleRewardData.tsReceive.TotalMinutes;
            int count = (int)idleRewardData.tsReceive.TotalSeconds;
            count = Mathf.Min(count, 60 * 12);

            idleRewardData.rewards = new()
            {
                TableManager.item.GetItemData(ItemType.gold, count),
                TableManager.item.GetItemData(ItemType.rice, (int)(count * 1.2f)),
            };

            var item = TableManager.item.GetItemData(ItemType.time_stone, (int)(count * 0.5f));
            if (item.count > 0)
                idleRewardData.rewards.Add(item);

            item = TableManager.item.GetItemData(ItemType.gold, (int)(count * 0.2f));
            if (item.count > 0)
                idleRewardData.rewards.Add(item);

            item = TableManager.item.GetItemData(ItemType.time_stone, (int)(count * 0.3f), HeroClassType.Champion.ToString());
            if (item.count > 0)
                idleRewardData.rewards.Add(item);

            item = TableManager.item.GetItemData(ItemType.rice, (int)(count * 0.3f), HeroClassType.Vanguard.ToString());
            if (item.count > 0)
                idleRewardData.rewards.Add(item);

            item = TableManager.item.GetItemData(ItemType.time_stone, (int)(count * 0.3f), HeroClassType.Strategist.ToString());
            if (item.count > 0)
                idleRewardData.rewards.Add(item);

            SaveData_IdleReward();
        }
    }

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

    public async UniTask<bool> API_SetUserData(string _nickname, string _desc)
    {
        m_element.userInfoData.nickname = _nickname;
        m_element.userInfoData.desc = _desc;
        SaveData();

        TopComponent.instance.SetNickname();

        await UniTask.NextFrame();
        return true;
    }
}
