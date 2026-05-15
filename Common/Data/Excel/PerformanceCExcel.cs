using March7thHoney.Data.Config;

namespace March7thHoney.Data.Excel;

[ResourceEntity("PerformanceC.json")]
public class PerformanceCExcel : ExcelResource
{
    public int PerformanceID { get; set; }
    public string PerformancePath { get; set; } = "";

    public LevelGraphConfigInfo? ActInfo { get; set; }

    public override int GetId()
    {
        return PerformanceID;
    }

    public override void Loaded()
    {
        GameData.PerformanceCData[GetId()] = this;
    }
}
