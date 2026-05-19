using March7thHoney.Data;
using March7thHoney.Data.Excel;

namespace March7thHoney.GameServer.Game.GridFight.Battle;

public record GridFightMonsterSpec(uint MonsterId, uint RoleStar, List<uint> DropItemIds);

public record GridFightLevelEncounter(
    uint StageId,
    uint PenaltyBonusRuleId,
    uint EliteGroupId,
    List<GridFightMonsterSpec> Monsters,
    List<uint> BindingBuffs,
    List<uint> BattleEvents,
    List<uint> TraitIds
);

public static class GridFightLevelResolver
{
    public const uint UnifiedStageId = 70000001;

    public static GridFightLevelEncounter Resolve(GridFightInstance inst)
    {
        var route = ResolveRoute(inst);
        var stageId = route?.StageID ?? UnifiedStageId;

        if (route?.NodeType == Enums.GridFight.GridFightNodeTypeEnum.Supply)
        {
            return new GridFightLevelEncounter(
                StageId: stageId,
                PenaltyBonusRuleId: 0u,
                EliteGroupId: 0u,
                Monsters: new List<GridFightMonsterSpec>(),
                BindingBuffs: new List<uint>(),
                BattleEvents: new List<uint>(),
                TraitIds: new List<uint>()
            );
        }

        var penaltyRuleId = ResolvePenaltyRuleId(inst);
        var (eliteGroup, monsters) = ResolveWave(inst);
        var bindingBuffs = ResolveBindingBuffs();
        var battleEvents = ResolveBattleEvents();
        var traitIds = ResolveTraitIds(inst);

        return new GridFightLevelEncounter(
            StageId: stageId,
            PenaltyBonusRuleId: penaltyRuleId,
            EliteGroupId: eliteGroup,
            Monsters: monsters,
            BindingBuffs: bindingBuffs,
            BattleEvents: battleEvents,
            TraitIds: traitIds
        );
    }

    public static GridFightStageRouteExcel? ResolveRoute(GridFightInstance inst)
    {
        var key = (inst.CurrentChapterId << 8) | inst.SectionId;
        if (GameData.GridFightStageRouteData.TryGetValue(inst.RouteId, out var routeBucket)
            && routeBucket.TryGetValue(key, out var route))
            return route;
        return GameData.GridFightStageRouteData.Values
            .SelectMany(d => d.Values)
            .FirstOrDefault(r => r.ChapterID == inst.CurrentChapterId && r.SectionID == inst.SectionId);
    }

    public static bool IsCombatNode(GridFightInstance inst)
    {
        var nt = ResolveRoute(inst)?.NodeType;
        return nt is not (null or Enums.GridFight.GridFightNodeTypeEnum.Supply);
    }

    private static uint ResolvePenaltyRuleId(GridFightInstance inst)
    {
        var route = ResolveRoute(inst);
        if (route == null) return 90303u;

        if (route.PenaltyBonusRuleIDList.Count > 0) return route.PenaltyBonusRuleIDList[0];
        if (GameData.GridFightNodeTemplateData.TryGetValue(route.NodeTemplateID, out var tpl) && tpl.PenaltyBonusRuleID > 0)
            return tpl.PenaltyBonusRuleID;
        return 90303u;
    }

    private static (uint EliteGroup, List<GridFightMonsterSpec> Monsters) ResolveWave(GridFightInstance inst)
    {
        var route = ResolveRoute(inst);

        var camp = ResolveCamp(inst);
        var monsters = camp?.Monsters ?? [];
        if (monsters.Count == 0)
        {
            return (1816u, new List<GridFightMonsterSpec>
            {
                new(201101005, 1, new List<uint> { 102 }),
                new(800104004, 1, new List<uint> { 199 }),
                new(800104004, 1, new List<uint> { 203 })
            });
        }

        var rng = Random.Shared;
        var wave = new List<GridFightMonsterSpec>();
        var nodeType = route?.NodeType;

        // 对齐 DH：普通怪点优先低 Tier，避免 1-1 刷出过高血量怪
        if (nodeType == Enums.GridFight.GridFightNodeTypeEnum.Monster)
        {
            var pool = monsters.Where(m => m.MonsterTier <= 2).ToList();
            if (pool.Count == 0) pool = monsters.ToList();
            for (var i = 0; i < 5 && pool.Count > 0; i++)
            {
                var pick = pool[rng.Next(pool.Count)];
                wave.Add(new GridFightMonsterSpec(pick.MonsterID, 1, []));
            }
            return (1816u, wave);
        }

        if (nodeType == Enums.GridFight.GridFightNodeTypeEnum.CampMonster
            || nodeType == Enums.GridFight.GridFightNodeTypeEnum.EliteBranch)
        {
            var targetTiers = new uint[] { 3, 2, 2, 2, 2 };
            foreach (var tier in targetTiers)
            {
                var pool = monsters.Where(m => m.MonsterTier == tier).ToList();
                if (pool.Count == 0) pool = monsters.Where(m => m.MonsterTier <= 3).ToList();
                if (pool.Count == 0) continue;
                var pick = pool[rng.Next(pool.Count)];
                wave.Add(new GridFightMonsterSpec(pick.MonsterID, 1, []));
            }
            if (wave.Count == 0)
            {
                var pick = monsters[rng.Next(monsters.Count)];
                wave.Add(new GridFightMonsterSpec(pick.MonsterID, 1, []));
            }
            return (1817u, wave);
        }

        if (nodeType == Enums.GridFight.GridFightNodeTypeEnum.Supply)
        {
            return (0u, new List<GridFightMonsterSpec>());
        }

        if (nodeType == Enums.GridFight.GridFightNodeTypeEnum.Boss)
        {
            var bossPool = monsters.Where(m => m.MonsterTier >= 5).ToList();
            if (bossPool.Count == 0) bossPool = monsters.ToList();
            var pick = bossPool[rng.Next(bossPool.Count)];
            wave.Add(new GridFightMonsterSpec(pick.MonsterID, 1, []));
            return (1819u, wave);
        }

        var fallbackPool = monsters.Where(m => m.MonsterTier <= 2).ToList();
        if (fallbackPool.Count == 0) fallbackPool = monsters.ToList();
        for (var i = 0; i < 3 && fallbackPool.Count > 0; i++)
        {
            var pick = fallbackPool[rng.Next(fallbackPool.Count)];
            wave.Add(new GridFightMonsterSpec(pick.MonsterID, 1, []));
        }
        return (1816u, wave);
    }

    private static GridFightCampExcel? ResolveCamp(GridFightInstance inst)
    {
        if (inst.SessionCampIds.Count > 0)
        {
            var idx = (int)(((inst.CurrentChapterId - 1) * 9 + (inst.SectionId - 1)) % (uint)inst.SessionCampIds.Count);
            for (var i = 0; i < inst.SessionCampIds.Count; i++)
            {
                var pick = inst.SessionCampIds[(idx + i) % inst.SessionCampIds.Count];
                if (GameData.GridFightCampData.TryGetValue(pick, out var camp) && camp.Monsters.Count > 0)
                    return camp;
            }
        }

        return GameData.GridFightCampData.Values
            .Where(c => c.Monsters.Count > 0 && (c.SeasonID == 0 || c.SeasonID == inst.Season))
            .OrderBy(c => c.ID)
            .FirstOrDefault();
    }

    private static List<uint> ResolveBindingBuffs() => new() { 35100001 };

    private static List<uint> ResolveBattleEvents() => new()
    {
        62210, 11216, 60014, 60020, 60023, 60024, 60028, 60036
    };

    private static List<uint> ResolveTraitIds(GridFightInstance inst)
    {
        var set = new HashSet<uint>();
        foreach (var roleId in inst.UniqueIdByPos.Values
                     .Select(uid => inst.RoleByUniqueId.GetValueOrDefault(uid))
                     .Where(r => r != 0))
        {
            if (!GameData.GridFightRoleBasicInfoData.TryGetValue(roleId, out var info)) continue;
            foreach (var t in info.TraitList) set.Add(t);
        }
        return set.ToList();
    }
}
