using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftRequirement.json")]
[MemoryPackable]
public partial class MechCraftRequirementExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("ENGGEFINDPH")] public List<int> BanCellSchemeIDs { get; set; } = [];
    [JsonProperty("IBNALLEGIFH")] public Dictionary<MechCraftAttributeType, int> AttributeLevels { get; set; } = [];
    [JsonProperty("KEDCLDIKAGC")] public List<MechCraftChipTraitType> TraitTypes { get; set; } = [];
    [JsonProperty("PCEHNLCLKGH")] public List<int> ConditionIDs { get; set; } = [];
    [JsonProperty("ADEJDFIJOKF")] public bool IsConditionRequired { get; set; }
    [JsonProperty("BFANELAELAK")] public int RuleValue { get; set; }

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftRequirementData.TryAdd(ID, this);
    }
}
