using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftTalentPoint.json")]
[MemoryPackable]
public partial class MechCraftTalentPointExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("LJGHBEFPOPP")] public int RequiredWorkshopLevel { get; set; }
    [JsonProperty("KDKPDJNMMCM")] public int Price { get; set; }
    [JsonProperty("FODGHIDJAPP")] public List<int> EffectIDList { get; set; } = [];
    [JsonProperty("NMAHGFAPENI")] public HashName Desc { get; set; } = new();
    [JsonProperty("ABJGONAEFCB")] public string RoundIcon { get; set; } = string.Empty;

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftTalentPointData.TryAdd(ID, this);
    }
}
