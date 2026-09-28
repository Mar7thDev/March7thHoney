using MemoryPack;

namespace March7thHoney.Data.Excel;

/// <summary>
///     Daily mission roster (DailyMissionData.json). Each ID is a manually taken main mission of type Daily.
/// </summary>
[ResourceEntity("DailyMissionData.json")]
[MemoryPackable]
public partial class DailyMissionDataExcel : ExcelResource
{
    public int ID { get; set; }
    public int DailyMissionType { get; set; }
    public int GroupID { get; set; }
    public int UnlockMainMission { get; set; }
    public int QuestID { get; set; }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.DailyMissionDataData[ID] = this;
    }
}
