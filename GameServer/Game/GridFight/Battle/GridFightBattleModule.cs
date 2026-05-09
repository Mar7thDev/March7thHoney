using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.GridFight.Battle;

public static class GridFightBattleModule
{
    public static BattleInstance? StartBattle(PlayerInstance player, GridFightInstance gridFightInstance)
    {
        if (player.BattleInstance != null)
            return player.BattleInstance;

        var lineup = player.LineupManager?.GetCurLineup();
        if (lineup == null)
            return null;

        StageConfigExcel? stageConfig = null;
        if (gridFightInstance.BattleComponent.StageId > 0 &&
            GameData.StageConfigData.TryGetValue((int)gridFightInstance.BattleComponent.StageId, out var gridStage))
        {
            stageConfig = gridStage;
        }

        if (stageConfig == null)
        {
            foreach (var stageId in gridFightInstance.StageCandidates)
            {
                if (!GameData.StageConfigData.TryGetValue((int)stageId, out var found)) continue;
                stageConfig = found;
                break;
            }
        }

        stageConfig ??= GameData.StageConfigData.Values.FirstOrDefault();
        if (stageConfig == null)
            return null;

        var battle = new BattleInstance(player, lineup, [stageConfig])
        {
            WorldLevel = player.Data.WorldLevel
        };

        player.BattleManager!.NextBattleMonsterIds = gridFightInstance.BattleComponent.MonsterIds.Select(x => (int)x).ToList();
        player.BattleInstance = battle;
        return battle;
    }
}
