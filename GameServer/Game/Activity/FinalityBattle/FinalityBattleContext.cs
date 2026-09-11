using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.Lineup;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Activity.FinalityBattle;

public readonly record struct FinalityBattleAvatarSelection(int Id, AvatarType AvatarType);

public sealed class FinalityBattleContext(
    ActivityFinalityBattleExcel group,
    FinalityBattleLevelExcel level,
    List<AvatarLineupData> lineupOverride)
{
    public ActivityFinalityBattleExcel Group { get; } = group;
    public FinalityBattleLevelExcel Level { get; } = level;
    public List<AvatarLineupData> LineupOverride { get; } = lineupOverride;
    public HashSet<int> FinishedTargetIds { get; } = [];
}

public sealed class FinalityBattleLevelRecord
{
    public int LevelId { get; set; }
    public int FinishedWave { get; set; }
    public int RemainingRound { get; set; }
    public HashSet<int> FinishedBattleTargetIds { get; } = [];
}
