namespace March7thHoney.Database.ChessRogue;

[DbTable("ChessRogue")]
public class ChessRogueData : BaseDatabaseDataHelper
{
    // MemoryPack-serialized ChessRogueRunData, base64.
    public string? RunInstance { get; set; }
}
