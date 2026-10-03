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

    // Level reason (proto FKNGOGLNKIG). 1 rides the pushes that report a settle; 2 and 3 were never observed.
    public const int ReasonNone = 0;
    public const int ReasonOnSettle = 1;

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
    Formula = 2,
    Buff = 3,
    Gift = 4,
    Miracle = 5,
    Exit = 6
}

/// <summary>
///     The live Divergent Universe run, persisted as a MemoryPack blob in <see cref="RogueTournData.RunInstance" />.
///     Members are only ever appended: a blob written by an older build deserializes the newer members as null/0,
///     which <see cref="EnsureCollections" /> heals.
/// </summary>
[MemoryPackable]
public partial class RogueTournRunData
{
    // The official ids are opaque to the client and consecutive within a run (75, 76 at the start, then 77 ...).
    private const int ModifierIdBase = 64;

    // The two modifiers minted at the top of a run are the only ones without a source_type.
    public const int StartModifierCount = 2;

    // GGJOKFEOEBK.EKNHHGMJCPM on every armed style row of both 4.6 traces.
    public const int StyleTierCap = 4;

    public int AreaId { get; set; }
    public int TournMode { get; set; }
    public int DifficultyId { get; set; }
    public int CurLayerIndex { get; set; }
    public List<RogueTournLayerData> Layers { get; set; } = [];
    public Dictionary<int, int> ItemValue { get; set; } = [];

    // Drafted blessings: MazeBuffID → level.
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

    // ---- Appended for the 4.6 Persona flow (absent from older blobs). ----

    // Room-card ids an offered triple already spent; the two unchosen cards never return.
    public List<int> DeckConsumed { get; set; } = [];

    // Persona gifts in pick order; a gift's position + 1 is the entry id the client is told.
    public List<int> Gifts { get; set; } = [];
    public List<int> Miracles { get; set; } = [];

    // Event-room dialogues keyed by the room's run-local unique id.
    public Dictionary<int, RogueTournRoomDialogueData> RoomDialogues { get; set; } = [];
    public int DialogueSeq { get; set; }

    // Persona style progression pushed as the EGPLOEAAFGL + MIDFLEHKPBF pair on every room entry.
    public int StyleExp { get; set; }
    public int StyleLevel { get; set; }

    // Answered blessing drafts: the only measured driver of the style tier (4, 7, 10, 13 → tiers 1..4).
    public int BlessingPicks { get; set; }

    // RogueModifier ids in mint order; StyleModifier is the one row carrying the tier arm.
    public List<int> Modifiers { get; set; } = [];
    public int StyleModifier { get; set; }
    public int ModifierSeq { get; set; }

    // ---- Transient per-request state, drained by the handler that owns the result stream. ----

    [MemoryPackIgnore] public bool StyleTierUp { get; set; }
    [MemoryPackIgnore] public List<RogueTournStyleRow> StyleRows { get; set; } = [];
    [MemoryPackIgnore] public List<RogueTournModifierAdd> ModifierAdds { get; set; } = [];

    /// <summary>Fills collections an older blob did not carry and numbers its rooms when it predates unique ids.</summary>
    public void EnsureCollections()
    {
        Layers ??= [];
        ItemValue ??= [];
        Buffs ??= [];
        Formulas ??= [];
        BaseAvatarIds ??= [];
        TransNo ??= "";
        DeckConsumed ??= [];
        Gifts ??= [];
        Miracles ??= [];
        RoomDialogues ??= [];
        Modifiers ??= [];
        StyleRows ??= [];
        ModifierAdds ??= [];
        foreach (var layer in Layers) layer.Rooms ??= [];
        if (Pending != null)
        {
            Pending.Options ??= [];
            Pending.OptionLevels ??= [];
            Pending.BuffOffers ??= [];
        }

        if (Layers.SelectMany(layer => layer.Rooms).Any(room => room.UniqueId == 0))
        {
            var uid = 0;
            foreach (var room in Layers.SelectMany(layer => layer.Rooms)) room.UniqueId = ++uid;
        }
    }

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

    public (RogueTournLayerData? Layer, RogueTournRoomData? Room) RoomByUniqueId(int uniqueId)
    {
        if (uniqueId == 0) return (null, null);
        foreach (var layer in Layers)
        foreach (var room in layer.Rooms)
            if (room.UniqueId == uniqueId)
                return (layer, room);
        return (null, null);
    }

    /// <summary>
    ///     The slot the run enters next: the first un-entered room of the current layer or, once it is walked out,
    ///     the first room of the next layer. The walk is linear and never jumps ahead.
    /// </summary>
    public (int LayerIndex, RogueTournLayerData? Layer, RogueTournRoomData? Room) NextRoomSlot()
    {
        var cur = CurLayerIndex - 1;
        if (cur < 0 || cur >= Layers.Count) return (-1, null, null);
        var layer = Layers[cur];
        if (layer.CurRoomIndex < layer.Rooms.Count) return (cur, layer, layer.Rooms[layer.CurRoomIndex]);
        if (cur + 1 < Layers.Count && Layers[cur + 1].Rooms.Count > 0)
            return (cur + 1, Layers[cur + 1], Layers[cur + 1].Rooms[0]);
        return (-1, null, null);
    }

    /// <summary>
    ///     The run deck seen from the card side: every room not yet entered nor spent by an offered triple is
    ///     remaining, the rest consumed. The two lists always partition the run's rooms.
    /// </summary>
    public (List<int> Remaining, List<int> Consumed, List<RogueTournRoomData> Cards) DeckView()
    {
        List<int> remaining = [], consumed = [];
        List<RogueTournRoomData> cards = [];
        foreach (var room in Layers.SelectMany(layer => layer.Rooms))
        {
            if (room.UniqueId == 0) continue;
            if (DeckConsumed.Contains(room.UniqueId) || room.Status != RogueTournStates.RoomNone)
                consumed.Add(room.UniqueId);
            else
                remaining.Add(room.UniqueId);
            cards.Add(room);
        }

        return (remaining, consumed, cards);
    }

    public void ConsumeDeck(IEnumerable<int> uniqueIds)
    {
        foreach (var uid in uniqueIds)
            if (!DeckConsumed.Contains(uid))
                DeckConsumed.Add(uid);
    }

    /// <summary>Whether the walk stands on the last room of the last layer, i.e. clearing it ends the run.</summary>
    public bool AtFinalRoom()
    {
        var layer = CurLayer();
        if (layer == null || CurRoom() == null) return false;
        return layer.CurRoomIndex >= layer.Rooms.Count && CurLayerIndex >= Layers.Count;
    }

    /// <summary>One tier per three blessings past the first, capped (tes1/tes2: steps at picks 4, 7, 10, 13).</summary>
    public int StyleTier()
    {
        if (BlessingPicks == 0) return 0;
        return Math.Min((BlessingPicks - 1) / 3, StyleTierCap);
    }

    public int AddModifier()
    {
        if (ModifierSeq < ModifierIdBase) ModifierSeq = ModifierIdBase;
        ModifierSeq++;
        Modifiers.Add(ModifierSeq);
        return ModifierSeq;
    }

    public bool ModifierIsStart(int id)
    {
        var index = Modifiers.IndexOf(id);
        return index < 0 || index < StartModifierCount;
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

    // Appended: an area's DifficultyIDList holds one id per layer (20405 → 3051/3052/3053).
    public int DifficultyId { get; set; }
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

    // Client composition type; diverges from RoomType for Reforge (21) and Conversion (23) rooms.
    public int CompositionType { get; set; }
    public int Level { get; set; }

    // ---- Appended ----

    // Run-local 1..N number across all layers: how the 4.6 client addresses a room (registration, room cards).
    public int UniqueId { get; set; }

    // How many of the room's cleared-room drafts have been dealt (the policy is a sequence).
    public int DealtStep { get; set; }

    // The RoguePersonaLayerRoom preset this slot replays; the door into it is a preset confirm, not a card draw.
    public int PresetId { get; set; }

    // The layer difficulty's LevelList entry for this room index (86..95 on area 20405), not the card level.
    public int MonsterLevel { get; set; }
}

[MemoryPackable]
public partial class RogueTournPendingActionData
{
    public int QueuePos { get; set; }
    public RogueTournPendingKind Kind { get; set; }
    public List<int> Options { get; set; } = [];
    public bool CanRoll { get; set; }

    // ---- Appended ----

    // Exit chooser that confirms the next slot's Persona preset instead of offering cards.
    public int ExitPreset { get; set; }

    // Exit chooser card → its level (KEGHPOHGPFF).
    public Dictionary<int, int> OptionLevels { get; set; } = [];

    // RogueMiracleGroupID echoed on a miracle draft (LGIPILAKIKB).
    public int DropGroup { get; set; }

    // Blessing draft options with the level on offer.
    public List<RogueTournBuffOffer> BuffOffers { get; set; } = [];
}

[MemoryPackable]
public partial class RogueTournBuffOffer
{
    public int BuffId { get; set; }
    public int Level { get; set; }
}

[MemoryPackable]
public partial class RogueTournRoomDialogueData
{
    // RogueCommon dialogue handle (AIAFEKGBFIF) the client echoes on a pick.
    public int Handle { get; set; }
    public int NpcId { get; set; }
    public List<int> Options { get; set; } = [];
    public List<int> Selected { get; set; } = [];
    public bool Closed { get; set; }
}

/// <summary>One buffered Persona style exp grant: the new state and the gain, sent as two result rows.</summary>
public readonly record struct RogueTournStyleRow(int Exp, int Level, int PrevExp, int PrevLevel, int Gain);

/// <summary>One buffered mid-run RogueModifierAddNotify row; StyleArm marks the row carrying the tier cap.</summary>
public readonly record struct RogueTournModifierAdd(int Id, bool StyleArm);
