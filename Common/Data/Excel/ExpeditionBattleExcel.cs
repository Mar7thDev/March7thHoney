using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ExpeditionBattleMap.json")]
[MemoryPackable]
public partial class ExpeditionBattleMapExcel : ExcelResource
{
    public int ID { get; set; }
    public int ActivityModuleID { get; set; }
    public List<int> RouteIDList { get; set; } = [];
    public HashName MapName { get; set; } = new();
    public string MapIconPath { get; set; } = "";
    public string ExpeditionBackgroundPrefabPath { get; set; } = "";
    public string ExpeditionBGM { get; set; } = "";

    public override int GetId() => ID;

    public override void Loaded() => GameData.ExpeditionBattleMapData.TryAdd(ID, this);
}

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
    public string MonsterFigurePath { get; set; } = "";

    public override int GetId() => ID;

    public override void Loaded() => GameData.ExpeditionBattleRouteData.TryAdd(ID, this);
}

[ResourceEntity("ExpeditionBattleLevel.json")]
[MemoryPackable]
public partial class ExpeditionBattleLevelExcel : ExcelResource
{
    public int ID { get; set; }
    public int StageID { get; set; }

    public override int GetId() => ID;

    public override void Loaded() => GameData.ExpeditionBattleLevelData.TryAdd(ID, this);
}
