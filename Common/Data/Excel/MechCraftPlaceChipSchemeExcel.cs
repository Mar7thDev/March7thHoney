using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftPlaceChipScheme.json")]
[MemoryPackable]
public partial class MechCraftPlaceChipSchemeExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("LNKOLCOBLMO")] public int Index { get; set; }
    [JsonProperty("PFDHNIJBGKO")] public int ChipID { get; set; }
    [JsonProperty("BBFOLEOPPPL")] public MechCraftCell Position { get; set; } = new();
    [JsonProperty("KBDAHAAAIBO")] public int RotationDegrees { get; set; }

    public override int GetId() => ID;

    public override void Loaded()
    {
        if (!GameData.MechCraftPlaceChipSchemeData.TryGetValue(ID, out var rows))
            GameData.MechCraftPlaceChipSchemeData[ID] = rows = [];
        rows.Add(this);
    }
}
