using March7thHoney.Data;
using March7thHoney.Database.Scene;
using March7thHoney.Enums.Mission;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Drop;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.GameServer.Server.Packet.Send.MarkChest;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Player;

public partial class PlayerInstance
{
    private static readonly Logger SceneLogger = new("Scene");

    /// <summary>Default spawn entrance: the fallback for unloadable scenes and Raid planes without a raid run.</summary>
    public const int DefaultSpawnEntryId = 2000101;

    #region Scene Actions

    public async ValueTask OnMove()
    {
        if (SceneInstance != null)
        {
            var prop = SceneInstance.GetNearestSpring(25_000_000);

            var isInRange = prop != null;

            if (isInRange)
                if (LineupManager?.GetCurLineup()?.Heal(10000, true) == true)
                    await SendPacket(new PacketSyncLineupNotify(LineupManager.GetCurLineup()!));

            // Waypoint unlock is independent of the spring heal: Space Anchors / Calyx checkpoints far from any
            // spring must still flip CheckPointEnable + UnlockTeleportNotify when the player walks up to them.
            await SceneInstance.TryUnlockNearbyWaypoint(Data.Pos);
        }
    }

    /// <summary>Remembers a floor the player has entered (GetEnteredSceneScRsp with the story enabled).</summary>
    public void RecordEnteredScene(int planeId, int floorId)
    {
        if (SceneData == null || planeId == 0 || floorId == 0) return;
        if (!SceneData.EnteredSceneData.TryGetValue(planeId, out var floors))
        {
            floors = [];
            SceneData.EnteredSceneData[planeId] = floors;
        }

        if (!floors.Contains(floorId)) floors.Add(floorId);
    }

    /// <summary>Whether the map may offer this waypoint. Story off: every waypoint is unlocked (legacy behaviour).</summary>
    public bool IsTeleportUnlocked(int floorId, int mappingInfoId)
    {
        if (!MissionEnabled) return true;
        return SceneData != null && SceneData.UnlockedTeleportData.TryGetValue(floorId, out var list) &&
               list.Contains(mappingInfoId);
    }

    /// <summary>
    ///     Records a reached waypoint and, when <paramref name="notify" /> is set, sends UnlockTeleportNotify the
    ///     first time. Returns true when the unlock was new.
    /// </summary>
    public async ValueTask<bool> RecordTeleportUnlock(int floorId, int mappingInfoId, bool notify)
    {
        if (SceneData == null || mappingInfoId == 0) return false;
        if (!SceneData.UnlockedTeleportData.TryGetValue(floorId, out var list))
        {
            list = [];
            SceneData.UnlockedTeleportData[floorId] = list;
        }

        if (list.Contains(mappingInfoId)) return false;
        list.Add(mappingInfoId);

        if (notify)
            await SendPacket(new PacketUnlockTeleportNotify(mappingInfoId,
                SceneInstance?.FloorId == floorId ? SceneInstance.EntryId : Data.EntryId));
        return true;
    }

    public async ValueTask<EntityProp?> InteractProp(int propEntityId, int interactId)
    {
        if (SceneInstance == null) return null;
        SceneInstance.Entities.TryGetValue(propEntityId, out var entity);
        if (entity is not EntityProp prop) return null;
        GameData.InteractConfigData.TryGetValue(interactId, out var config);
        if (config == null || config.SrcState != prop.State) return prop;
        var oldState = prop.State;
        await prop.SetState(config.TargetState);
        var newState = prop.State;

        switch (prop.Excel.PropType)
        {
            case PropTypeEnum.PROP_TREASURE_CHEST:
                if (oldState == PropStateEnum.ChestClosed && newState == PropStateEnum.ChestUsed)
                {
                    // TODO: Filter treasure chest
                    var items = DropService.CalculateDropsFromProp(prop.PropInfo.ChestID);
                    await InventoryManager!.AddItems(items);
                    await SendPacket(new PacketOpenChestScNotify(prop.PropInfo.ChestID));

                    var notifyMark = false;
                    foreach (var markedChest in SceneData!.MarkedChestData.Values)
                    {
                        var chest = markedChest.Find(x =>
                            x.FloorId == SceneInstance.FloorId && x.GroupId == prop.GroupId &&
                            x.ConfigId == prop.PropInfo.ID);

                        if (chest == null) continue;
                        markedChest.Remove(chest);
                        notifyMark = true;
                    }

                    if (notifyMark)
                        await SendPacket(new PacketMarkChestChangedScNotify(this));
                }

                break;
            case PropTypeEnum.PROP_DESTRUCT:
                if (newState == PropStateEnum.Closed) await prop.SetState(PropStateEnum.Open);
                break;
            case PropTypeEnum.PROP_MAZE_JIGSAW:
                switch (newState)
                {
                    case PropStateEnum.Closed:
                    {
                        foreach (var p in SceneInstance.GetEntitiesInGroup<EntityProp>(prop.GroupId))
                            if (p.Excel.PropType == PropTypeEnum.PROP_TREASURE_CHEST)
                            {
                                await p.SetState(PropStateEnum.ChestClosed);
                            }
                            else if (p.Excel.PropType == prop.Excel.PropType)
                            {
                                // Skip
                            }
                            else
                            {
                                await p.SetState(PropStateEnum.Open);
                            }

                        break;
                    }
                    case PropStateEnum.Open:
                    {
                        foreach (var p in SceneInstance.GetEntitiesInGroup<EntityProp>(prop.GroupId).Where(p =>
                                     p.Excel.PropType is not PropTypeEnum.PROP_TREASURE_CHEST &&
                                     p.Excel.PropType != prop.Excel.PropType))
                            await p.SetState(PropStateEnum.Open);

                        break;
                    }
                }

                break;
            case PropTypeEnum.PROP_MAZE_PUZZLE:
                if (newState is PropStateEnum.Closed or PropStateEnum.Open)
                    foreach (var p in SceneInstance.GetEntitiesInGroup<EntityProp>(prop.GroupId))
                    {
                        if (p.Excel.PropType == PropTypeEnum.PROP_TREASURE_CHEST)
                        {
                            await p.SetState(PropStateEnum.ChestClosed);
                        }
                        else if (p.Excel.PropType == prop.Excel.PropType)
                        {
                            // Skip
                        }
                        else
                        {
                            await p.SetState(PropStateEnum.Open);
                        }

                        await MissionManager!.OnPlayerInteractWithProp();
                    }

                break;
            case PropTypeEnum.PROP_ORDINARY:
                if (prop.PropInfo.CommonConsole)
                    // set group
                    foreach (var p in SceneInstance.GetEntitiesInGroup<EntityProp>(prop.GroupId))
                    {
                        await p.SetState(newState);

                        await MissionManager!.OnPlayerInteractWithProp();
                    }

                if (prop.Excel.ID == 104039)
                {
                    foreach (var p in SceneInstance.GetEntitiesInGroup<EntityProp>(prop.GroupId))
                        await p.SetState(newState);

                    await MissionManager!.OnPlayerInteractWithProp();
                }

                if (prop.PropInfo.Name.Contains("Piece"))
                {
                    var pieceDone = SceneInstance.GetEntitiesInGroup<EntityProp>(prop.GroupId)
                        .Where(p => p.PropInfo.Name.Contains("Piece")).All(p => p.State == PropStateEnum.Closed);

                    if (pieceDone)
                        // set JigsawSir to open
                        foreach (var p in SceneInstance.GetEntitiesInGroup<EntityProp>(prop.GroupId)
                                     .Where(p => p.PropInfo.Name.Contains("JigsawSir") &&
                                                 p.State != PropStateEnum.Closed))
                            await p.SetState(PropStateEnum.TriggerEnable);
                }

                break;
        }

        // Prop-graph fan-out (RPG.GameCore.PropReqInteract): the interacted prop's InitLevelGraph may request
        // sibling interacts server-side - the robot treasure-hunt scan (floor 20111001 g47) reveals its chest
        // this way. The target's own graph is re-driven recursively so the whole chain lands at once.
        await SceneInstance.ApplyPropInteractFanOut(prop);

        // for door unlock
        if (prop.PropInfo.UnlockDoorID.Count > 0)
            foreach (var p in prop.PropInfo.UnlockDoorID.SelectMany(id =>
                         SceneInstance.GetEntitiesInGroup<EntityProp>(id.Key)
                             .Where(p => id.Value.Contains(p.PropInfo.ID))))
            {
                await p.SetState(PropStateEnum.Open);
                await MissionManager!.OnPlayerInteractWithProp();
            }

        // Pad-puzzle groups reveal the hidden chest their ValueSource names on a real interaction, before
        // the PropState gate that waits on that chest is checked.
        if (SceneInstance.FloorInfo?.Groups.TryGetValue(prop.GroupId, out var interactedGroup) == true)
            await SceneInstance.RevealAuthoredChest(interactedGroup);

        // for mission
        await MissionManager!.OnPlayerInteractWithProp();

        // plane event
        await InventoryManager!.HandlePlaneEvent(prop.PropInfo.EventID);

        // handle plugin event

        var floorSavedKey = prop.PropInfo.Name.Replace("Controller_", "");
        var key = $"FSV_ML{floorSavedKey}{(config.TargetState == PropStateEnum.Open ? "Started" : "Complete")}";

        if (prop.Group.GroupName.Contains("JigsawPuzzle") && prop.Group.GroupName.Contains("MainLine"))
        {
            var splits = prop.Group.GroupName.Split('_');
            key =
                $"JG_ML_{splits[3]}_Puzzle{(config.TargetState == PropStateEnum.Open ? "Started" : "Complete")}";
        }

        if (SceneInstance?.FloorInfo?.FloorSavedValue.Find(x => x.Name == key) != null)
        {
            // should save
            var plane = SceneInstance.PlaneId;
            var floor = SceneInstance.FloorId;
            SceneData!.FloorSavedData.TryGetValue(floor, out var value);
            if (value == null)
            {
                value = [];
                SceneData.FloorSavedData[floor] = value;
            }

            value[key] = 1; // ParamString[2] is the key
            await SendPacket(new PacketUpdateFloorSavedValueNotify(key, 1, this));

            TaskManager?.SceneTaskTrigger.TriggerFloor(plane, floor, SceneInstance);
            MissionManager?.HandleFinishType(MissionFinishTypeEnum.FloorSavedValue);
        }

        if (prop.PropInfo.IsLevelBtn) await prop.SetState(PropStateEnum.Closed);

        return prop;
    }

    public async ValueTask SetPropTimeline(int propEntityId, PropTimelineInfo info)
    {
        if (SceneInstance == null) return;
        SceneInstance.Entities.TryGetValue(propEntityId, out var entity);
        if (entity is not EntityProp prop) return;

        var data = new ScenePropTimelineData
        {
            BoolValue = info.TimelineBoolValue,
            ByteValue = info.TimelineByteValue.ToBase64()
        };

        // save to db
        SceneData!.PropTimelineData.TryGetValue(Data.FloorId, out var floorData);
        if (floorData == null)
        {
            floorData = new Dictionary<int, Dictionary<int, ScenePropTimelineData>>();
            SceneData.PropTimelineData[Data.FloorId] = floorData;
        }

        if (!floorData.ContainsKey(prop.GroupId))
            floorData[prop.GroupId] = new Dictionary<int, ScenePropTimelineData>();

        floorData[prop.GroupId][prop.PropInfo.ID] = data;

        prop.PropTimelineData = data;

        // handle mission / quest
        await MissionManager!.HandleFinishType(MissionFinishTypeEnum.TimeLineSetState);
        await MissionManager!.HandleFinishType(MissionFinishTypeEnum.TimeLineSetStateCnt);
    }

    public ScenePropTimelineData? GetScenePropTimelineData(int floorId, int groupId, int propId)
    {
        SceneData!.PropTimelineData.TryGetValue(floorId, out var floorData);
        if (floorData == null) return null;
        floorData.TryGetValue(groupId, out var groupData);
        if (groupData == null) return null;
        groupData.TryGetValue(propId, out var data);
        return data;
    }

    public async ValueTask<bool> EnterScene(int entryId, int teleportId, bool sendPacket, int storyLineId = 0,
        bool mapTp = false)
    {
        // Preserve the existing return contract for callers that only need the over-map flag.
        return (await TryEnterScene(entryId, teleportId, sendPacket, storyLineId, mapTp)).IsOverMap;
    }

    public async ValueTask<(bool Success, bool IsOverMap)> TryEnterScene(int entryId, int teleportId,
        bool sendPacket, int storyLineId = 0, bool mapTp = false)
    {
        var beforeStoryLineId = StoryLineManager?.StoryLineData.CurStoryLineId ?? 0;
        if (entryId == 0)
        {
            // Story-line transitions with no entrance use the story's saved position.
            if (StoryLineManager == null || storyLineId == beforeStoryLineId) return (false, false);
            var beforeScene = SceneInstance;
            await StoryLineManager.EnterStoryLine(storyLineId, true);
            var transitioned = StoryLineManager.StoryLineData.CurStoryLineId == storyLineId
                               && SceneInstance != null && !ReferenceEquals(beforeScene, SceneInstance);
            return (transitioned, transitioned);
        }

        GameData.MapEntranceData.TryGetValue(entryId, out var entrance);
        if (entrance == null || !GameData.MazePlaneData.ContainsKey(entrance.PlaneID)) return (false, false);

        GameData.GetFloorInfo(entrance.PlaneID, entrance.FloorID, out var floorInfo);
        // GetFloorInfo hands back null for a floor the resource dump does not contain. Dereferencing it
        // used to throw, the handler answered a bare failure, and the client sat on the loading screen
        // with no way to tell what happened — report it instead.
        if (floorInfo == null)
        {
            SceneLogger.Warn($"EnterScene({entryId}) aborted: no floor data for P{entrance.PlaneID}_F{entrance.FloorID}.");
            return (false, false);
        }

        var startGroup = entrance.StartGroupID;
        var startAnchor = entrance.StartAnchorID;

        if (teleportId != 0)
        {
            floorInfo.CachedTeleports.TryGetValue(teleportId, out var teleport);
            if (teleport != null)
            {
                startGroup = teleport.AnchorGroupID;
                startAnchor = teleport.AnchorID;
            }
        }
        else if (startAnchor == 0)
        {
            startGroup = floorInfo.StartGroupID;
            startAnchor = floorInfo.StartAnchorID;
        }

        var anchor = floorInfo.GetAnchorInfo(startGroup, startAnchor)
                     // The entrance may point at a group the dump does not carry; fall back to the floor's
                     // own start anchor, then to any anchor at all, so a teleport degrades to "wrong spot"
                     // instead of "stuck on the loading screen".
                     ?? floorInfo.GetAnchorInfo(floorInfo.StartGroupID, floorInfo.StartAnchorID)
                     ?? floorInfo.Groups.Values.SelectMany(x => x.AnchorList).FirstOrDefault();
        if (anchor == null)
        {
            SceneLogger.Warn($"EnterScene({entryId}) aborted: no usable anchor on P{entrance.PlaneID}_F{entrance.FloorID} " +
                        $"(group {startGroup}, anchor {startAnchor}).");
            return (false, false);
        }

        // Validate the destination before changing the player's story-line state.
        if (storyLineId != beforeStoryLineId)
        {
            if (StoryLineManager == null) return (false, false);
            await StoryLineManager.EnterStoryLine(storyLineId, false);
            if (StoryLineManager.StoryLineData.CurStoryLineId != storyLineId) return (false, false);
            mapTp = false;
        }

        await MissionManager!.HandleFinishType(MissionFinishTypeEnum.EnterMapByEntrance, entrance);

        var beforeEntryId = Data.EntryId;

        var loaded = await TryLoadScene(entrance.PlaneID, entrance.FloorID, entryId, anchor.ToPositionProto(),
            anchor.ToRotationProto(), sendPacket, mapTp);

        return (loaded, loaded && (beforeEntryId != Data.EntryId || beforeStoryLineId != storyLineId));
    }

    /// <summary>
    ///     Moves the saved location to an entrance without building a scene, for login-time relocation that runs before
    ///     the login scene load. A null <paramref name="pos" />/<paramref name="rot" /> uses the entrance's start anchor.
    ///     Returns false, leaving the location untouched, when the entrance, its floor or its anchor is unusable or its
    ///     plane type is one of <paramref name="rejectPlaneTypes" />.
    /// </summary>
    public bool TryPlaceAtEntrance(int entryId, Position? pos = null, Position? rot = null,
        params PlaneTypeEnum[] rejectPlaneTypes)
    {
        if (!GameData.MapEntranceData.TryGetValue(entryId, out var entrance)) return false;
        if (!GameData.MazePlaneData.TryGetValue(entrance.PlaneID, out var plane) ||
            rejectPlaneTypes.Contains(plane.PlaneType))
            return false;

        if (pos == null || rot == null)
        {
            GameData.GetFloorInfo(entrance.PlaneID, entrance.FloorID, out var floor);
            var anchor = floor?.GetAnchorInfo(entrance.StartGroupID, entrance.StartAnchorID) ??
                         floor?.GetAnchorInfo(floor.StartGroupID, floor.StartAnchorID);
            if (anchor == null) return false;
            pos = anchor.ToPositionProto();
            rot = anchor.ToRotationProto();
        }

        Data.PlaneId = entrance.PlaneID;
        Data.FloorId = entrance.FloorID;
        Data.EntryId = entryId;
        Data.Pos = pos;
        Data.Rot = rot;
        March7thHoney.Database.DatabaseHelper.MarkDirty(Uid);
        return true;
    }

    public async ValueTask EnterSceneByEntranceId(int entranceId, int anchorGroupId, int anchorId, bool sendPacket)
    {
        GameData.MapEntranceData.TryGetValue(entranceId, out var entrance);
        if (entrance == null) return;

        GameData.GetFloorInfo(entrance.PlaneID, entrance.FloorID, out var floorInfo);
        if (floorInfo == null)
        {
            SceneLogger.Warn(
                $"EnterSceneByEntranceId({entranceId}) aborted: no floor data for P{entrance.PlaneID}_F{entrance.FloorID}.");
            return;
        }

        var startGroup = anchorGroupId == 0 ? entrance.StartGroupID : anchorGroupId;
        var startAnchor = anchorId == 0 ? entrance.StartAnchorID : anchorId;

        if (startAnchor == 0)
        {
            startGroup = floorInfo.StartGroupID;
            startAnchor = floorInfo.StartAnchorID;
        }

        var anchor = floorInfo.GetAnchorInfo(startGroup, startAnchor)
                     ?? floorInfo.GetAnchorInfo(floorInfo.StartGroupID, floorInfo.StartAnchorID)
                     ?? floorInfo.Groups.Values.SelectMany(x => x.AnchorList).FirstOrDefault();
        if (anchor == null)
        {
            SceneLogger.Warn($"EnterSceneByEntranceId({entranceId}) aborted: no usable anchor on " +
                        $"P{entrance.PlaneID}_F{entrance.FloorID} (group {startGroup}, anchor {startAnchor}).");
            return;
        }

        await MissionManager!.HandleFinishType(MissionFinishTypeEnum.EnterMapByEntrance, entrance);
        await LoadScene(entrance.PlaneID, entrance.FloorID, entranceId, anchor.ToPositionProto(),
            anchor.ToRotationProto(), sendPacket);
    }

    public async ValueTask MoveTo(Position position)
    {
        Data.Pos = position;
        await SendPacket(new PacketSceneEntityMoveScNotify(this));
    }

    public void MoveTo(EntityMotion motion)
    {
        Data.Pos = motion.Motion.Pos.ToPosition();
        Data.Rot = motion.Motion.Rot.ToPosition();
    }

    public async ValueTask MoveTo(Position pos, Position rot)
    {
        Data.Pos = pos;
        Data.Rot = rot;
        await SendPacket(new PacketSceneEntityMoveScNotify(this));
    }

    private readonly HashSet<long> _loadingFloors = [];

    public async ValueTask LoadScene(int planeId, int floorId, int entryId, Position pos, Position rot, bool sendPacket,
        bool mapTp = false)
    {
        await TryLoadScene(planeId, floorId, entryId, pos, rot, sendPacket, mapTp);
    }

    private async ValueTask<bool> TryLoadScene(int planeId, int floorId, int entryId, Position pos, Position rot,
        bool sendPacket, bool mapTp = false)
    {
        GameData.MazePlaneData.TryGetValue(planeId, out var plane);
        if (plane == null) return false;

        B51RacingManager?.ClearRace();
        var floorKey = ((long)planeId << 32) | (uint)floorId;
        if (!_loadingFloors.Add(floorKey)) return false; // cycle guard: floor already loading (orphan level-graph EnterMap) -> stack overflow
        try
        {
            if (plane.PlaneType == PlaneTypeEnum.Raid && RaidManager!.Data.CurRaidId == 0)
            {
                await EnterScene(DefaultSpawnEntryId, 0, sendPacket);
                return false; // The requested raid was not entered, even if the fallback succeeded.
            }

            Data.Pos = pos;
            Data.Rot = rot;
            var notSendMove = true;
            if (planeId != Data.PlaneId || floorId != Data.FloorId || entryId != Data.EntryId || SceneInstance == null ||
                !mapTp)
            {
                var previousScene = SceneInstance;
                // Rogue rooms on the same floor reuse client actors (official 4.6 seq 28808/29174).
                // Reserve the old entity range before loading props, then restore the actor IDs.
                Dictionary<(int AvatarId, AvatarType Type), int>? retainedAvatarIds = null;
                var previousLastEntityId = 0;
                if (!sendPacket && previousScene != null && previousScene.PlaneId == planeId &&
                    previousScene.FloorId == floorId && previousScene.EntryId == entryId &&
                    RogueManager?.GetActiveRoomExcel(entryId) != null)
                {
                    retainedAvatarIds = previousScene.AvatarInfo.Values.ToDictionary(
                        avatar => (avatar.AvatarInfo.BaseAvatarId, avatar.AvatarType), avatar => avatar.EntityId);
                    previousLastEntityId = previousScene.LastEntityId;
                }
                if (previousScene != null) await previousScene.OnDestroy();
                SceneInstance instance = new(this, plane, floorId, entryId) { LastEntityId = previousLastEntityId };
                SceneInstance = instance;
                await instance.LoadEntitiesAsync();

                await instance.SyncLineup(true, retainedAvatarIds);
                Data.PlaneId = planeId;
                Data.FloorId = floorId;
                Data.EntryId = entryId;
                RecordEnteredScene(planeId, floorId);
                March7thHoney.Database.DatabaseHelper.MarkDirty(Uid);
            }
            else if (StoryLineManager?.StoryLineData.CurStoryLineId == 0 &&
                     mapTp) // only send move packet when not in story line and mapTp
            {
                notSendMove = false;
            }

            if (MissionManager != null)
            {
                await MissionManager.OnPlayerChangeScene();
                await MissionManager.HandleFinishType(MissionFinishTypeEnum.EnterFloor);
                await MissionManager.HandleFinishType(MissionFinishTypeEnum.EnterPlane);
                await MissionManager.HandleFinishType(MissionFinishTypeEnum.NotInFloor);
                await MissionManager.HandleFinishType(MissionFinishTypeEnum.NotInPlane);
                if (SceneInstance != null) await SceneInstance.SyncGroupInfo();
            }

            if (sendPacket && notSendMove)
            {
                await SendPacket(new PacketSyncServerSceneChangeNotify());
                await SendPacket(new PacketEnterSceneByServerScNotify(SceneInstance!));
            }
            else if (sendPacket && !notSendMove) // send move packet
                await SendPacket(new PacketSceneEntityMoveScNotify(this));

            return Data.PlaneId == planeId && Data.FloorId == floorId && Data.EntryId == entryId;
        }
        finally
        {
            _loadingFloors.Remove(floorKey);
        }
    }

    public ScenePropData? GetScenePropData(int floorId, int groupId, int propId)
    {
        if (SceneData != null)
            if (SceneData.ScenePropData.TryGetValue(floorId, out var floorData))
                if (floorData.TryGetValue(groupId, out var groupData))
                {
                    var propData = groupData.Find(x => x.PropId == propId);
                    return propData;
                }

        return null;
    }

    public void SetScenePropData(int floorId, int groupId, int propId, PropStateEnum state)
    {
        if (SceneData != null)
        {
            if (!SceneData.ScenePropData.TryGetValue(floorId, out var floorData))
            {
                floorData = [];
                SceneData.ScenePropData.Add(floorId, floorData);
            }

            if (!floorData.TryGetValue(groupId, out var groupData))
            {
                groupData = [];
                floorData.Add(groupId, groupData);
            }

            var propData = groupData.Find(x => x.PropId == propId); // find prop data
            if (propData == null)
            {
                propData = new ScenePropData
                {
                    PropId = propId,
                    State = state
                };
                groupData.Add(propData);
            }
            else
            {
                propData.State = state;
            }
        }
    }

    public void SetGroupCustomSaveData(int entryId, int groupId, string saveData)
    {
        if (SceneData == null) return;

        if (!SceneData.CustomSaveData.TryGetValue(entryId, out var groupData))
        {
            groupData = [];
            SceneData.CustomSaveData.Add(entryId, groupData);
        }

        groupData[groupId] = saveData;
    }

    public void EnterSection(int sectionId)
    {
        // Section 0 is a real, enterable id. A rogue/raid plane records reveals on the scene, not the player.
        if (SceneInstance is { IsTransientPlane: true })
        {
            SceneInstance.RevealedSections.Add(sectionId);
            return;
        }

        if (SceneInstance != null)
        {
            SceneData!.UnlockSectionIdList.TryGetValue(SceneInstance.FloorId, out var unlockList);
            if (unlockList == null)
            {
                unlockList = [sectionId];
                SceneData.UnlockSectionIdList.Add(SceneInstance.FloorId, unlockList);
            }
            else
            {
                SceneData.UnlockSectionIdList[SceneInstance.FloorId].Add(sectionId);
            }
        }
    }

    public void SetCustomSaveData(int entryId, int groupId, string data)
    {
        if (SceneData != null)
        {
            if (!SceneData.CustomSaveData.TryGetValue(entryId, out var entryData))
            {
                entryData = [];
                SceneData.CustomSaveData.Add(entryId, entryData);
            }

            entryData[groupId] = data;
        }
    }

    public async ValueTask ForceQuitBattle()
    {
        if (BattleInstance != null)
        {
            BattleInstance = null;
        }
    }

    #endregion
}
