using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftReport.json")]
[MemoryPackable]
public partial class MechCraftReportExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("FBKAMIHGLFK")] public string ImagePath { get; set; } = string.Empty;
    [JsonProperty("AABNPBGMOFN")] public HashName Content { get; set; } = new();
    [JsonProperty("JONJKELPPAI")] public float LikeCount { get; set; }
    [JsonProperty("BEEMLBPEGKH")] public List<int> CmtList { get; set; } = [];

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftReportData.TryAdd(ID, this);
    }
}
