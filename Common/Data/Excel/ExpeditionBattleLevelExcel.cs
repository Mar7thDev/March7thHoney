using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ExpeditionBattleLevel.json")]
[MemoryPackable]
public partial class ExpeditionBattleLevelExcel : ExcelResource
{
    public int ID { get; set; }
    public int StageID { get; set; }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.ExpeditionBattleLevelData.TryAdd(ID, this);
    }
}
