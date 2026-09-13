using March7thHoney.Data;
using March7thHoney.Data.Config.Scene;
using March7thHoney.Database.RogueTourn;
using March7thHoney.Enums.Scene;
using March7thHoney.Enums.TournRogue;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;

namespace March7thHoney.GameServer.Game.RogueTourn;

/// <summary>
///     Loads only the current room's block of a Divergent Universe plane: the official scene ships the
///     room's area/content groups plus the ambient decoration, never every block of the floor.
/// </summary>
public class RogueTournEntityLoader : SceneEntityLoader
{
    private readonly HashSet<int> _roomGroups;
    private readonly Dictionary<int, HashSet<int>> _propWhitelist;
    private readonly Dictionary<int, List<RogueTournMonsterSlot>> _stageMonsters;

    // A plane without room blocks falls back to the plain loader, like an absent override.
    private readonly bool _filtered;

    public RogueTournEntityLoader(SceneInstance scene, RogueTournRoomData room) : base(scene)
    {
        var roomType = (RogueTournRoomTypeEnum)room.RoomType;
        _roomGroups = RogueTournResources.RoomBlockGroups(room.PlaneId, roomType, room.RoomIndex);
        _filtered = _roomGroups.Count > 0;
        _propWhitelist = _filtered ? RogueTournResources.RoomPropWhitelist(room.PlaneId, room.RoomIndex) : [];
        _stageMonsters = _filtered
            ? RogueTournResources.RoomStageMonsters(room.PlaneId, roomType, room.RoomIndex, room.Level)
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
            CustomLevel = slot.Level
        };

        await Scene.AddEntity(entity, sendPacket);
        return entity;
    }

    public override async ValueTask<EntityProp?> LoadProp(PropInfo info, GroupInfo group, bool sendPacket = false)
    {
        if (_filtered && _propWhitelist.TryGetValue(group.Id, out var allow) && !allow.Contains(info.ID))
            return null;

        var prop = await base.LoadProp(info, group, sendPacket);

        // The area group's door is the room's exit marker: ship it shut (CustomState01) unless it was
        // already opened (CustomState04).
        if (_filtered && prop != null && prop.Excel.PropType == PropTypeEnum.PROP_ROGUE_DOOR &&
            prop.State != PropStateEnum.CustomState04)
            prop.State = PropStateEnum.CustomState01;

        return prop;
    }
}
