using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftDiyPaintColor.json")]
[MemoryPackable]
public partial class MechCraftDiyPaintColorExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("FBKAMIHGLFK")] public string ImagePath { get; set; } = string.Empty;

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftDiyPaintColorData.TryAdd(ID, this);
    }
}
