using MemoryPack;

namespace March7thHoney.Data.Excel;

/// <summary>How many daily missions of one DailyMissionType are offered per day (DailyMissionCount.json).</summary>
[ResourceEntity("DailyMissionCount.json")]
[MemoryPackable]
public partial class DailyMissionCountExcel : ExcelResource
{
    public int ID { get; set; }
    public int DailyMissionType { get; set; }
    public int DailyCount { get; set; }

    public override int GetId()
    {
        return DailyMissionType;
    }

    public override void Loaded()
    {
        GameData.DailyMissionCountData[DailyMissionType] = this;
    }
}
