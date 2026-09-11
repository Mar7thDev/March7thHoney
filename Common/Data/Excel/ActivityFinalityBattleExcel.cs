using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ActivityFinalityBattle.json")]
[MemoryPackable]
public partial class ActivityFinalityBattleExcel : ExcelResource
{
    public int ID { get; set; }
    public int Order { get; set; }
    public List<int> LevelList { get; set; } = [];
    public int ActivityModuleID { get; set; }
    public List<int> LeadingRoleList { get; set; } = [];
    public int PerfectRound { get; set; }
    public int TutorialGuideID { get; set; }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.ActivityFinalityBattleData.TryAdd(ID, this);
    }
}
