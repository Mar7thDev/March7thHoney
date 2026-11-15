using MemoryPack;

namespace March7thHoney.Database.Friend;

/// <summary>
///     Support-character rewards the owner has earned: lifetime use count, today's earned units, and the pending
///     bucket per borrower until claimed.
/// </summary>
[DbTable("assist_reward_data")]
public class AssistRewardData : BaseDatabaseDataHelper
{
    public uint TotalAssists { get; set; }
    public List<AssistRewardEntry> Pending { get; set; } = [];
    public int EarnDate { get; set; }
    public uint EarnedToday { get; set; }

    public uint PendingUnits => (uint)Pending.Sum(entry => (long)entry.Count);
}

[MemoryPackable]
public partial class AssistRewardEntry
{
    public int Uid { get; set; }
    public uint Count { get; set; }
}
