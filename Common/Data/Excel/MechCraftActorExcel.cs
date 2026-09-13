using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftActor.json")]
[MemoryPackable]
public partial class MechCraftActorExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("OENAMINOLLF")] public HashName Name { get; set; } = new();
    [JsonProperty("HIKPBAFAIMF")] public string HeadImage { get; set; } = string.Empty;
    [JsonProperty("HLDFOGCMOMM")] public string PortraitImage { get; set; } = string.Empty;

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftActorData.TryAdd(ID, this);
    }
}
