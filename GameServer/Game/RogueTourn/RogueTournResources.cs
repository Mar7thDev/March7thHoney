using March7thHoney.Data;
using March7thHoney.Data.Config.Scene;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.TournRogue;

namespace March7thHoney.GameServer.Game.RogueTourn;

public sealed class RogueTournRoomPlanConfig
{
    public List<RogueTournSeasonPlan> Seasons { get; set; } = [];
}

public sealed class RogueTournSeasonPlan
{
    private static readonly RogueTournClearedRoomPlan DefaultClearedRoom = new() { Drafts = ["gift"] };

    public string TournMode { get; set; } = "";
    public List<RogueTournPlanePlan> Planes { get; set; } = [];

    // The planes a run draws its layers from, one per layer without replacement; wins over Planes[].PlaneID.
    // No table binds a layer to a plane: both official 4.6 runs of area 20405 walked different planes.
    public List<int> PlanePool { get; set; } = [];

    // What a cleared room deals before the room chooser opens; absent = one Persona gift choice.
    public RogueTournClearedRoomPlan? ClearedRoom { get; set; }

    // The settle-score rule; absent or incomplete = the run scores nothing.
    public RogueTournScorePlan? Score { get; set; }

    // RogueTournNPC ids an Event room plays, dealt in order and wrapping; absent = Event rooms announce no talk.
    public List<int> EventPool { get; set; } = [];

    public RogueTournPlanePlan? Plane(int index)
    {
        return Planes.FirstOrDefault(plane => plane.PlaneIndex == index);
    }

    public RogueTournClearedRoomPlan ClearedRoomPlan()
    {
        return ClearedRoom is { Drafts.Count: > 0 } ? ClearedRoom : DefaultClearedRoom;
    }

    public RogueTournScorePlan? ScorePlan()
    {
        return Score is { RoomTypes.Count: > 0, PerRoom: > 0, ExpPerScore: > 0 } ? Score : null;
    }
}

public sealed class RogueTournPlanePlan
{
    public int PlaneIndex { get; set; }
    public int PlaneID { get; set; }
    public int EntranceID { get; set; }
    public List<int> RoomTypes { get; set; } = [];

    // The card level each preset-less slot shows (a preset-bearing slot plays its preset's level).
    public List<int> RoomLevels { get; set; } = [];
    public int AnchorGroup { get; set; }
}

public sealed class RogueTournClearedRoomPlan
{
    // Dealt in order before the chooser: "gift", "gift_core", "miracle". Unknown names are skipped.
    public List<string> Drafts { get; set; } = [];

    // RogueMiracleGroupID echoed on a miracle offer (the pool itself is the run's TournMode).
    public int MiracleGroup { get; set; }
    public int MiracleCount { get; set; }
}

public sealed class RogueTournScorePlan
{
    public List<int> RoomTypes { get; set; } = [];
    public int PerRoom { get; set; }
    public int ExpPerScore { get; set; }
}

public readonly record struct RogueTournRoomSlot(int RoomType, int AreaGroup, int BaseGroup);

public readonly record struct RogueTournMonsterSlot(
    int ConfigId,
    int NpcMonsterId,
    int EventId,
    int RogueMonsterId,
    int HardLevelGroup,
    int Level);

public sealed class RogueTournRoomBlock
{
    public int AreaGroup { get; init; }
    public Dictionary<string, int> Contents { get; } = [];
}

/// <summary>
///     Divergent Universe data derivations. A tourn plane is ONE floor holding several complete room
///     layouts ("blocks"); the floor's group order (Area → Base → Content) is the room→scene binding.
/// </summary>
public static class RogueTournResources
{
    // Both 4.5 official run scenes draw their three room mobs from this 40-entry weighted pool.
    public const int RoomMonsterPool = 302001;

    // The level export authors five placeholder slots; the official room ships the first three.
    public const int RoomMonsterCount = 3;

    // Room-mark/portal placeholder; the client draws the portal from its own prefab.
    private const int RoomMarkPropId = 1063;

    // The official room ships two of its Destruct content's six props.
    private const int DestructPropLimit = 2;

    private const string AreaMarker = "RogueModule_Tournament_Area";
    private const string BaseMarker = "RogueModule_Tournament_Base";
    private const string ContentMarker = "RogueModule_Tournament_Content";
    private const string ContentPrefix = "RogueModule_Tournament_Content_";
    private const string RoomNameMarker = "RogueModule_Tournament";
    private const string TemplateMarker = "Tpl_";

    // Shipped room-template planes, one per run layer.
    private static readonly int[] GuidePlanes = [80601, 80602, 80603, 80604];

    private static readonly string[] GenericContentKinds = ["Chest", "Destruct", "Gamble"];

    private static readonly Dictionary<RogueTournRoomTypeEnum, string[]> RoomContentKinds = new()
    {
        { RogueTournRoomTypeEnum.Boss, ["Monster"] },
        { RogueTournRoomTypeEnum.Elite, ["Monster"] },
        { RogueTournRoomTypeEnum.Battle, ["Monster"] },
        { RogueTournRoomTypeEnum.Encounter, ["NPC"] },
        { RogueTournRoomTypeEnum.Event, ["NPC"] },
        { RogueTournRoomTypeEnum.Coin, ["CoinChest", "CoinGamble", "CoinGambleExtra"] },
        { RogueTournRoomTypeEnum.Shop, ["Shop"] },
        { RogueTournRoomTypeEnum.Reward, ["Chest"] },
        { RogueTournRoomTypeEnum.Adventure, ["SecretChest"] },
        { RogueTournRoomTypeEnum.Respite, ["Revive"] },
        { RogueTournRoomTypeEnum.Reforge, ["Reforge_1", "Reforge_2"] },
        { RogueTournRoomTypeEnum.Hidden, ["SecretChest", "SecretGamble", "SecretMiracle"] },
        // Conversion fights to a counted win and owns the block's Convert content; Puman is a combat variant.
        { RogueTournRoomTypeEnum.Conversion, ["Monster", "Convert"] },
        { RogueTournRoomTypeEnum.Puman, ["Monster"] }
    };

    // RoguePersonaRoomComposition keys; an unknown composition type NREs the client.
    private static readonly HashSet<int> PersonaCompositionTypes =
        [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 17, 21, 22, 23, 24];

    // RoguePersonaRoomCompType's own room-type → composition mapping, the fallback for a cache without the table.
    private static readonly Dictionary<RogueTournRoomTypeEnum, int> FallbackCompositions = new()
    {
        { RogueTournRoomTypeEnum.Boss, 1 }, { RogueTournRoomTypeEnum.Elite, 2 },
        { RogueTournRoomTypeEnum.Battle, 3 }, { RogueTournRoomTypeEnum.Encounter, 4 },
        { RogueTournRoomTypeEnum.Event, 5 }, { RogueTournRoomTypeEnum.Coin, 6 },
        { RogueTournRoomTypeEnum.Shop, 7 }, { RogueTournRoomTypeEnum.Reward, 8 },
        { RogueTournRoomTypeEnum.Adventure, 9 }, { RogueTournRoomTypeEnum.Respite, 10 },
        { RogueTournRoomTypeEnum.Reforge, 21 }, { RogueTournRoomTypeEnum.Hidden, 12 },
        { RogueTournRoomTypeEnum.Blank, 22 }, { RogueTournRoomTypeEnum.Conversion, 23 },
        { RogueTournRoomTypeEnum.Puman, 24 }
    };

    private static readonly Lazy<RogueTournRoomPlanConfig?> RoomPlan =
        new(() => ResourceManager.LoadCustomFile<RogueTournRoomPlanConfig>("RogueTourn Room Plan",
            "RogueTournRoomPlan"));

    #region Tables

    public static RogueTournSeasonPlan? SeasonPlan(RogueTournModeEnum mode)
    {
        return RoomPlan.Value?.Seasons.FirstOrDefault(season => season.TournMode == mode.ToString());
    }

    public static int RoomsOfLayerCount(int layerId)
    {
        return GameData.RogueTournLayerRoomData.TryGetValue(layerId, out var rows) ? rows.Count : 0;
    }

    public static List<int> FormulaIds(RogueTournModeEnum mode, RogueFormulaCategoryEnum category)
    {
        return GameData.RogueTournFormulaData.Values
            .Where(row => (mode == RogueTournModeEnum.Permanent || row.TournMode == mode) &&
                          row.FormulaCategory == category)
            .Select(row => row.FormulaID)
            .OrderBy(id => id)
            .ToList();
    }

    // Server policy: the dump has no default/unlock column, so the lowest style id is used.
    public static int PersonaStyleDefault()
    {
        return GameData.RoguePersonaStyleData.Keys.Where(id => id != 0).DefaultIfEmpty(0).Min();
    }

    // The current generation's areas plus the generation-less Guide area (101).
    public static List<int> AreasForSeason(RogueTournModeEnum mode)
    {
        return GameData.RogueTournAreaData.Values
            .Where(area => area.TournMode == mode ||
                           (area.TournMode == RogueTournModeEnum.Permanent &&
                            area.AreaGroupID == RogueTournAreaGroupIDEnum.Guide))
            .Select(area => area.AreaID)
            .OrderBy(id => id)
            .ToList();
    }

    public static List<int> PermanentTalentsSorted()
    {
        return GameData.RogueTournPermanentTalentData.Keys.OrderBy(id => id).ToList();
    }

    // RoguePersonaTalent, not RogueTournTitanTalent: the extra Titan ids make the client drop the tree.
    public static List<RoguePersonaTalentExcel> PersonaTalentsSorted()
    {
        return GameData.RoguePersonaTalentData.Values.OrderBy(row => row.ID).ToList();
    }

    // RogueTournKeyword has no season column; families are the id/1000 prefixes in ascending
    // generation order. An unknown mode falls back to the newest family.
    public static List<int> KeywordsForSeason(RogueTournModeEnum mode)
    {
        var families = GameData.RogueTournKeywordData.Keys
            .GroupBy(id => id / 1000)
            .OrderBy(group => group.Key)
            .ToList();
        if (families.Count == 0) return [];

        var generation = (int)mode;
        var index = generation >= 1 && generation <= families.Count ? generation - 1 : families.Count - 1;
        return families[index].OrderBy(id => id).ToList();
    }

    public static int PersonaCompositionType(int compositionType)
    {
        return PersonaCompositionTypes.Contains(compositionType)
            ? compositionType
            : (int)RogueTournRoomTypeEnum.Battle;
    }

    /// <summary>
    ///     The composition a room of this type ships: the lowest RoguePersonaRoomCompType id whose label is the type
    ///     (Hidden owns 12/13/14/15/17), else the restated table, else Battle.
    /// </summary>
    public static int CompositionForRoomType(int roomType)
    {
        var lowest = 0;
        foreach (var (id, row) in GameData.RoguePersonaRoomCompTypeData)
            if (Enum.TryParse<RogueTournRoomTypeEnum>(row.RoomType, out var label) && (int)label == roomType &&
                (lowest == 0 || id < lowest))
                lowest = id;
        if (lowest != 0) return lowest;
        return FallbackCompositions.GetValueOrDefault((RogueTournRoomTypeEnum)roomType,
            (int)RogueTournRoomTypeEnum.Battle);
    }

    // One Persona layer's authored room slots in index order; empty for the older generations.
    public static List<RoguePersonaLayerRoomExcel> PersonaRooms(int layerId)
    {
        return GameData.RoguePersonaLayerRoomData.GetValueOrDefault(layerId) ?? [];
    }

    /// <summary>The card a Persona preset places: its composition, level and the room type that composition renders as.</summary>
    public static (int Composition, int Level, int RoomType)? PersonaRoomCard(int presetId)
    {
        if (presetId == 0 || !GameData.RoguePersonaRoomPresetData.TryGetValue(presetId, out var preset)) return null;
        if (!GameData.RoguePersonaRoomCompTypeData.TryGetValue(preset.CompositionType, out var comp)) return null;
        if (!Enum.TryParse<RogueTournRoomTypeEnum>(comp.RoomType, out var roomType) ||
            roomType == RogueTournRoomTypeEnum.Unknown) return null;
        return (preset.CompositionType, preset.Level, (int)roomType);
    }

    /// <summary>The monster level a room of a layer stamps: the difficulty's LevelList indexed by room, clamped.</summary>
    public static int DifficultyLevelAt(int difficultyId, int roomIndex)
    {
        if (!GameData.RogueTournDifficultyData.TryGetValue(difficultyId, out var difficulty) ||
            difficulty.LevelList.Count == 0) return 0;
        return difficulty.LevelList[Math.Clamp(roomIndex - 1, 0, difficulty.LevelList.Count - 1)];
    }

    #endregion

    #region Draft Pools

    /// <summary>
    ///     The blessing pool of one generation: every RogueBuffTag the generation's RogueTournBuffGroup rows drop,
    ///     resolved to its lowest-level buff row, each MazeBuffID once (a buff keys one tag per level, and the
    ///     official never repeats an id inside one draft). Sorted, so only the window rotation varies.
    /// </summary>
    public static List<RogueTournBuffExcel> BuffPool(RogueTournModeEnum mode)
    {
        var tags = GameData.RogueBuffGroupData.Values.OfType<RogueTournBuffGroupExcel>()
            .Where(group => group.TournMode == mode)
            .SelectMany(group => group.RogueBuffDrop)
            .ToHashSet();
        if (tags.Count == 0) return [];

        Dictionary<int, RogueTournBuffExcel> byTag = [];
        foreach (var buff in GameData.RogueBuffData.Values.OfType<RogueTournBuffExcel>())
            if (tags.Contains(buff.RogueBuffTag) &&
                (!byTag.TryGetValue(buff.RogueBuffTag, out var best) || buff.MazeBuffLevel < best.MazeBuffLevel))
                byTag[buff.RogueBuffTag] = buff;

        HashSet<int> seen = [];
        List<RogueTournBuffExcel> pool = [];
        foreach (var tag in tags.OrderBy(tag => tag))
            if (byTag.TryGetValue(tag, out var buff) && buff.MazeBuffID != 0 && seen.Add(buff.MazeBuffID))
                pool.Add(buff);
        return pool;
    }

    public static RogueTournBuffExcel? BuffRow(int buffId, int level)
    {
        return GameData.RogueBuffData.GetValueOrDefault(buffId * 100 + level) as RogueTournBuffExcel;
    }

    /// <summary>
    ///     A Persona gift window: ids the run does not hold, in the requested tier band (Core or not), belonging to
    ///     the run's style (a row naming no style fits every window), and sharing at least one style across the
    ///     whole window — every official window does, and a mixed one is what blanks the gift card panel.
    /// </summary>
    public static List<int> GiftChoices(IReadOnlyCollection<int> held, int styleId, bool core, int count)
    {
        if (count <= 0) return [];
        var pool = GameData.RoguePersonaStyleGiftData.Values
            .Where(row => !held.Contains(row.GiftID) && row.IsCore == core)
            .Where(row => styleId == 0 || row.StyleIDList.Count == 0 || row.StyleIDList.Contains(styleId))
            .Select(row => row.GiftID)
            .OrderBy(id => id)
            .ToList();
        if (pool.Count == 0) return [];

        List<int> window = [];
        HashSet<int>? common = null;
        void Add(int id)
        {
            window.Add(id);
            var styles = GiftStyles(id);
            if (common == null) common = styles;
            else if (styles != null) common.IntersectWith(styles);
        }

        Add(pool[Random.Shared.Next(pool.Count)]);
        while (window.Count < count)
        {
            var next = pool.Where(id => !window.Contains(id)).Where(id =>
            {
                var styles = GiftStyles(id);
                return common == null || styles == null || styles.Overlaps(common);
            }).ToList();
            if (next.Count == 0) break;
            Add(next[Random.Shared.Next(next.Count)]);
        }

        return window;
    }

    private static HashSet<int>? GiftStyles(int giftId)
    {
        var row = GameData.RoguePersonaStyleGiftData.GetValueOrDefault(giftId);
        return row == null || row.StyleIDList.Count == 0 ? null : [.. row.StyleIDList];
    }

    /// <summary>
    ///     A miracle draft: the generation's RogueTournMiracle rows the run does not hold, curses excluded, drawn
    ///     uniformly without replacement. RogueMiracleGroup keys only classic Simulated Universe miracles.
    /// </summary>
    public static List<int> MiracleChoices(RogueTournModeEnum mode, IReadOnlyCollection<int> held, int count)
    {
        var pool = GameData.RogueTournMiracleData.Values
            .Where(row => row.TournMode == mode && !held.Contains(row.MiracleID) &&
                          row.MiracleCategory != RogueTournMiracleCategoryEnum.Negative)
            .Select(row => row.MiracleID)
            .OrderBy(id => id)
            .ToList();
        List<int> picks = [];
        while (picks.Count < count && pool.Count > 0)
        {
            var index = Random.Shared.Next(pool.Count);
            picks.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return picks;
    }

    #endregion

    #region Planes

    public static int GuidePlane(int layerIndex)
    {
        return layerIndex <= 0 || layerIndex > GuidePlanes.Length ? 0 : GuidePlanes[layerIndex - 1];
    }

    public static int PlaneFloorId(int planeId)
    {
        return GameData.MazePlaneData.TryGetValue(planeId, out var plane) ? plane.StartFloorID : 0;
    }

    public static FloorInfo? PlaneFloor(int planeId)
    {
        var floorId = PlaneFloorId(planeId);
        if (floorId == 0) return null;
        GameData.GetFloorInfo(planeId, floorId, out var floor);
        return floor;
    }

    public static int PlaneEntranceId(int planeId)
    {
        var floorId = PlaneFloorId(planeId);
        if (floorId == 0) return 0;
        return GameData.MapEntranceData
            .Where(entry => entry.Value.FloorID == floorId)
            .Select(entry => entry.Key)
            .OrderBy(id => id)
            .FirstOrDefault();
    }

    public static int PlaneAnchorGroup(int planeId)
    {
        var floor = PlaneFloor(planeId);
        var area = floor?.GroupInstanceList.FirstOrDefault(group => group.Name == AreaMarker);
        if (area != null) return area.ID;

        var entranceId = PlaneEntranceId(planeId);
        return entranceId != 0 && GameData.MapEntranceData.TryGetValue(entranceId, out var entrance)
            ? entrance.StartGroupID
            : 0;
    }

    public static List<RogueTournRoomSlot> RoomSlots(int planeId)
    {
        var floor = PlaneFloor(planeId);
        if (floor == null) return [];

        List<RogueTournRoomSlot> slots = [];
        HashSet<int> seen = [];
        List<int> types = [];
        int areaGroup = 0, anchor = 0;
        var isCommon = false;

        void AddType(int roomType)
        {
            if (roomType == 0 || !seen.Add(roomType)) return;
            types.Add(roomType);
        }

        void Flush()
        {
            foreach (var roomType in types) slots.Add(new RogueTournRoomSlot(roomType, areaGroup, anchor));
        }

        foreach (var group in floor.GroupInstanceList)
        {
            if (group.IsDelete || group.Name.Contains(TemplateMarker)) continue;

            if (group.Name.Contains(AreaMarker))
            {
                Flush();
                areaGroup = group.ID;
                anchor = 0;
                isCommon = false;
                types = [];
            }
            else if (group.Name.Contains(BaseMarker))
            {
                if (anchor == 0)
                {
                    anchor = group.ID;
                    isCommon = group.Name.Contains("_Common");
                }

                if (!group.Name.Contains("_Common")) AddType(BaseRoomType(group.Name));
            }
            else if (areaGroup != 0 && anchor != 0 && isCommon && group.Name.Contains(ContentMarker))
            {
                AddType(ContentRoomType(group.Name));
            }
        }

        Flush();
        return slots;
    }

    public static List<int> RoomTypes(int planeId)
    {
        return RoomSlots(planeId).Select(slot => slot.RoomType).ToList();
    }

    public static int RoomAnchorGroup(int planeId, int roomType, int fallback)
    {
        foreach (var slot in RoomSlots(planeId))
            if (slot.RoomType == roomType)
                return slot.BaseGroup;
        return fallback;
    }

    private static int BaseRoomType(string name)
    {
        if (name.Contains("_Boss")) return (int)RogueTournRoomTypeEnum.Boss;
        if (name.Contains("_Elite")) return (int)RogueTournRoomTypeEnum.Elite;
        if (name.Contains("_Shop")) return (int)RogueTournRoomTypeEnum.Shop;
        if (name.Contains("_Rest")) return (int)RogueTournRoomTypeEnum.Respite;
        if (name.Contains("_Adventure")) return (int)RogueTournRoomTypeEnum.Adventure;
        if (name.Contains("_Secret")) return (int)RogueTournRoomTypeEnum.Hidden;
        return 0;
    }

    private static int ContentRoomType(string name)
    {
        if (name.Contains("Monster")) return (int)RogueTournRoomTypeEnum.Battle;
        if (name.Contains("Event")) return (int)RogueTournRoomTypeEnum.Event;
        if (name.Contains("Coin")) return (int)RogueTournRoomTypeEnum.Coin;
        return 0;
    }

    #endregion

    #region Room Blocks

    public static List<RogueTournRoomBlock> RoomBlocks(int planeId)
    {
        var floor = PlaneFloor(planeId);
        if (floor == null) return [];

        List<RogueTournRoomBlock> blocks = [];
        RogueTournRoomBlock? current = null;
        foreach (var group in floor.GroupInstanceList)
        {
            if (group.IsDelete) continue;

            if (group.Name.Contains(AreaMarker))
            {
                current = new RogueTournRoomBlock { AreaGroup = group.ID };
                blocks.Add(current);
            }
            else if (current != null && group.Name.Contains(ContentPrefix))
            {
                var kind = group.Name[(group.Name.LastIndexOf(ContentPrefix, StringComparison.Ordinal) +
                                       ContentPrefix.Length)..];
                current.Contents.TryAdd(kind, group.ID);
            }
        }

        return blocks;
    }

    // Blocks differ only in geometry, so rooms are assigned round-robin.
    public static RogueTournRoomBlock? RoomBlockFor(int planeId, int roomIndex)
    {
        var blocks = RoomBlocks(planeId);
        if (blocks.Count == 0) return null;
        if (roomIndex <= 0) roomIndex = 1;
        return blocks[(roomIndex - 1) % blocks.Count];
    }

    public static int RoomBlockArea(int planeId, int roomIndex)
    {
        return RoomBlockFor(planeId, roomIndex)?.AreaGroup ?? 0;
    }

    /// <summary>
    ///     The groups of the room the player stands in: the block's area group, the content groups its
    ///     room type needs plus the generic trio, and the floor's ambient Server/LoadOnInitial groups
    ///     outside every room block. The official scene ships only these.
    /// </summary>
    public static HashSet<int> RoomBlockGroups(int planeId, RogueTournRoomTypeEnum roomType, int roomIndex)
    {
        var floor = PlaneFloor(planeId);
        var block = RoomBlockFor(planeId, roomIndex);
        if (floor == null || block == null) return [];

        HashSet<int> groups = [block.AreaGroup];
        var kinds = RoomContentKinds.GetValueOrDefault(roomType, []).Concat(GenericContentKinds);
        foreach (var kind in kinds)
            if (block.Contents.TryGetValue(kind, out var groupId))
                groups.Add(groupId);

        foreach (var group in floor.GroupInstanceList)
        {
            if (group.IsDelete || group.Name.Contains(RoomNameMarker)) continue;
            if (floor.GroupInstanceCommonMap.TryGetValue(group.ID.ToString(), out var life) &&
                life.LoadSide == "Server" && life.LoadOnInitial == true)
                groups.Add(group.ID);
        }

        return groups;
    }

    /// <summary>
    ///     Per group of the room's block, which prop instances may spawn at build (an empty set keeps the
    ///     group but spawns nothing). Chest/Gamble content arrives later in the run.
    /// </summary>
    public static Dictionary<int, HashSet<int>> RoomPropWhitelist(int planeId, int roomIndex)
    {
        var floor = PlaneFloor(planeId);
        var block = RoomBlockFor(planeId, roomIndex);
        if (floor == null || block == null) return [];

        Dictionary<int, HashSet<int>> whitelist = [];
        HashSet<int> area = [];
        if (floor.Groups.TryGetValue(block.AreaGroup, out var areaGroup))
            foreach (var prop in areaGroup.PropList)
                if (prop.PropID != RoomMarkPropId)
                    area.Add(prop.ID);
        whitelist[block.AreaGroup] = area;

        foreach (var (kind, groupId) in block.Contents)
            switch (kind)
            {
                case "Chest":
                case "Gamble":
                    whitelist[groupId] = [];
                    break;
                case "Destruct":
                    HashSet<int> allow = [];
                    if (floor.Groups.TryGetValue(groupId, out var group))
                        foreach (var prop in group.PropList.Take(DestructPropLimit))
                            allow.Add(prop.ID);
                    whitelist[groupId] = allow;
                    break;
            }

        return whitelist;
    }

    public static int RoomMonsterGroup(int planeId, RogueTournRoomTypeEnum roomType, int roomIndex)
    {
        if (roomType is not (RogueTournRoomTypeEnum.Boss or RogueTournRoomTypeEnum.Elite
            or RogueTournRoomTypeEnum.Battle or RogueTournRoomTypeEnum.Conversion
            or RogueTournRoomTypeEnum.Puman)) return 0;
        return RoomBlockFor(planeId, roomIndex)?.Contents.GetValueOrDefault("Monster") ?? 0;
    }

    /// <summary>
    ///     The block's Monster content group carries placeholder slots; the room rewrites the first
    ///     <see cref="RoomMonsterCount" /> into weighted draws from <see cref="RoomMonsterPool" />.
    /// </summary>
    public static Dictionary<int, List<RogueTournMonsterSlot>> RoomStageMonsters(int planeId,
        RogueTournRoomTypeEnum roomType, int roomIndex, int level)
    {
        var groupId = RoomMonsterGroup(planeId, roomType, roomIndex);
        if (groupId == 0) return [];
        var floor = PlaneFloor(planeId);
        if (floor == null || !floor.Groups.TryGetValue(groupId, out var group) || group.MonsterList.Count == 0)
            return [];
        if (!GameData.RogueMonsterGroupData.TryGetValue(RoomMonsterPool, out var pool) ||
            pool.RogueMonsterListAndWeight.Count == 0)
            return [];

        List<RogueTournMonsterSlot> slots = [];
        for (var i = 0; i < group.MonsterList.Count && i < RoomMonsterCount; i++)
        {
            if (!GameData.RogueMonsterData.TryGetValue(PickWeighted(pool.RogueMonsterListAndWeight), out var monster))
                continue;
            // The official room stamps hard_level_group 1 on every room mob.
            slots.Add(new RogueTournMonsterSlot(group.MonsterList[i].ID, monster.NpcMonsterID, monster.EventID,
                monster.RogueMonsterID, 1, level));
        }

        return new Dictionary<int, List<RogueTournMonsterSlot>> { { groupId, slots } };
    }

    private static int PickWeighted(Dictionary<string, double> weights)
    {
        var total = weights.Values.Where(weight => weight > 0).Sum();
        if (total <= 0) return 0;

        var roll = Random.Shared.NextDouble() * total;
        foreach (var (id, weight) in weights)
        {
            if (weight <= 0) continue;
            roll -= weight;
            if (roll <= 0) return int.TryParse(id, out var value) ? value : 0;
        }

        return 0;
    }

    #endregion
}
