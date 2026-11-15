using MemoryPack;
namespace March7thHoney.Data.Excel;

// Season 2 partners ("PetPal"). Every season-2 run carries one partner whose effects come from EffIDList;
// the client's game flow reads the first synced effect as soon as the run starts.
[ResourceEntity("GridFightPetPal.json")]
[MemoryPackable]
public partial class GridFightPetPalExcel : ExcelResource
{
    public uint ID { get; set; }
    public List<uint> EffIDList { get; set; } = [];
    public uint Sort { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightPetPalData[ID] = this;
    }
}
