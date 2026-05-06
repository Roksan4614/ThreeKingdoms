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

        await UniTask.WhenAll(tasks);
    }

    public TableStatData GetResultStat(HeroInfoData _heroInfoData)
    {
        var result = TableManager.statHero.GetStatData(_heroInfoData);

        //var statData = GetBonusStatData(_heroInfoData);

        return result;
    }

    TableStatData GetBonusStatData(HeroInfoData _heroInfoData)
    {
        var statData = new TableStatData();

        return statData;
    }
}
