using MemoryPack;

namespace March7thHoney.Data.Excel;

/// <summary>
///     Level band of one daily-training quest group (DailyQuest.json). The rows cover the same QuestType 5
///     quests as DailyActiveQuestPool.json and are the pool the daily roll draws from.
/// </summary>
[ResourceEntity("DailyQuest.json")]
[MemoryPackable]
public partial class DailyQuestExcel : ExcelResource
{
    public int DailyID { get; set; }
    public List<int> QuestList { get; set; } = [];
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public bool IsDelete { get; set; }

    public override int GetId()
    {
        return DailyID;
    }

    public override void Loaded()
    {
        GameData.DailyQuestData[DailyID] = this;
    }
}
