using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftDesignLevel.json")]
[MemoryPackable]
public partial class MechCraftDesignLevelExcel : ExcelResource
{
    [JsonProperty("AAGKEBFHLMC")] public int Level { get; set; }
    [JsonProperty("KJIFHEPDMEI")] public int MinExp { get; set; }
    [JsonProperty("FACDDCMCMIP")] public List<int> UnlockChipIDList { get; set; } = [];

    public override int GetId() => Level;

    public override void Loaded()
    {
        GameData.MechCraftDesignLevelData.TryAdd(Level, this);
    }
}
