using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Data.Custom;

public enum GridFightTalentRewardTrigger
{
    PlaneEnter,
    BossClear,
}

[MemoryPackable]
public partial class GridFightRewardRulesConfig
{
    public Dictionary<uint, GridFightRewardNodeProfile> NodeProfiles { get; set; } = [];
    public List<GridFightTalentRewardRule> TalentRules { get; set; } = [];
}

[MemoryPackable]
public partial class GridFightRewardNodeProfile
{
    public List<uint> MonsterTiers { get; set; } = [];
    public uint OrbRollCount { get; set; }
    public uint MinimumOrbPerMonster { get; set; } = 1;
    public uint FormationWaveId { get; set; } = 5;
    public uint RoleStar { get; set; } = 1;
    public Dictionary<uint, uint> OrbWeights { get; set; } = [];
}

[MemoryPackable]
public partial class GridFightTalentRewardRule
{
    public uint TalentId { get; set; }
    public uint EffectId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightTalentRewardTrigger Trigger { get; set; }

    public List<uint> ChapterIds { get; set; } = [];
    public List<uint> OrbIds { get; set; } = [];
}
