using MemoryPack;

namespace March7thHoney.Database.Activity;

[DbTable("ExpeditionBattle")]
public class ExpeditionBattleData : BaseDatabaseDataHelper
{
    public Dictionary<uint, List<ExpeditionBattleLineupData>> Lineups { get; set; } = [];
    public HashSet<uint> CompletedMaps { get; set; } = [];
}

[MemoryPackable]
public partial class ExpeditionBattleLineupData
{
    public uint RouteId { get; set; }
    public List<ExpeditionBattleAvatarData> Avatars { get; set; } = [];
}

[MemoryPackable]
public partial class ExpeditionBattleAvatarData
{
    public uint AvatarId { get; set; }
    public int AvatarType { get; set; }
}
