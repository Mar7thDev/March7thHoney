using March7thHoney.Proto;
using SqlSugar;

namespace March7thHoney.Database.ChessRogue;

[SugarTable("ChessRogueNous")]
public class ChessRogueNousData : BaseDatabaseDataHelper
{
    [SugarColumn(IsJson = true)] public Dictionary<int, ChessRogueNousDiceData> RogueDiceData { get; set; } = [];
}

public class ChessRogueNousDiceData
{
    public int BranchId { get; set; }
    public Dictionary<int, int> Surfaces { get; set; } = [];
    public int AreaId { get; set; }
    public int DifficultyLevel { get; set; }

    // TODO 4.3: DFAOIALEBOH 在 4.3 已消失或更名，棋盘大冒险不在登录路径，暂时移除
}
