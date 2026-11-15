using MemoryPack;
namespace March7thHoney.Data.Excel;

// Season ownership of GridFight equipment, consumables and forge items. Season 1 and season 2 share
// GridFightEquipment/Consumables/Forge, so this table is what separates their pools.
[ResourceEntity("GridFightSeasonItem.json")]
[MemoryPackable]
public partial class GridFightSeasonItemExcel : ExcelResource
{
    public uint ItemID { get; set; }
    public uint SeasonID { get; set; }

    public override int GetId()
    {
        return (int)(SeasonID * 100000000 + ItemID);
    }

    public override void Loaded()
    {
        if (!GameData.GridFightSeasonItemData.TryGetValue(ItemID, out var seasons))
            GameData.GridFightSeasonItemData[ItemID] = seasons = [];
        if (!seasons.Contains(SeasonID)) seasons.Add(SeasonID);
    }
}
