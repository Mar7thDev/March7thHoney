using MemoryPack;

namespace March7thHoney.Database.RogueTourn;

[DbTable("RogueTourn")]
public class RogueTournData : BaseDatabaseDataHelper
{
    public int Exp { get; set; }
    public List<int> TakenLevelRewards { get; set; } = [];
    public List<int> UnlockDifficulty { get; set; } = [];
    public List<int> PermanentTalents { get; set; } = [];
    public Dictionary<int, int> SeasonTalents { get; set; } = [];
    public Dictionary<int, RogueTournAreaProgressData> AreaProgress { get; set; } = [];
    public RogueTournWeekChallengeData WeekChallenge { get; set; } = new();
    public List<RogueTournArchiveData> Archives { get; set; } = [];
    public List<RogueTournBuildRefData> BuildRefs { get; set; } = [];
    public RogueTournDivisionData Division { get; set; } = new();

    // 0 = not chosen yet; the manager falls back to the lowest RoguePersonaStyle id.
    public int PersonaStyle { get; set; }

    public string? RunInstance { get; set; }

    // Discovery ledger behind the query's handbook arms. Added later: rows written before it load as null.
    public RogueTournHandbookData? Handbook { get; set; } = new();
}

/// <summary>
///     The ids the player has actually met, per handbook family (formulas 31xxxxx, blessings 617xxx, curios 9xxx,
///     events, workbenches 101-108). The official lists are discoveries, never a dump of the tables.
/// </summary>
[MemoryPackable]
public partial class RogueTournHandbookData
{
    public List<int> Formulas { get; set; } = [];
    public List<int> Buffs { get; set; } = [];
    public List<int> Miracles { get; set; } = [];
    public List<int> Events { get; set; } = [];
    public List<int> Workbenches { get; set; } = [];

    public static bool Discover(List<int> family, int id)
    {
        if (id == 0 || family.Contains(id)) return false;
        family.Add(id);
        return true;
    }
}

[MemoryPackable]
public partial class RogueTournAreaProgressData
{
    public bool IsUnlocked { get; set; }
    public bool IsFirstPassed { get; set; }
    public bool IsTakenReward { get; set; }
}

[MemoryPackable]
public partial class RogueTournWeekChallengeData
{
    public int Id { get; set; }
    public long EndTime { get; set; }
    public bool HasGained { get; set; }
    public int ExtraExp { get; set; }
}

[MemoryPackable]
public partial class RogueTournArchiveData
{
    public int Slot { get; set; }
    public string Name { get; set; } = "";
    public List<int> Avatars { get; set; } = [];
    public string Data { get; set; } = "";
    public long Time { get; set; }
    public long EndTime { get; set; }
}

[MemoryPackable]
public partial class RogueTournBuildRefData
{
    public int Slot { get; set; }
    public string Name { get; set; } = "";
    public List<int> Avatars { get; set; } = [];
    public string Data { get; set; } = "";
    public long Time { get; set; }
}

[MemoryPackable]
public partial class RogueTournDivisionData
{
    public int Level { get; set; }
    public int Progress { get; set; }
}
