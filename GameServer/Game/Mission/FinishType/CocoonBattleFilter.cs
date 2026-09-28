using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Mission.FinishType;

/// <summary>Shared matching for the cocoon finish types (CocoonFinish / FarmTypeCocoonFinish / AnyCocoonFinish).</summary>
public static class CocoonBattleFilter
{
    /// <summary>
    ///     A won cocoon battle: the battle-finish arg, won, started from a CocoonConfig row. Farm-element battles
    ///     also set CocoonWave but never a CocoonId, so they are excluded; heartbeats pass no BattleInstance.
    /// </summary>
    public static bool TryGetWonCocoon(object? arg, out BattleInstance battle)
    {
        battle = null!;
        if (arg is not BattleInstance instance) return false;
        if (instance.BattleResult?.EndStatus != BattleEndStatus.BattleEndWin) return false;
        if (instance.CocoonWave <= 0 || instance.CocoonId <= 0) return false;
        battle = instance;
        return true;
    }

    /// <summary>The CocoonConfig row of the battle, preferring its own world level.</summary>
    public static CocoonConfigExcel? GetConfig(BattleInstance battle)
    {
        if (GameData.CocoonConfigData.TryGetValue(battle.CocoonId * 100 + battle.WorldLevel, out var config))
            return config;
        for (var worldLevel = 0; worldLevel < 10; worldLevel++)
            if (GameData.CocoonConfigData.TryGetValue(battle.CocoonId * 100 + worldLevel, out config))
                return config;
        return null;
    }

    /// <summary>ListContain semantics: an empty list accepts anything.</summary>
    public static bool ListAccepts(List<int>? paramList, int value)
    {
        return paramList is not { Count: > 0 } || paramList.Contains(value);
    }
}
