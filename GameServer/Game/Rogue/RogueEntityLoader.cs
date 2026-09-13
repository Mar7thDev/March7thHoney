using March7thHoney.Data;
using March7thHoney.Data.Config.Scene;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;

namespace March7thHoney.GameServer.Game.Rogue;

public class RogueEntityLoader(SceneInstance scene, PlayerInstance player) : SceneEntityLoader(scene)
{
    public override async ValueTask LoadEntity()
    {
        if (!Scene.IsLoaded && player.RogueManager?.GetActiveRoomExcel(Scene.EntryId) != null)
            Scene.LeaveEntryId = RogueManager.RogueEntranceId;

        await base.LoadEntity();
    }

    public override async ValueTask<EntityMonster?> LoadMonster(MonsterInfo info, GroupInfo group,
        bool sendPacket = false)
    {
        var manager = player.RogueManager;
        var roomExcel = manager?.GetActiveRoomExcel(Scene.EntryId);
        if (manager == null || roomExcel == null) return await base.LoadMonster(info, group, sendPacket);

        // In a live room only the room's own groups spawn, each swapped to a drawn RogueMonster.
        if (info.IsClientOnly || info.IsDelete) return null;
        var content = roomExcel.GroupWithContent.GetValueOrDefault(group.Id);
        if (content == 0) return null;

        var rogueMonsterId = RogueManager.ResolveRogueMonsterId(content);
        if (!GameData.RogueMonsterData.TryGetValue(rogueMonsterId, out var rogueMonster)) return null;
        if (!GameData.NpcMonsterDataData.TryGetValue(rogueMonster.NpcMonsterID, out var npcMonster)) return null;

        var entity = new EntityMonster(Scene, info.ToPositionProto(), info.ToRotationProto(), group.Id, info.ID,
            npcMonster, info)
        {
            EventId = rogueMonster.EventID,
            CustomStageId = rogueMonster.EventID,
            RogueMonsterId = rogueMonsterId,
            CustomLevel = manager.GetCurrentMonsterLevel()
        };

        await Scene.AddEntity(entity, sendPacket);
        return entity;
    }
}
