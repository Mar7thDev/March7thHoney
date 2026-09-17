using MemoryPack;

namespace March7thHoney.Database.RogueMagic;

public static class RogueMagicStates
{
    // Room status (proto HOIFDJPBGLB).
    public const int RoomNone = 0;
    public const int RoomInited = 1;
    public const int RoomProcessing = 2;
    public const int RoomFinish = 3;

    // Layer status (proto JGNAEPIJDNC).
    public const int LayerNone = 0;
    public const int LayerProcessing = 1;
    public const int LayerFinish = 2;

    // Level status (proto PFKHKAMBHBN).
    public const int LevelProcessing = 1;

    public const int RunDoing = 1;

    public const int ItemValueCoin = 31;
}

public enum RogueMagicPendingKind
{
    None = 0,
    Scepter = 1,
    MagicUnit = 2
}

[MemoryPackable]
public partial class RogueMagicRunData
{
    public int AreaId { get; set; }
    public int StyleType { get; set; }
    public List<int> DifficultyCompIds { get; set; } = [];
    public List<int> BaseAvatarIds { get; set; } = [];
    public string TransNo { get; set; } = "";
    public int Status { get; set; }
    public int LevelStatus { get; set; }
    public int CurLayerIndex { get; set; }
    public List<RogueMagicLayerData> Layers { get; set; } = [];
    public int Coin { get; set; }
    public int BasicRoundCnt { get; set; }
    public int ExtraRoundCnt { get; set; }
    public List<RogueMagicScepterData> Scepters { get; set; } = [];
    public List<RogueMagicUnitData> Units { get; set; } = [];
    public int NextUnitUniqueId { get; set; } = 1;
    public int ActionSeq { get; set; }

    // Front entry is the open action; later entries roll their options when they reach the front.
    public List<RogueMagicPendingActionData> PendingActions { get; set; } = [];

    public RogueMagicLayerData? CurLayer()
    {
        return CurLayerIndex <= 0 || CurLayerIndex > Layers.Count ? null : Layers[CurLayerIndex - 1];
    }

    public RogueMagicRoomData? CurRoom()
    {
        var layer = CurLayer();
        return layer?.Rooms.FirstOrDefault(room => room.RoomIndex == layer.CurRoomIndex);
    }
}

[MemoryPackable]
public partial class RogueMagicLayerData
{
    public int LayerId { get; set; }
    public int LayerIndex { get; set; }
    public int Status { get; set; }
    public int CurRoomIndex { get; set; }
    public int EntranceId { get; set; }
    public List<RogueMagicRoomData> Rooms { get; set; } = [];
}

[MemoryPackable]
public partial class RogueMagicRoomData
{
    public int RoomIndex { get; set; }
    public int RoomId { get; set; }
    public int RoomType { get; set; }
    public int Status { get; set; }
    public int EntranceId { get; set; }
    public int PlaneId { get; set; }
    public int AnchorGroup { get; set; }
    public int AnchorId { get; set; }
    public List<int> LoadGroups { get; set; } = [];
}

[MemoryPackable]
public partial class RogueMagicScepterData
{
    public int ScepterId { get; set; }
    public int Level { get; set; }
}

[MemoryPackable]
public partial class RogueMagicUnitData
{
    public int UniqueId { get; set; }
    public int UnitId { get; set; }
    public int Level { get; set; }
}

[MemoryPackable]
public partial class RogueMagicPendingActionData
{
    public int QueuePos { get; set; }
    public RogueMagicPendingKind Kind { get; set; }
    public int DraftLevel { get; set; }
    public List<int> UnitCategories { get; set; } = [];
    public List<int> OptionIds { get; set; } = [];
    public List<int> OptionLevels { get; set; } = [];
}
