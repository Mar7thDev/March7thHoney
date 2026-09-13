using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ExpeditionBattleBuff.json")]
[MemoryPackable]
public partial class ExpeditionBattleBuffExcel : ExcelResource
{
    public int ID { get; set; }
    public int MazeBuffID { get; set; }
    public ulong BuffRank { get; set; }
    public bool BuffDataIsPercentage { get; set; }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.ExpeditionBattleBuffData.TryAdd(ID, this);
    }
}
