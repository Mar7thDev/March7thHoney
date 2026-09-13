using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftReportCmt.json")]
[MemoryPackable]
public partial class MechCraftReportCmtExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("OOLEAPLDIEA")] public string HeadIcon { get; set; } = string.Empty;
    [JsonProperty("OENAMINOLLF")] public HashName Name { get; set; } = new();
    [JsonProperty("JMBJBDDMNLO")] public HashName Cmt { get; set; } = new();
    [JsonProperty("BNLCCCCMABF")] public int Parent { get; set; }
    [JsonProperty("BEDKAABCNDD")] public bool IsPoster { get; set; }

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftReportCmtData.TryAdd(ID, this);
    }
}
