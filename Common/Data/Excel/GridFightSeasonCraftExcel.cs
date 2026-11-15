using MemoryPack;
namespace March7thHoney.Data.Excel;

// Season ownership of GridFightCraftConfig recipes (keyed by CraftID).
[ResourceEntity("GridFightSeasonCraft.json")]
[MemoryPackable]
public partial class GridFightSeasonCraftExcel : ExcelResource
{
    public uint CraftID { get; set; }
    public uint SeasonID { get; set; }

    public override int GetId()
    {
        return (int)(SeasonID * 100000 + CraftID);
    }

    public override void Loaded()
    {
        if (!GameData.GridFightSeasonCraftData.TryGetValue(CraftID, out var seasons))
            GameData.GridFightSeasonCraftData[CraftID] = seasons = [];
        if (!seasons.Contains(SeasonID)) seasons.Add(SeasonID);
    }
}
