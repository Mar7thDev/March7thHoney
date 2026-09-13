using March7thHoney.Data;

namespace March7thHoney.Database.Rogue;

[DbTable("Rogue")]
public class RogueData : BaseDatabaseDataHelper
{
    public List<int> Talents { get; set; } = [];
    public int FinishCnt { get; set; }
    public int ExploreScore { get; set; }
    public List<int> FirstPassAreas { get; set; } = [];

    // Difficulty tier per area; a won run advances it, which moves the next run to the next map.
    public Dictionary<int, int> AreaDifficulty { get; set; } = [];

    // Base64 MemoryPack blob of the live run so a mid-run relog resumes.
    public string? RunBlob { get; set; }
}
