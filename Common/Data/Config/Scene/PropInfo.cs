using MemoryPack;
using March7thHoney.Enums.Scene;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace March7thHoney.Data.Config.Scene;

[MemoryPackable]
public partial class PropInfo : PositionInfo
{
    [JsonIgnore] public bool CommonConsole = false;
    public int MappingInfoID { get; set; }
    public int AnchorGroupID { get; set; }
    public int AnchorID { get; set; }
    public int PropID { get; set; }
    public int EventID { get; set; }
    public int CocoonID { get; set; }
    public int ChestID { get; set; }
    public int FarmElementID { get; set; }
    public bool IsClientOnly { get; set; }
    public bool LoadOnInitial { get; set; } = true;

    // Raw JSON consumed at load time by Load() to populate UnlockDoorID/UnlockControllerID/etc.;
    // not needed in the binary cache (and JObject isn't MemoryPack-serializable).
    [MemoryPackIgnore] public ValueSourceInfo? ValueSource { get; set; }
    public string? InitLevelGraph { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public PropStateEnum State { get; set; } = PropStateEnum.Closed;

    [JsonIgnore] public Dictionary<int, List<int>> UnlockDoorID { get; set; } = [];

    [JsonIgnore] public Dictionary<int, List<int>> UnlockControllerID { get; set; } = [];

    [JsonIgnore] public bool IsLevelBtn { get; set; }

    /// <summary>
    ///     Server-side PropReqInteract edges of this prop's InitLevelGraph, resolved at load by
    ///     PropInteractGraphLoader. Applied by SceneInstance.ApplyPropInteractFanOut on every successful interact.
    /// </summary>
    [JsonIgnore] public List<PropInteractLink> InteractLinks { get; set; } = [];

    /// <summary>The prop replaces MazeProp.JsonPath with <see cref="InitLevelGraph" />.</summary>
    public bool IsOverrideInitLevelGraph { get; set; }

    /// <summary>The prop's own graph has a PropStateExecute branch for Open.</summary>
    [JsonIgnore] public bool GraphHasOpenBranch { get; set; }

    /// <summary>The prop's own graph has a PropStateExecute branch for Locked.</summary>
    [JsonIgnore] public bool GraphHasLockedBranch { get; set; }

    /// <summary>
    ///     The prop's own graph has a real Closed branch: the State field is absent or literally Closed.
    ///     Unknown state names are not a Closed branch.
    /// </summary>
    [JsonIgnore] public bool GraphHasRealClosedBranch { get; set; }

    /// <summary>ValueSource key "怪物死亡标记" target, captured before the raw JSON is dropped.</summary>
    [JsonIgnore] public int DeathMarkGroupId { get; set; }

    [JsonIgnore] public int DeathMarkInstId { get; set; }

    /// <summary>
    ///     Fog gate a hub console names (ValueSource key "...FogDoor..."). The gate runs CustomStateNN, so the
    ///     console must not open it; captured only so a save the old fan-out broke can be repaired.
    /// </summary>
    [JsonIgnore] public int FogDoorGroupId { get; set; }

    [JsonIgnore] public int FogDoorInstId { get; set; }

    /// <summary>
    ///     Reward chest a puzzle board names in its own ValueSource key "TargetChest" (the Hexanexus / RubikCube and
    ///     BlockProjection boards). The official reveals it in the same refresh as the board's state change (4.7:
    ///     floor 20212001 board 106/300001 press ships chest 106/300003 Hidden → ChestClosed). Zero: no target.
    /// </summary>
    [JsonIgnore] public int PuzzleChestGroupId { get; set; }

    [JsonIgnore] public int PuzzleChestInstId { get; set; }

    /// <summary>
    ///     The prop a minimap hint marker watches: ValueSource "Chest" (treasure, compass and Hexanexus markers) or
    ///     "InsertionID" (Shifting Screen markers). The marker (MazeProp 211) never changes its own state in its
    ///     client graph; the official moves it to Open in the beat its watched prop finishes. Zero: not a marker.
    /// </summary>
    [JsonIgnore] public int MapMarkerWatchGroupId { get; set; }

    [JsonIgnore] public int MapMarkerWatchInstId { get; set; }

    /// <summary>
    ///     Console a puzzle board unlocks when it is solved (ValueSource "LockedConsole"; the third Ambrosial-Arbor
    ///     coil board 45/300002 on floor 20222001 names console 10/300013). The console's graph answers a press while
    ///     Locked with a toast and no packet, so only the server can unlock it. Zero: no link.
    /// </summary>
    [JsonIgnore] public int LockedConsoleGroupId { get; set; }

    [JsonIgnore] public int LockedConsoleInstId { get; set; }

    /// <summary>
    ///     Every EndPropInteract of the prop's own graph waits for the prop to reach Open (the Chap02 lift call
    ///     consoles): the client keeps the player in the interact pose until the server moves the console.
    /// </summary>
    [JsonIgnore] public bool GraphReleasesInteractOnOpen { get; set; }

    /// <summary>
    ///     Every PropReqInteract edge of a <see cref="GraphReleasesInteractOnOpen" /> graph, guards ignored. Read by
    ///     the lift call console rule to learn which lift interacts each console sends.
    /// </summary>
    [JsonIgnore] public List<PropInteractLink> InteractOnOpenLinks { get; set; } = [];

    // A cache written before the list existed reads it back as null.
    [MemoryPackOnDeserialized]
    private void FillMissingLinks()
    {
        InteractOnOpenLinks ??= [];
    }

    public bool HasEditorOnlyLevelGraph()
    {
        return IsOverrideInitLevelGraph &&
               InitLevelGraph?.Contains("/DesignerBackup/", StringComparison.Ordinal) == true;
    }

    /// <summary>
    ///     The prop's interact button is armed only from Open. A Closed landing has no press branch.
    /// </summary>
    public bool ArmsOnlyFromOpen()
    {
        return GraphHasOpenBranch && !GraphHasRealClosedBranch;
    }

    public void Load(GroupInfo info)
    {
        if (ValueSource == null) return;

        if (Name.StartsWith("Button_") &&
            ValueSource.Values.Find(x => x["Key"]?.ToString() == "AnchorName") != null)
            IsLevelBtn = true;

        if (TryReadLink("TargetChest", out var chestGroup, out var chestInst))
        {
            PuzzleChestGroupId = chestGroup;
            PuzzleChestInstId = chestInst;
        }

        if (TryReadLink("Chest", out var watchGroup, out var watchInst) ||
            TryReadLink("InsertionID", out watchGroup, out watchInst))
        {
            MapMarkerWatchGroupId = watchGroup;
            MapMarkerWatchInstId = watchInst;
        }

        if (TryReadLink("LockedConsole", out var consoleGroup, out var consoleInst))
        {
            LockedConsoleGroupId = consoleGroup;
            LockedConsoleInstId = consoleInst;
        }

        foreach (var v in ValueSource.Values)
        {
            var key = v["Key"];
            var value = v["Value"];
            if (value == null || key == null) continue;

            if (key.ToString() == "ListenTriggerCustomString")
            {
                if (!info.PropTriggerCustomString.TryGetValue(value.ToString(), out var list))
                {
                    list = [];
                    info.PropTriggerCustomString.Add(value.ToString(), list);
                }

                list.Add(ID);
            }
            else if (key.ToString() == "怪物死亡标记")
            {
                var parts = value.ToString().Split(',');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out var markGroup) &&
                    int.TryParse(parts[1], out var markInst))
                {
                    DeathMarkGroupId = markGroup;
                    DeathMarkInstId = markInst;
                }
            }
            // RotateBridge names the rotating bridge part, a BridgeState machine, not a door.
            // Opening it writes Open, a state the part does not have, and the rotate cutscene freezes.
            else if (key.ToString().Contains("RotateBridge"))
            {
            }
            // A fog gate (CustomState01..05) and the BridgeController consoles are not doors: opening them with
            // the console left the gate red and the consoles dead (floor 20132001, main 1011301).
            else if (key.ToString().Contains("FogDoor"))
            {
                var parts = value.ToString().Split(',');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out var fogGroup) &&
                    int.TryParse(parts[1], out var fogInst))
                {
                    FogDoorGroupId = fogGroup;
                    FogDoorInstId = fogInst;
                }
            }
            else if (key.ToString().Contains("BridgeController"))
            {
            }
            else if (key.ToString().Contains("Door") ||
                     key.ToString().Contains("Bridge") ||
                     key.ToString().Contains("UnlockTarget") ||
                     key.ToString().Contains("Rootcontamination") ||
                     key.ToString().Contains("Portal"))
            {
                var parts = value.ToString().Split(',');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out var keyId) &&
                    int.TryParse(parts[1], out var valueId))
                {
                    if (!UnlockDoorID.ContainsKey(keyId))
                        UnlockDoorID.Add(keyId, []);
                    UnlockDoorID[keyId].Add(valueId);
                }
            }
            else if (key.ToString().Contains("Controller"))
            {
                var parts = value.ToString().Split(',');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out var keyId) &&
                    int.TryParse(parts[1], out var valueId))
                {
                    if (!UnlockControllerID.ContainsKey(keyId))
                        UnlockControllerID.Add(keyId, []);
                    UnlockControllerID[keyId].Add(valueId);
                }
            }
        }
    }

    /// <summary>
    ///     Reads the "group,inst" pair stored under one exact ValueSource key. A malformed or zero pair is a miss
    ///     rather than a guess.
    /// </summary>
    private bool TryReadLink(string key, out int groupId, out int instId)
    {
        groupId = 0;
        instId = 0;
        var value = ValueSource?.Values.Find(x => x["Key"]?.ToString() == key)?["Value"]?.ToString();
        var parts = value?.Split(',');
        if (parts is not { Length: 2 } ||
            !int.TryParse(parts[0].Trim(), out var parsedGroup) ||
            !int.TryParse(parts[1].Trim(), out var parsedInst) ||
            parsedGroup <= 0 || parsedInst <= 0)
            return false;

        groupId = parsedGroup;
        instId = parsedInst;
        return true;
    }
}

[MemoryPackable]
public partial class ValueSourceInfo
{
    public List<JObject> Values { get; set; } = [];
}
