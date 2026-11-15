using MemoryPack;

namespace March7thHoney.Data.Excel;

// Older archives contain SeasonID and MonsterList directly in GridFightCamp.
[ResourceEntity("GridFightCampSeason.json", OptionalFileNames = "GridFightCampSeason.json")]
[MemoryPackable]
public partial class GridFightCampSeasonExcel : ExcelResource
{
    public uint CampID { get; set; }
    public uint SeasonID { get; set; }
    public List<uint> MonsterList { get; set; } = [];
}
