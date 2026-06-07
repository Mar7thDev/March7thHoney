using MemoryPack;
using March7thHoney.Data.Excel;

namespace March7thHoney.Data.Custom;

[MemoryPackable]
public partial class GridFightBasicOrbRewardsConfig
{
    public Dictionary<uint, GridFightBasicOrbRewardsInfo> OrbRewards { get; set; } = [];
}

[MemoryPackable]
public partial class GridFightBasicOrbRewardsInfo
{
    public uint OrbId { get; set; }
    public Dictionary<uint, List<GridFightBasicBonusPoolV2Excel>> Rewards { get; set; } = [];
}
