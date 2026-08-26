using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class Data_Stat
{
    public Data_Stat_Relic relic { get; private set; } = new();
    public Data_Stat_FriendShip friendShip { get; private set; } = new();

    public async UniTask InitializeAsync()
    {
        List<UniTask> tasks = new();

        tasks.Add(relic.InitializeAsync());
        tasks.Add(friendShip.InitializeAsync());

        await UniTask.WhenAll(tasks.ToArray());
    }

    public TableStatData GetResultStat(HeroInfoData _heroInfoData)
    {
        var result = TableManager.statHero.GetStatData(_heroInfoData);

        if (result == null)
            result = new();

        // 내께 아니면 혹시 모르니 기본만..
        if (_heroInfoData.isMine == false)
            return result;

        // todo
        //var statData = GetBonusStatData(_heroInfoData);

        return result;
    }

    TableStatData GetBonusStatData(HeroInfoData _heroInfoData)
    {
        var statData = new TableStatData();

        return statData;
    }
}
