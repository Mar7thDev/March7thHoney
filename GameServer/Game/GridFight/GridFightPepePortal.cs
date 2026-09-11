using March7thHoney.Data;
using March7thHoney.Enums.GridFight;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed class GridFightNpcState
{
    public uint UniqueId { get; init; }
    public uint NpcId { get; init; }
    public uint Position { get; init; }
    public List<uint> EquippedEquipmentUniqueIds { get; } = [];
}

public sealed record GridFightPepeDropResult(
    bool Success,
    GridFightEquipmentState? Equipment,
    GridFightNpcState? Npc)
{
    public static GridFightPepeDropResult Failed { get; } = new(false, null, null);
}

public sealed partial class GridFightSession
{
    public const uint ThreeStarPepePortalId = 115;
    public const uint RoughStarPepePortalId = 116;

    // The official 4.5 capture creates NPC 7 at position 11 for portal 116. Per the
    // requested 115/116 shared implementation, 115 uses the same carrier and differs
    // only by not scheduling post-battle emblem drops.
    public const uint PepeNpcId = 7;
    public const uint PepeNpcPosition = 11;
    public const uint PepeEquipmentSource = 4;

    // Current official sample: 1 drop in 5 completed battles. Keep this isolated so a
    // larger capture set can correct the rate without touching the NPC/equipment state machine.
    public const double RoughPepeObservedDropChance = 0.20;

    private bool _roughPepeDropPending;

    public GridFightNpcState? PepeNpc { get; private set; }

    public bool HasPepePortal => SelectedPortalId is ThreeStarPepePortalId or RoughStarPepePortalId;

    public uint OffFieldMaxCount => Math.Min(9u,
        _catalog.OffFieldInitialCount +
        EffectiveWealthDiamondBonus +
        LizhiBenchBonus +
        (PepeNpc == null ? 0u : 1u));

    // GridFightSyncData uses this value during Pepe's pre-initial-supply effect. The battle
    // snapshot still keeps MaxActiveRoles as the normal player-role deployment cap and counts
    // Pepe separately in ActiveRoleCount, matching the official packet shape.
    public uint PepeDisplayRoleCount => MaxActiveRoles + (PepeNpc == null ? 0u : 1u);

    public IReadOnlyList<GridFightEquipmentState> GetPepeEquippedEquipment()
    {
        if (PepeNpc == null) return [];
        return PepeNpc.EquippedEquipmentUniqueIds
            .Select(uniqueId => Equipments.FirstOrDefault(equipment => equipment.UniqueId == uniqueId))
            .Where(equipment => equipment != null)
            .Cast<GridFightEquipmentState>()
            .ToList();
    }

    public IReadOnlyList<(uint TraitId, GridFightEquipmentState Equipment)> ResolvePepeTraitMembers()
    {
        var result = new List<(uint TraitId, GridFightEquipmentState Equipment)>();
        foreach (var equipment in GetPepeEquippedEquipment())
        {
            var config = GameData.GridFightEquipmentData.GetValueOrDefault(equipment.EquipmentId);
            if (config?.EquipFunc is not (GridFightEquipFuncTypeEnum.OriginEmblem or
                GridFightEquipFuncTypeEnum.ClassEmblem))
                continue;
            var traitId = config.EquipFuncParamList.FirstOrDefault();
            if (traitId != 0) result.Add((traitId, equipment));
        }
        return result;
    }

    private void InitializePepePortal()
    {
        PepeNpc = null;
        _roughPepeDropPending = false;
        if (!HasPepePortal) return;

        var emblemIds = _catalog.RollPepeEmblems(3);
        if (emblemIds.Count != 3) return;

        var equippedUniqueIds = new List<uint>(3);
        foreach (var emblemId in emblemIds)
        {
            if (!TryAddEquipment(emblemId, out var equipment, PepeEquipmentSource, isLocked: true))
            {
                foreach (var uniqueId in equippedUniqueIds)
                    Equipments.RemoveAll(candidate => candidate.UniqueId == uniqueId);
                return;
            }
            equippedUniqueIds.Add(equipment.UniqueId);
        }

        var npc = new GridFightNpcState
        {
            UniqueId = _nextEntityUniqueId++,
            NpcId = PepeNpcId,
            Position = PepeNpcPosition,
        };
        npc.EquippedEquipmentUniqueIds.AddRange(equippedUniqueIds);
        PepeNpc = npc;
    }

    private void ScheduleRoughPepeDrop(bool battleEndedNormally)
    {
        _roughPepeDropPending = battleEndedNormally &&
                                SelectedPortalId == RoughStarPepePortalId &&
                                PepeNpc?.EquippedEquipmentUniqueIds.Count > 0 &&
                                _catalog.RollChance(RoughPepeObservedDropChance);
    }

    public GridFightPepeDropResult TakePendingPepeDrop()
    {
        if (!_roughPepeDropPending || SelectedPortalId != RoughStarPepePortalId || PepeNpc == null)
            return GridFightPepeDropResult.Failed;
        _roughPepeDropPending = false;

        var uniqueId = _catalog.RollOne(PepeNpc.EquippedEquipmentUniqueIds);
        if (uniqueId == 0) return GridFightPepeDropResult.Failed;
        var equipment = Equipments.FirstOrDefault(candidate => candidate.UniqueId == uniqueId);
        if (equipment == null) return GridFightPepeDropResult.Failed;

        PepeNpc.EquippedEquipmentUniqueIds.Remove(uniqueId);
        equipment.IsLocked = false;
        return new GridFightPepeDropResult(true, equipment, PepeNpc);
    }
}
