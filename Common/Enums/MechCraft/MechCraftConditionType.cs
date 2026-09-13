using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Enums.MechCraft;

[JsonConverter(typeof(StringEnumConverter))]
public enum MechCraftConditionType
{
    [EnumMember(Value = "NLCDGIPGFDJ")]
    None = 0,
    [EnumMember(Value = "NDGDDODNDMP")]
    And = 1,
    [EnumMember(Value = "FBBPJEHGBKM")]
    WorkshopLevelReach = 2,
    [EnumMember(Value = "MPBCHLMIGOG")]
    DesignLevelReach = 3,
    [EnumMember(Value = "NKEHAKMAHCN")]
    AllAttributeLevelReach = 4,
    [EnumMember(Value = "CFPCDJGEBDH")]
    ExamPass = 5,
    [EnumMember(Value = "LABLGILFHFC")]
    ChipCountGE = 6,
    [EnumMember(Value = "FPDMBKKIJHG")]
    ChipCountLE = 7,
    [EnumMember(Value = "APFOFHLKBAC")]
    ChipTypeCountGE = 8,
    [EnumMember(Value = "IJPGGPMMHCF")]
    ChipTypeCountLE = 9,
    [EnumMember(Value = "JCPLLMLHPGF")]
    ChipTraitCountGE = 10,
    [EnumMember(Value = "DCDKNLECHEP")]
    ChipTraitCountLE = 11,
    [EnumMember(Value = "OGAPKAILJNA")]
    ChipTypeCount = 12,
    [EnumMember(Value = "GGBCIGOKHBL")]
    ChipCount = 13,
    [EnumMember(Value = "ICBLGHEDCLO")]
    IntegrationPercentGE = 14,
    [EnumMember(Value = "GFBJMGMCIAF")]
    TargetChipCount = 15,
    [EnumMember(Value = "EHBAFDGFMHA")]
    TargetChipUsed = 16,
    [EnumMember(Value = "OPHCNOPFEJI")]
    TargetChipsOnly = 17,
    [EnumMember(Value = "CFFENIKFKML")]
    CoverageSymmetrical = 18,
}
