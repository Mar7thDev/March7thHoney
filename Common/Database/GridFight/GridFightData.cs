namespace March7thHoney.Database.GridFight;

[DbTable("GridFight")]
public class GridFightData : BaseDatabaseDataHelper
{
    // Base64 MemoryPack GridFightRunSnapshot of the live run (GameServer side); null when no run is open.
    public string? RunInstance { get; set; }

    // Session unique ids keep increasing across runs and restarts.
    public uint NextSessionUniqueId { get; set; } = 1;
}
