using MemoryPack;
using System.Collections.Concurrent;
using March7thHoney.Enums.Scene;
using March7thHoney.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Data.Config.Scene;

[MemoryPackable]
public partial class FloorInfo
{
    [JsonConverter(typeof(ConcurrentDictionaryConverter<int, PropInfo>))]
    public ConcurrentDictionary<int, PropInfo> CachedTeleports = [];

    [JsonConverter(typeof(ConcurrentDictionaryConverter<int, GroupInfo>))]
    public ConcurrentDictionary<int, GroupInfo> Groups = [];

    [JsonIgnore] public bool Loaded;

    [JsonConverter(typeof(ConcurrentBagConverter<int>))]
    public ConcurrentBag<int> MapSections = [];

    public int FloorID { get; set; }
    public int StartGroupIndex { get; set; }
    public int StartAnchorID { get; set; }
    public string NavmapConfigPath { get; set; } = "";
    public List<FloorGroupInfo> GroupInstanceList { get; set; } = [];
    public List<FloorDimensionInfo> DimensionList { get; set; } = [];

    /// <summary>
    ///     Per-instance lifecycle fields of the runtime floor export, keyed by group id. Consumed at load time
    ///     by <see cref="StampCommonMapFields" /> and not cached: the stamped values live on the group.
    /// </summary>
    [MemoryPackIgnore]
    public Dictionary<string, GroupInstanceCommonInfo> GroupInstanceCommonMap { get; set; } = [];

    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
    public List<LevelFeatureTypeEnum> LevelFeatureModules { get; set; } = [];

    [JsonIgnore] public int StartGroupID { get; set; }
    [JsonIgnore] public List<FloorSavedValueInfo> FloorSavedValue { get; set; } = [];

    /// <summary>
    ///     Overlays the floor's GroupInstanceCommonMap entry onto a freshly loaded group. The group FILE usually
    ///     omits SaveType (in the 4.5 dump 10.7k groups carry none while their common-map row says Permanent);
    ///     the runtime floor export is the authoritative per-instance source, so it wins whenever it names a
    ///     value. Persistence (EntityProp.SetState / SceneSkillManager) reads the resulting SaveType.
    /// </summary>
    public void StampCommonMapFields(GroupInfo group)
    {
        if (!GroupInstanceCommonMap.TryGetValue(group.Id.ToString(), out var common)) return;

        if (!string.IsNullOrEmpty(common.SaveType) &&
            Enum.TryParse<SaveTypeEnum>(common.SaveType, out var saveType))
            group.SaveType = saveType;

        if (!string.IsNullOrEmpty(common.Category) &&
            Enum.TryParse<GroupCategoryEnum>(common.Category, out var category))
            group.Category = category;

        if (common.IsHoyoGroup is { } isHoyoGroup)
            group.IsHoyoGroup = isHoyoGroup;
    }

    public AnchorInfo? GetAnchorInfo(int groupId, int anchorId)
    {
        Groups.TryGetValue(groupId, out var group);
        if (group == null) return null;

        return group.AnchorList.Find(info => info.ID == anchorId);
    }


    public void OnLoad()
    {
        if (Loaded) return;

        StartGroupID = GroupInstanceList[StartGroupIndex].ID;

        foreach (var dimension in DimensionList) dimension.OnLoad(this);

        // Cache anchors
        foreach (var group in Groups.Values)
        foreach (var prop in group.PropList)
            // Check if prop can be teleported to
            if (prop.AnchorID > 0)
            {
                // Teleport destinations by MappingInfoID (EnterScene(teleportId) + GetUnlockTeleport). The
                // checkpoint's STATE is no longer forced here: with the story enabled a waypoint starts in its
                // configured CheckPointDisable and is unlocked when the player reaches it
                // (SceneInstance.TryUnlockNearbyWaypoint); the story-off mode re-enables them per scene in
                // SceneEntityLoader.LoadProp. Forcing the config copy made every waypoint permanently unlocked
                // and broke the mission PropState gates that watch these checkpoints.
                CachedTeleports.TryAdd(prop.MappingInfoID, prop);
            }
            else if (!string.IsNullOrEmpty(prop.InitLevelGraph))
            {
                var json = prop.InitLevelGraph;

                // Hacky way to setup prop triggers
                if (json.Contains("Maze_GroupProp_OpenTreasure_WhenMonsterDie"))
                {
                    //prop.Trigger = new TriggerOpenTreasureWhenMonsterDie(group.Id);
                }
                else if (json.Contains("Common_Console"))
                {
                    prop.CommonConsole = true;
                }

                // Clear for garbage collection
                prop.ValueSource = null;
                prop.InitLevelGraph = null;
            }

        Loaded = true;
    }
}

/// <summary>One GroupInstanceCommonMap row of a runtime floor export (only the fields the server consumes).</summary>
public class GroupInstanceCommonInfo
{
    public bool? IsHoyoGroup { get; set; }
    public string? SaveType { get; set; }
    public string? Category { get; set; }
    public string? LoadSide { get; set; }
    public bool? LoadOnInitial { get; set; }
}

[MemoryPackable]
public partial class FloorGroupInfo
{
    public string GroupPath { get; set; } = "";
    public bool IsDelete { get; set; }
    public int ID { get; set; }
    public string Name { get; set; } = "";
}

[MemoryPackable]
public partial class FloorSavedValueInfo
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultValue { get; set; }
}

[MemoryPackable]
public partial class FloorDimensionInfo
{
    public int ID { get; set; }
    public List<DimensionSavedValues> SavedValues { get; set; } = [];
    public List<int> GroupIndexList { get; set; } = [];

    [JsonIgnore] public List<int> GroupIDList { get; set; } = [];

    public void OnLoad(FloorInfo floor)
    {
        foreach (var data in SavedValues)
        {
            if (floor.FloorSavedValue.Any(x => x.Name == data.Name)) continue;

            floor.FloorSavedValue.Add(new FloorSavedValueInfo
            {
                ID = data.ID,
                Name = data.Name,
                DefaultValue = data.DefaultValue
            });
        }

        foreach (var index in GroupIndexList) GroupIDList.Add(floor.GroupInstanceList[index].ID);
    }
}

[MemoryPackable]
public partial class DimensionSavedValues
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxValue { get; set; }
    public int DefaultValue { get; set; }
}
