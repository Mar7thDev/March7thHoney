using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftWorkshopLevel.json")]
[MemoryPackable]
public partial class MechCraftWorkshopLevelExcel : ExcelResource
{
    [JsonProperty("AAGKEBFHLMC")] public int Level { get; set; }
    [JsonProperty("MPHAIOHJKCD")] public int MinIncome { get; set; }
    [JsonProperty("AGDJPAPAFEO")] public int ExamID { get; set; }

    public override int GetId() => Level;

    public override void Loaded()
    {
        GameData.MechCraftWorkshopLevelData.TryAdd(Level, this);
    }
}
