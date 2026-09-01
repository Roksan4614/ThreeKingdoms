using Cysharp.Threading.Tasks;
using Rev9.Tournament;
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

        Dictionary<BattleStatType, float> bonusRate = new();

        // 유물
        {
            var heroRelicBonus = _heroInfoData.relicLevel * 10;
            var classBonus = relic.bonusClassBonus[_heroInfoData.classType];
            var resultBonus = heroRelicBonus + classBonus;

            bonusRate.Add(BattleStatType.attack_power, resultBonus);
            bonusRate.Add(BattleStatType.defence, resultBonus);
            bonusRate.Add(BattleStatType.health_max, resultBonus);
        }

        //특성
        if (_heroInfoData.traits != null)
        {
            foreach (var trait in _heroInfoData.traits)
            {
                var sd = TableManager.traitsValue.GetStatData(trait.type, trait.indexValue);

                if (bonusRate.ContainsKey(sd.statType))
                    bonusRate[sd.statType] += sd.value;
                else
                    bonusRate.Add(sd.statType, sd.value);
            }
        }

        // 보물
        if (_heroInfoData.isTournament)
        {
            var batchData = TournamentWorker.instance.GetBatchData(_heroInfoData.isTournament_Attack);

            foreach (var bs in batchData.treasure)
            {
                var db = TableManager.treasure.Get(bs.key);

                foreach (var effect in db.dbEffect)
                {
                    if (bonusRate.ContainsKey(effect.Key))
                        bonusRate[effect.Key] += effect.Value.value;
                    else
                        bonusRate.Add(effect.Key, effect.Value.value);
                }
            }
        }
        else
        {
            foreach (var bs in relic.bonusTreasureBonus)
            {
                if (bonusRate.ContainsKey(bs.Key))
                    bonusRate[bs.Key] += bs.Value.value;
                else
                    bonusRate.Add(bs.Key, bs.Value.value);
            }
        }

        // 인연
        foreach (var bs in friendShip.bonusStatBonus)
        {
            if (bonusRate.ContainsKey(bs.Key))
                bonusRate[bs.Key] += bs.Value.value;
            else
                bonusRate.Add(bs.Key, bs.Value.value);
        }

        // 최종합산
        foreach (var br in bonusRate)
        {
            var stat = result.GetStatData(br.Key);
            var resultStat = stat + stat * br.Value * 0.01f;
            result.SetStatData(br.Key, resultStat);
        }

        return result;
    }

}
