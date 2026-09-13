using March7thHoney.Data;
using March7thHoney.Data.Config.Scene;
using March7thHoney.Data.Excel;
using March7thHoney.Database.ChessRogue;
using March7thHoney.Enums.Rogue;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.ChessRogue;

/// <summary>
///     Loads the current cell's room groups on a ChessRogue plane and swaps its monsters for RogueMonster rows.
/// </summary>
public class ChessRogueEntityLoader(SceneInstance scene, ChessRogueCellData cell) : SceneEntityLoader(scene)
{
    // The event NPC hosts the rogue dialogue event, which this server does not implement.
    private const int RogueEventNpcId = 3013;
    private const int ChessRogueDoorPropId = 1026;

    public override async ValueTask LoadEntity()
    {
        if (Scene.IsLoaded || Scene.FloorInfo == null) return;

        LoadGroups.AddRange(Scene.FloorInfo.Groups.Keys);
        var roomGroups = cell.LoadGroups.ToHashSet();
        foreach (var group in Scene.FloorInfo.Groups.Values)
        {
            if (group.LoadSide == GroupLoadSideEnum.Client) continue;
            if (roomGroups.Contains(group.Id))
                await LoadGroup(group, true);
            else if (group.Category == GroupCategoryEnum.Normal)
                await LoadGroup(group);
        }

        Scene.IsLoaded = true;
    }

    public override async ValueTask<EntityNpc?> LoadNpc(NpcInfo info, GroupInfo group, bool sendPacket = false)
    {
        if (info.NPCID == RogueEventNpcId) return null;
        return await base.LoadNpc(info, group, sendPacket);
    }

    public override async ValueTask<EntityMonster?> LoadMonster(MonsterInfo info, GroupInfo group,
        bool sendPacket = false)
    {
        if (info.IsClientOnly || info.IsDelete) return null;

        RogueMonsterExcel? rogueMonster;
        if (cell.SelectMonsterId > 0)
        {
            GameData.RogueMonsterData.TryGetValue(cell.SelectMonsterId * 10 + 1, out rogueMonster);
        }
        else
        {
            List<MonsterRankEnum> ranks = (RogueDLCBlockTypeEnum)cell.BlockType == RogueDLCBlockTypeEnum.MonsterElite
                ? [MonsterRankEnum.Elite]
                : [MonsterRankEnum.Minion, MonsterRankEnum.MinionLv2];
            var candidates = GameData.RogueMonsterData.Values
                .Where(monster => GameData.NpcMonsterDataData.TryGetValue(monster.NpcMonsterID, out var npc) &&
                                  ranks.Contains(npc.Rank))
                .ToList();
            rogueMonster = candidates.Count == 0 ? null : candidates.RandomElement();
        }

        if (rogueMonster == null ||
            !GameData.NpcMonsterDataData.TryGetValue(rogueMonster.NpcMonsterID, out var excel)) return null;

        var entity = new EntityMonster(Scene, info.ToPositionProto(), info.ToRotationProto(), group.Id, info.ID,
            excel, info)
        {
            EventId = rogueMonster.EventID,
            CustomStageId = rogueMonster.EventID,
            RogueMonsterId = rogueMonster.RogueMonsterID
        };

        await Scene.AddEntity(entity, sendPacket);
        return entity;
    }

    public override async ValueTask<EntityProp?> LoadProp(PropInfo info, GroupInfo group, bool sendPacket = false)
    {
        var prop = await base.LoadProp(info, group, sendPacket);
        if (prop == null || info.PropID != ChessRogueDoorPropId) return prop;

        // The cell exit: CustomState02 opens it, CustomState04 marks a boss cell's exit.
        prop.State = (RogueDLCBlockTypeEnum)cell.BlockType is RogueDLCBlockTypeEnum.MonsterBoss
            or RogueDLCBlockTypeEnum.MonsterNousBoss or RogueDLCBlockTypeEnum.MonsterSwarmBoss
            ? PropStateEnum.CustomState04
            : PropStateEnum.CustomState02;
        return prop;
    }
}
