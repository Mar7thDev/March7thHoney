using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ExpeditionBattleRoute.json")]
[MemoryPackable]
public partial class ExpeditionBattleRouteExcel : ExcelResource
{
    public int ID { get; set; }
    public List<int> LevelIDList { get; set; } = [];
    public int BuffID { get; set; }
    public List<int> SpecialAvatarIDList { get; set; } = [];
    public int MainMonster { get; set; }
    public int MazeBuffID { get; set; }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.ExpeditionBattleRouteData.TryAdd(ID, this);
    }
}
