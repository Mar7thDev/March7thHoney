using MemoryPack;
using Newtonsoft.Json;
namespace March7thHoney.Data.Excel;

[ResourceEntity("PetConfig.json")]
[MemoryPackable]
public partial class PetExcel : ExcelResource
{
    public int PetID { get; set; }
    public int PetItemID { get; set; }
    public int SummonUnitID { get; set; }

    // this table's field names stay obfuscated in the client metadata, so accept both spellings
    [JsonProperty("PBLDLDIEFNC")]
    [MemoryPackIgnore]
    public int PetIDObfuscated
    {
        set => PetID = value;
    }

    [JsonProperty("HHDHIDNHCBI")]
    [MemoryPackIgnore]
    public int PetItemIDObfuscated
    {
        set => PetItemID = value;
    }

    [JsonProperty("FNBGMDDOHEA")]
    [MemoryPackIgnore]
    public int SummonUnitIDObfuscated
    {
        set => SummonUnitID = value;
    }

    public override int GetId()
    {
        return PetID;
    }

    public override void Loaded()
    {
        GameData.PetData.TryAdd(PetID, this);
    }
}
