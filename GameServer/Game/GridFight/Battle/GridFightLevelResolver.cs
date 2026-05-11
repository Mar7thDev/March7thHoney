using March7thHoney.Data;

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
        var stageId = UnifiedStageId;
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

    private static uint ResolvePenaltyRuleId(GridFightInstance inst)
    {
        var route = GameData.GridFightStageRouteData.Values
            .SelectMany(d => d.Values)
            .FirstOrDefault(r => r.ChapterID == inst.CurrentChapterId && r.SectionID == inst.SectionId);
        if (route == null) return 90303u;

        if (route.PenaltyBonusRuleIDList.Count > 0) return route.PenaltyBonusRuleIDList[0];
        if (GameData.GridFightNodeTemplateData.TryGetValue(route.NodeTemplateID, out var tpl) && tpl.PenaltyBonusRuleID > 0)
            return tpl.PenaltyBonusRuleID;
        return 90303u;
    }

    private static (uint EliteGroup, List<GridFightMonsterSpec> Monsters) ResolveWave(GridFightInstance inst)
    {
        // 1-1 默认 wave（与官服抓包一致）
        return (1816u, new List<GridFightMonsterSpec>
        {
            new(201101005, 1, new List<uint> { 102, 204 }),
            new(800104004, 1, new List<uint> { 203 }),
            new(800104004, 1, new List<uint> { 199 })
        });
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
