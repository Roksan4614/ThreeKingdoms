using System.Collections.Generic;
using System.Linq;
using ThreeKingdoms.Client.Server;

public partial class Data_Stat_Relic
{
    private ThreeKingdoms.Shared.Types.CharacterSnapshotDto m_projectionSnapshot;
    private bool m_hasServerProjection;
    private void RefreshServerProjection()
    {
        if (m_hasServerProjection && ReferenceEquals(m_projectionSnapshot, ServerState.Characters)) return;
        m_bonusClassBonus.Clear();
        m_bonusTreasureBonus.Clear();
        for (var type = HeroClassType.NONE + 1; type < HeroClassType.MAX; type++) m_bonusClassBonus[type] = 0;
        foreach (var hero in ServerState.Characters?.Characters ?? new())
        {
            var master = TableManager.hero.Get(hero.CharacterKey);
            if (master != null) m_bonusClassBonus[master.classType] += hero.Relic.EnhanceStage;
        }
        m_dataTreasure = ServerState.Characters?.Treasures.Select(treasure => new TreasureBatchData
        {
            key = treasure.TreasureKey, isBatch = treasure.EquippedSlot.HasValue,
            tickBatch = treasure.EquippedSlot.HasValue ? treasure.EquippedSlot.Value + 1 : 0
        }).ToList() ?? new List<TreasureBatchData>();
        foreach (var treasure in m_dataTreasure.Where(x => x.isBatch)) SetBonusTreasureBonus(treasure.key, true);
        m_projectionSnapshot = ServerState.Characters;
        m_hasServerProjection = true;
    }
}
