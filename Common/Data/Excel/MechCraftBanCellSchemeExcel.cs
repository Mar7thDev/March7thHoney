using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftBanCellScheme.json")]
[MemoryPackable]
public partial class MechCraftBanCellSchemeExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("NJMFEDCACPC")] public int Width { get; set; }
    [JsonProperty("GMFGFLLHPJJ")] public int Height { get; set; }
    [JsonProperty("EMCCLPLCOKG")] public MechCraftBanCellAxesDirectionType AxesDirection { get; set; }
    [JsonProperty("DIIINDCGMBE")] public List<MechCraftCell> Cells { get; set; } = [];

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftBanCellSchemeData.TryAdd(ID, this);
    }
}
