namespace March7thHoney.GameServer.Game.GridFight;

/// <summary>
///     爆晶矿 (102601 银 / 202001 金 / 301501 彩, EffectParamList [chance, 21006, 21007, 21008]): "我方小队消灭敌方目标时，有
///     X%概率获得一个战利品晶矿". 21006-21008 are the bonus ids of the white/blue/gold loot orbs 121006-121008. Kills come
///     from the battle statistics; the colour weights (6:3:1) are an estimate, the client data does not give them.
///     Several 爆晶矿 tiers do not stack: the best chance applies.
/// </summary>
public sealed partial class GridFightSession
{
    // Chances from GridFightAugment.EffectParamList[0] (the server excel model does not load that column).
    private static readonly IReadOnlyDictionary<uint, double> LootOrbChances = new Dictionary<uint, double>
    {
        [102601] = 0.20,
        [202001] = 0.30,
        [301501] = 0.45,
    };
    private static readonly (uint OrbId, int Weight)[] LootOrbWeights = [(121006, 6), (121007, 3), (121008, 1)];

    private IReadOnlyList<GridFightOrbState> RollLootOrbsOnKills(IReadOnlyList<GridFightKilledMonsterInstance> killed)
    {
        var chance = LootOrbChances
            .Where(entry => ActiveInvestmentIds.Contains(entry.Key))
            .Select(entry => entry.Value)
            .DefaultIfEmpty(0)
            .Max();
        if (chance <= 0) return [];
        var result = new List<GridFightOrbState>();
        var kills = killed.Select(kill => (kill.WaveIndex, kill.MonsterIndex)).Distinct().Count();
        for (var index = 0; index < kills; index++)
        {
            if (!_catalog.RollChance(chance)) continue;
            var roll = _catalog.NextRandom(0, LootOrbWeights.Sum(entry => entry.Weight));
            var orbId = LootOrbWeights[^1].OrbId;
            foreach (var (candidate, weight) in LootOrbWeights)
            {
                if (roll < weight) { orbId = candidate; break; }
                roll -= weight;
            }
            if (TryAddOrb(orbId, out var orb)) result.Add(orb);
        }
        return result;
    }
}
