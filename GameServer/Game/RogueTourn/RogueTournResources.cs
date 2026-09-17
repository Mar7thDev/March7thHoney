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
    public string TournMode { get; set; } = "";
    public List<RogueTournPlanePlan> Planes { get; set; } = [];

    public RogueTournPlanePlan? Plane(int index)
    {
        return Planes.FirstOrDefault(plane => plane.PlaneIndex == index);
    }
}

public sealed class RogueTournPlanePlan
{
    public int PlaneIndex { get; set; }
    public int PlaneID { get; set; }
    public int EntranceID { get; set; }
    public List<int> RoomTypes { get; set; } = [];
    public int AnchorGroup { get; set; }
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
        { RogueTournRoomTypeEnum.Hidden, ["SecretChest", "SecretGamble", "SecretMiracle"] }
    };

    // RoguePersonaRoomComposition keys; an unknown composition type NREs the client.
    private static readonly HashSet<int> PersonaCompositionTypes =
        [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 17, 21, 22, 23, 24];

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
            or RogueTournRoomTypeEnum.Battle)) return 0;
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
