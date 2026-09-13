using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Activity.ExpeditionBattle;

public readonly record struct ExpeditionBattleAvatarSelection(int Id, AvatarType AvatarType);

public readonly record struct ExpeditionBattleFunEntry(int FunId, int AvatarId, ulong FunCount);

public sealed class ExpeditionBattleLevelResult
{
    public int LevelId { get; init; }
    public Dictionary<int, ulong> AvatarDamage { get; } = [];
}

public sealed class ExpeditionBattleRouteResult
{
    public int RouteId { get; init; }
    public int PassedLevel { get; set; }
    public int BattleRoundCount { get; set; }
    public int LastLevelPercent { get; set; }
    public ulong BuffValue { get; set; }
    public bool IsNewRecord { get; set; }
    public List<ExpeditionBattleAvatarSelection> Avatars { get; } = [];
    public List<ExpeditionBattleFunEntry> FunList { get; } = [];
    public List<ExpeditionBattleLevelResult> Levels { get; } = [];
}

public sealed class ExpeditionBattleMapState(int mapId, int remainingChallengeCount)
{
    public int MapId { get; } = mapId;
    public int RemainingChallengeCount { get; set; } = remainingChallengeCount;
    public Dictionary<int, List<ExpeditionBattleAvatarSelection>> Lineups { get; } = [];
    public Dictionary<int, ExpeditionBattleRouteResult> BestRecords { get; } = [];
}
