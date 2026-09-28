using March7thHoney.Data.Config.Scene;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Mission;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Scene;

/// <summary>
///     Runtime monster/prop injection for the /spawn GM command. Spawned entities live only in this scene
///     instance: they are never saved, disappear on scene rebuild, and leave with their group when it unloads.
/// </summary>
public partial class SceneInstance
{
    public const int GmSpawnMaxEntities = 5000;

    /// <summary>
    ///     Only ordinary world and story scenes. Challenge, Simulated Universe and activity scenes have their own
    ///     loaders and battle-end settlement that a foreign monster would corrupt.
    /// </summary>
    public bool SupportsGmSpawn => EntityLoader != null &&
                                   (EntityLoader.GetType() == typeof(SceneEntityLoader) ||
                                    EntityLoader.GetType() == typeof(StoryLineEntityLoader));

    /// <summary>
    ///     Spawns <paramref name="amount" /> copies of an NPC monster around <paramref name="origin" />. Group,
    ///     instance and EventID are borrowed from the first non-farm monster of a loaded group (loading one first
    ///     only when no loaded group has a usable monster), because the client drops entities of groups it has not
    ///     loaded.
    /// </summary>
    /// <param name="requireEventStage">The template's EventID must resolve to a stage (no stage was pinned).</param>
    /// <returns><c>null</c> when this floor has no usable monster template.</returns>
    public async ValueTask<List<EntityMonster>?> SpawnGmMonsters(NPCMonsterDataExcel excel, int amount,
        Position origin, int radius, bool requireEventStage, Action<EntityMonster>? configure = null)
    {
        bool Usable(MonsterInfo info) => info.FarmElementID == 0 && !info.IsDelete &&
                                         (!requireEventStage ||
                                          BattleInstance.TryResolveStageFromEvent(info.EventID,
                                              Player.Data.WorldLevel, out _));

        var template = await FindGmSpawnTemplate(group => group.MonsterList.FirstOrDefault(Usable));
        if (template == null) return null;
        var (group, monsterInfo) = template.Value;

        var spawned = new List<EntityMonster>();
        for (var i = 0; i < amount; i++)
        {
            var pos = GmSpawnPosition(origin, radius);
            var info = new MonsterInfo
            {
                ID = monsterInfo.ID,
                Name = monsterInfo.Name,
                NPCMonsterID = excel.ID,
                EventID = monsterInfo.EventID,
                PosX = pos.X / 1000f,
                PosY = pos.Y / 1000f,
                PosZ = pos.Z / 1000f
            };
            var monster = new EntityMonster(this, pos, new Position(), group.Id, monsterInfo.ID, excel, info)
            {
                IsGmSpawned = true
            };
            configure?.Invoke(monster);
            await AddEntity(monster, false);
            spawned.Add(monster);
        }

        await SendGmSpawnRefresh(spawned);
        return spawned;
    }

    /// <summary>
    ///     Spawns <paramref name="amount" /> copies of a maze prop, in the Open state, around
    ///     <paramref name="origin" />. Group and instance come from the first plain (non-farm, non-anchor) prop of a
    ///     loaded group. The copy carries none of the template's event, chest, cocoon or interact wiring.
    /// </summary>
    /// <returns><c>null</c> when this floor has no usable prop template.</returns>
    public async ValueTask<List<EntityProp>?> SpawnGmProps(MazePropExcel excel, int amount, Position origin,
        int radius)
    {
        var template = await FindGmSpawnTemplate(group =>
            group.PropList.FirstOrDefault(prop => prop.FarmElementID == 0 && prop.AnchorID == 0 && !prop.IsDelete));
        if (template == null) return null;
        var (group, propInfo) = template.Value;

        var spawned = new List<EntityProp>();
        for (var i = 0; i < amount; i++)
        {
            var pos = GmSpawnPosition(origin, radius);
            var info = new PropInfo
            {
                ID = propInfo.ID,
                Name = propInfo.Name,
                PropID = excel.ID,
                State = PropStateEnum.Open,
                PosX = pos.X / 1000f,
                PosY = pos.Y / 1000f,
                PosZ = pos.Z / 1000f
            };
            var prop = new EntityProp(this, excel, group, info)
            {
                Position = pos,
                Rotation = new Position(),
                State = PropStateEnum.Open,
                IsGmSpawned = true
            };
            await AddEntity(prop, false);
            spawned.Add(prop);
        }

        await SendGmSpawnRefresh(spawned);
        return spawned;
    }

    private async ValueTask<(GroupInfo Group, T Template)?> FindGmSpawnTemplate<T>(Func<GroupInfo, T?> pick)
        where T : class
    {
        if (FloorInfo == null || EntityLoader == null) return null;

        foreach (var groupId in Groups)
        {
            if (!FloorInfo.Groups.TryGetValue(groupId, out var group) ||
                group.LoadSide == GroupLoadSideEnum.Client) continue;
            if (pick(group) is { } template) return (group, template);
        }

        // No loaded group fits: activate one of this dimension's server groups first, preferring groups that
        // do not belong to a main mission. That group's own entities join the scene like any group load.
        var candidates = FloorInfo.Groups.Values
            .Where(group => group.LoadSide != GroupLoadSideEnum.Client && !Groups.Contains(group.Id) &&
                            EntityLoader.LoadGroups.Contains(group.Id))
            .OrderBy(group => group.OwnerMainMissionID != 0)
            .ThenBy(group => group.Id);
        foreach (var group in candidates)
        {
            if (pick(group) is not { } template) continue;
            await EntityLoader.LoadGroup(group.Id, IsLoaded);
            if (Groups.Contains(group.Id)) return (group, template);
        }

        return null;
    }

    private async ValueTask SendGmSpawnRefresh<T>(List<T> spawned) where T : BaseGameEntity
    {
        if (!IsLoaded || spawned.Count == 0) return;
        await Player.SendPacket(new PacketSceneGroupRefreshScNotify(Player, spawned.Cast<BaseGameEntity>().ToList()));
    }

    private static Position GmSpawnPosition(Position origin, int radius)
    {
        if (radius <= 0) return new Position(origin);

        return new Position(origin.X + Random.Shared.Next(-radius, radius + 1), origin.Y,
            origin.Z + Random.Shared.Next(-radius, radius + 1));
    }
}
