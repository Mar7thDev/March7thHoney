using MemoryPack;

namespace March7thHoney.Database.ChessRogue;

public static class ChessRogueStates
{
    // ChessRogueLevelStatus (GDHLDNFLOOF): Idle, Processing, Pending, Finish, Failed, ForceFinish.
    public const int LevelProcessing = 1;
    public const int LevelFinish = 3;
    public const int LevelFailed = 4;

    // ChessRogueBoardCellStatus.
    public const int CellIdle = 0;
    public const int CellSelected = 1;
    public const int CellProcessing = 2;
    public const int CellFinish = 3;

    // ChessRogueDiceStatus.
    public const int DiceIdle = 0;
    public const int DiceRolled = 1;
    public const int DiceConfirmed = 2;
}

[MemoryPackable]
public partial class ChessRogueRunData
{
    public int AreaId { get; set; }
    public int SubMode { get; set; }
    public int AeonId { get; set; }
    public int BranchId { get; set; }
    public List<int> DifficultyIds { get; set; } = [];
    public List<int> BaseAvatarIds { get; set; } = [];

    public List<int> LayerIds { get; set; } = [];
    public int CurLayerIndex { get; set; }
    public int BoardId { get; set; }
    public int LayerEntranceId { get; set; }
    public int StartCellId { get; set; }
    public int CurCellId { get; set; }
    public Dictionary<int, ChessRogueCellData> Cells { get; set; } = [];
    public List<int> HistoryCellIds { get; set; } = [];

    // Extra movable cells granted by a SetColCanMove modifier, consumed by the next level-info build.
    public List<int> CanMoveCellIds { get; set; } = [];
    public bool FirstEnterBattle { get; set; } = true;
    public int LevelStatus { get; set; } = ChessRogueStates.LevelProcessing;

    public int ActionPoint { get; set; }
    public int Money { get; set; }
    public int ReviveCost { get; set; }

    // Maze buff id -> level.
    public Dictionary<int, int> Buffs { get; set; } = [];

    public int BossAeonId { get; set; }
    public List<int> BossDecayIds { get; set; } = [];

    public ChessRogueDiceStateData Dice { get; set; } = new();
    public ChessRogueModifierData? Modifier { get; set; }
    public int NextModifierId { get; set; } = 1;

    // Pending blessing draft: RogueBuff keys (buffId * 100 + level). Persisted so a relog keeps the pick.
    public List<int> PendingBuffKeys { get; set; } = [];
    public int ActionSeq { get; set; }

    public ChessRogueCellData? CurCell()
    {
        return Cells.GetValueOrDefault(CurCellId);
    }

    public int CurLayerId()
    {
        return CurLayerIndex < LayerIds.Count ? LayerIds[CurLayerIndex] : 0;
    }
}

[MemoryPackable]
public partial class ChessRogueCellData
{
    public int CellId { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int BlockType { get; set; }
    public int Status { get; set; }
    public int MarkType { get; set; }
    public int RoomId { get; set; }

    // Chosen room config (0 = not rolled yet).
    public int EntranceId { get; set; }
    public int AnchorGroup { get; set; }
    public int AnchorId { get; set; }
    public List<int> LoadGroups { get; set; } = [];

    public int SelectMonsterId { get; set; }
    public List<ChessRogueCellMonsterData> Monsters { get; set; } = [];
    public List<int> SelectedDecayIds { get; set; } = [];
}

[MemoryPackable]
public partial class ChessRogueCellMonsterData
{
    public int BossDecayId { get; set; }
    public int MonsterId { get; set; }
}

[MemoryPackable]
public partial class ChessRogueDiceStateData
{
    // Slot (1..6) -> surface id, copied from the branch loadout at run start.
    public Dictionary<int, int> Surfaces { get; set; } = [];
    public int CurSurfaceId { get; set; }
    public int Status { get; set; }
    public int CheatTimes { get; set; } = 1;
    public int RerollTimes { get; set; } = 1;
}

[MemoryPackable]
public partial class ChessRogueModifierData
{
    public int ModifierId { get; set; }
    public int SurfaceId { get; set; }
    public List<int> SelectableCells { get; set; } = [];
    public int SelectedCell { get; set; }
    public bool IsConfirmed { get; set; }
}
