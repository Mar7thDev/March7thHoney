using MemoryPack;

namespace March7thHoney.Database.RogueTourn;

public static class RogueTournStates
{
    // Room status (proto IIAOGEIADGA).
    public const int RoomNone = 0;
    public const int RoomInited = 1;
    public const int RoomProcessing = 2;
    public const int RoomFinish = 3;

    // Layer status (proto IDCBNMFDICO).
    public const int LayerNone = 0;
    public const int LayerProcessing = 1;
    public const int LayerFinish = 2;

    // Level status (proto NHMPGNHBBLC).
    public const int LevelNone = 0;
    public const int LevelProcessing = 1;
    public const int LevelAllRoomsDone = 2;
    public const int LevelSettling = 3;

    // Level reason (proto RogueTournLevelStatus).
    public const int ReasonNone = 0;
    public const int ReasonProcessing = 1;
    public const int ReasonFinished = 2;
    public const int ReasonSettled = 3;

    // Internal run lifecycle.
    public const int RunIdle = 0;
    public const int RunDoing = 1;
    public const int RunFinished = 2;

    public const int ItemValueCosmicFragment = 31;
}

public enum RogueTournPendingKind
{
    None = 0,
    Style = 1,
    Formula = 2
}

[MemoryPackable]
public partial class RogueTournRunData
{
    public int AreaId { get; set; }
    public int TournMode { get; set; }
    public int DifficultyId { get; set; }
    public int CurLayerIndex { get; set; }
    public List<RogueTournLayerData> Layers { get; set; } = [];
    public Dictionary<int, int> ItemValue { get; set; } = [];
    public Dictionary<int, int> Buffs { get; set; } = [];
    public List<int> Formulas { get; set; } = [];
    public int LevelStatus { get; set; }
    public int LevelReason { get; set; }
    public int Status { get; set; }
    public bool IsWin { get; set; }

    // Shared RogueCommon queue position; never reused within a run, the client only echoes it.
    public int ActionSeq { get; set; }
    public RogueTournPendingActionData? Pending { get; set; }
    public int PersonaStyle { get; set; }
    public List<int> BaseAvatarIds { get; set; } = [];
    public string TransNo { get; set; } = "";

    public RogueTournLayerData? CurLayer()
    {
        return CurLayerIndex <= 0 || CurLayerIndex > Layers.Count ? null : Layers[CurLayerIndex - 1];
    }

    public RogueTournRoomData? CurRoom()
    {
        var layer = CurLayer();
        if (layer == null) return null;
        var processing = layer.Rooms.FirstOrDefault(room => room.Status == RogueTournStates.RoomProcessing);
        if (processing != null) return processing;
        return layer.CurRoomIndex <= 0 || layer.CurRoomIndex > layer.Rooms.Count
            ? null
            : layer.Rooms[layer.CurRoomIndex - 1];
    }

    public int Coin()
    {
        return ItemValue.GetValueOrDefault(RogueTournStates.ItemValueCosmicFragment);
    }

    public void AddCoin(int count)
    {
        if (count <= 0) return;
        ItemValue[RogueTournStates.ItemValueCosmicFragment] = Coin() + count;
    }

    public int NextActionPos()
    {
        return ++ActionSeq;
    }
}

[MemoryPackable]
public partial class RogueTournLayerData
{
    public int LayerId { get; set; }
    public int LayerIndex { get; set; }
    public int Status { get; set; }
    public int CurRoomIndex { get; set; }
    public List<RogueTournRoomData> Rooms { get; set; } = [];
    public int EntranceId { get; set; }
    public int PlaneId { get; set; }
    public int AnchorGroup { get; set; }
}

[MemoryPackable]
public partial class RogueTournRoomData
{
    public int RoomId { get; set; }
    public int RoomIndex { get; set; }
    public int RoomType { get; set; }
    public int Status { get; set; }
    public int PlaneId { get; set; }
    public int EntranceId { get; set; }
    public int AnchorGroup { get; set; }

    // Client composition type; diverges from RoomType for Reforge rooms.
    public int CompositionType { get; set; }
    public int Level { get; set; }
}

[MemoryPackable]
public partial class RogueTournPendingActionData
{
    public int QueuePos { get; set; }
    public RogueTournPendingKind Kind { get; set; }
    public List<int> Options { get; set; } = [];
    public bool CanRoll { get; set; }
}
