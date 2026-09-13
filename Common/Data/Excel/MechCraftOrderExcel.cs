using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftOrder.json")]
[MemoryPackable]
public partial class MechCraftOrderExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("KHIJNANHBFB")] public int UnlockConditionID { get; set; }
    [JsonProperty("CLGNIABAHII")] public int RequirementID { get; set; }
    [JsonProperty("GGBPEGPAGNJ")] public int BaseIncome { get; set; }
    [JsonProperty("ANCJFPFEGHA")] public int DesignExperience { get; set; }
    [JsonProperty("MEPGCJIPDON")] public List<int> AdvancedIncomeBonuses { get; set; } = [];
    [JsonProperty("JKKJNOPADBO")] public List<int> AdvancedExperienceBonuses { get; set; } = [];
    [JsonProperty("BLAJKCJIAPH")] public int CustomerActorID { get; set; }
    [JsonProperty("AAGKEBFHLMC")] public int Level { get; set; }
    [JsonProperty("NMAHGFAPENI")] public HashName Desc { get; set; } = new();
    [JsonProperty("LNPNCKGNKID")] public HashName CustomerCmt { get; set; } = new();
    [JsonProperty("KHPNJPFNHKN")] public HashName CustomerPraiseCraft { get; set; } = new();

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftOrderData.TryAdd(ID, this);
    }
}
