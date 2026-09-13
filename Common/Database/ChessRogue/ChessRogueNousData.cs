using MemoryPack;

namespace March7thHoney.Database.ChessRogue;

// Per-branch dice loadouts edited in the Nous dice workshop; the run itself lives in ChessRogueData.
[DbTable("ChessRogueNous")]
public class ChessRogueNousData : BaseDatabaseDataHelper
{
    public Dictionary<int, ChessRogueNousDiceData> RogueDiceData { get; set; } = [];
}

[MemoryPackable]
public partial class ChessRogueNousDiceData
{
    public int BranchId { get; set; }

    // Slot (1..6) -> surface id.
    public Dictionary<int, int> Surfaces { get; set; } = [];
    public int AreaId { get; set; }
    public int DifficultyLevel { get; set; }
}
