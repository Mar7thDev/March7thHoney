using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ExpeditionBattleMap.json")]
[MemoryPackable]
public partial class ExpeditionBattleMapExcel : ExcelResource
{
    public int ID { get; set; }
    public int ActivityModuleID { get; set; }
    public List<int> RouteIDList { get; set; } = [];

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.ExpeditionBattleMapData.TryAdd(ID, this);
    }
}
