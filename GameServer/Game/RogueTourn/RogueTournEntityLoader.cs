using March7thHoney.Data;
using March7thHoney.Data.Config.Scene;
using March7thHoney.Database.RogueTourn;
using March7thHoney.Enums.Scene;
using March7thHoney.Enums.TournRogue;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;

namespace March7thHoney.GameServer.Game.RogueTourn;

/// <summary>
///     Loads only the current room's block of a Divergent Universe plane: the official scene ships the
///     room's area/content groups plus the ambient decoration, never every block of the floor.
/// </summary>
public class RogueTournEntityLoader : SceneEntityLoader
{
    // A room door shut until the room is cleared (CustomState01) and opened afterwards (CustomState04).
    public const PropStateEnum DoorShut = PropStateEnum.CustomState01;
    public const PropStateEnum DoorOpen = PropStateEnum.CustomState04;

    private HashSet<int> _roomGroups = [];
    private Dictionary<int, HashSet<int>> _propWhitelist = [];
    private Dictionary<int, List<RogueTournMonsterSlot>> _stageMonsters = [];

    // A plane without room blocks falls back to the plain loader, like an absent override.
    private bool _filtered;

    public RogueTournEntityLoader(SceneInstance scene, RogueTournRoomData room) : base(scene)
    {
        Target(room);
    }

    public int RoomUniqueId { get; private set; }

    // Rogue planes keep nothing between runs: prop states belong to the run, not to the player.
    public override bool PersistsPropState => false;

    private void Target(RogueTournRoomData room)
    {
        var roomType = (RogueTournRoomTypeEnum)room.RoomType;
        RoomUniqueId = room.UniqueId;
        _roomGroups = RogueTournResources.RoomBlockGroups(room.PlaneId, roomType, room.RoomIndex);
        _filtered = _roomGroups.Count > 0;
        _propWhitelist = _filtered ? RogueTournResources.RoomPropWhitelist(room.PlaneId, room.RoomIndex) : [];
        // The mobs play at the layer difficulty's ladder entry; a run without one keeps the card level.
        var level = room.MonsterLevel != 0 ? room.MonsterLevel : room.Level;
        _stageMonsters = _filtered
            ? RogueTournResources.RoomStageMonsters(room.PlaneId, roomType, room.RoomIndex, level)
            : [];
    }

    public override async ValueTask LoadEntity()
    {
        if (!_filtered)
        {
            await base.LoadEntity();
            return;
        }

        if (Scene.IsLoaded || Scene.FloorInfo == null) return;

        LoadGroups.AddRange(_roomGroups);
        // Room content groups are not in the floor's dimension-0 map, so they must be force-loaded.
        foreach (var groupId in _roomGroups.OrderBy(id => id))
            if (Scene.FloorInfo.Groups.TryGetValue(groupId, out var group))
                await LoadGroup(group, true);

        Scene.IsLoaded = true;
    }

    /// <summary>
    ///     Re-targets the live layer scene at another room of the same floor: the previous room's entities leave, the
    ///     new block loads with fresh entity ids from this scene's counter, and the ambient groups both rooms share keep
    ///     their entities. The official keeps ONE scene per layer and ships every room move under it; a fresh scene per
    ///     room handed the client a scene it never adopted (black world). Returns the newly loaded group ids.
    /// </summary>
    public async ValueTask<List<int>> SwapRoom(RogueTournRoomData room)
    {
        Target(room);
        if (!_filtered || Scene.FloorInfo == null) return [];

        foreach (var entity in Scene.Entities.Values.Where(entity => entity.GroupId != 0 &&
                                                                     !_roomGroups.Contains(entity.GroupId)).ToList())
            await Scene.RemoveEntity(entity, false);
        Scene.Groups.RemoveAll(groupId => !_roomGroups.Contains(groupId));
        Scene.HealingSprings.RemoveAll(spring => !_roomGroups.Contains(spring.GroupId));

        LoadGroups.Clear();
        LoadGroups.AddRange(_roomGroups);
        List<int> loaded = [];
        foreach (var groupId in _roomGroups.OrderBy(id => id))
        {
            if (Scene.Groups.Contains(groupId)) continue;
            if (!Scene.FloorInfo.Groups.TryGetValue(groupId, out var group)) continue;
            await LoadGroup(group, true);
            loaded.Add(groupId);
        }

        return loaded;
    }

    // Mission reconciliation walks every floor group; letting it run would re-add the other blocks.
    public override ValueTask SyncEntity()
    {
        return _filtered ? ValueTask.CompletedTask : base.SyncEntity();
    }

    public override async ValueTask<EntityMonster?> LoadMonster(MonsterInfo info, GroupInfo group,
        bool sendPacket = false)
    {
        if (!_filtered) return await base.LoadMonster(info, group, sendPacket);
        if (info.IsDelete || !_stageMonsters.TryGetValue(group.Id, out var slots)) return null;

        var index = slots.FindIndex(slot => slot.ConfigId == info.ID);
        if (index < 0) return null;
        var slot = slots[index];

        if (!GameData.NpcMonsterDataData.TryGetValue(slot.NpcMonsterId, out var excel)) return null;

        var entity = new EntityMonster(Scene, info.ToPositionProto(), info.ToRotationProto(), group.Id, info.ID,
            excel, info)
        {
            EventId = slot.EventId,
            CustomStageId = slot.EventId,
            RogueMonsterId = slot.RogueMonsterId,
            HardLevelGroup = slot.HardLevelGroup,
            CustomLevel = slot.Level,
            // Every DU room mob of the 4.6 run stamps rogue_game_info.DPCNDADDCAJ = 1.
            RogueMonsterKind = 1
        };

        await Scene.AddEntity(entity, sendPacket);
        return entity;
    }

    public override async ValueTask<EntityProp?> LoadProp(PropInfo info, GroupInfo group, bool sendPacket = false)
    {
        if (_filtered && _propWhitelist.TryGetValue(group.Id, out var allow) && !allow.Contains(info.ID))
            return null;

        var prop = await base.LoadProp(info, group, sendPacket);

        // The area group's door is the room's exit marker: it ships shut until the room is cleared.
        if (_filtered && prop != null && prop.Excel.PropType == PropTypeEnum.PROP_ROGUE_DOOR &&
            prop.State != DoorOpen)
            prop.State = DoorShut;

        return prop;
    }

    /// <summary>
    ///     Opens every shut room door (the server side of SetAllRogueDoorState) and returns the doors that changed.
    ///     sendPacket=false when the scene itself is about to be serialized, so the open state rides that payload.
    /// </summary>
    public static async ValueTask<List<EntityProp>> OpenRoomDoors(SceneInstance scene, bool sendPacket)
    {
        List<EntityProp> opened = [];
        foreach (var door in scene.Entities.Values.OfType<EntityProp>()
                     .Where(prop => prop.Excel.PropType == PropTypeEnum.PROP_ROGUE_DOOR && prop.State != DoorOpen)
                     .ToList())
        {
            await door.SetState(DoorOpen, sendPacket);
            opened.Add(door);
        }

        return opened;
    }

    public static int AliveMonsterCount(SceneInstance scene)
    {
        return scene.Entities.Values.OfType<EntityMonster>().Count(monster => monster.IsAlive);
    }

    public static bool IsTournRoomScene(PlayerInstance player)
    {
        return player.SceneInstance is { GameModeType: GameModeTypeEnum.TournRogue, EntityLoader: RogueTournEntityLoader };
    }
}
