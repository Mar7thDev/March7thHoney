using March7thHoney.Data;
using March7thHoney.Enums.GridFight;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed record GridFightConsumableInventoryChange(
    uint GroupId,
    uint ItemId,
    uint RemainingCount,
    bool Removed);

public sealed record GridFightEquipDressResult(
    bool Success,
    IReadOnlyList<GridFightEquipmentState> RemovedEquipments,
    IReadOnlyList<GridFightEquipmentState> AddedEquipments,
    IReadOnlyList<GridFightRoleState> UpdatedRoles,
    bool AutoCrafted,
    bool MaxActiveRolesChanged)
{
    public static GridFightEquipDressResult Failed { get; } = new(false, [], [], [], false, false);
}

public sealed record GridFightEquipCraftResult(
    bool Success,
    IReadOnlyList<GridFightEquipmentState> RemovedEquipments,
    GridFightEquipmentState? AddedEquipment,
    bool MaxActiveRolesChanged)
{
    public static GridFightEquipCraftResult Failed { get; } = new(false, [], null, false);
}

public sealed record GridFightConsumableUseResult(
    bool Success,
    uint ItemId,
    GridFightConsumeTypeEnum Rule,
    GridFightConsumableInventoryChange? InventoryChange,
    IReadOnlyList<GridFightEquipmentState> RemovedEquipments,
    IReadOnlyList<GridFightEquipmentState> AddedEquipments,
    IReadOnlyList<GridFightRoleState> UpdatedRoles,
    GridFightRoleAcquisitionResult? RoleAcquisition,
    bool RecommendationStarted,
    bool MaxActiveRolesChanged)
{
    public static GridFightConsumableUseResult Failed { get; } =
        new(false, 0, GridFightConsumeTypeEnum.Remove, null, [], [], [], null, false, false);
}

public sealed record GridFightRecommendEquipmentSelectionResult(
    bool Success,
    uint FinishedQueuePosition,
    GridFightEquipmentState? Equipment)
{
    public static GridFightRecommendEquipmentSelectionResult Failed { get; } = new(false, 0, null);
}

public sealed record GridFightEquipmentRecommendationState(
    uint QueuePosition,
    uint RoleUniqueId,
    IReadOnlyList<uint> EquipmentIds);

public sealed partial class GridFightSession
{
    private const uint CraftedEquipmentSource = 5;
    private const uint RolledEquipmentSource = 0;
    private const uint RecommendedEquipmentSource = 0;
    private const int NormalEquipmentSlotCount = 3;

    public GridFightEquipmentRecommendationState? EquipmentRecommendation { get; private set; }

    public GridFightEquipDressResult DressEquipment(uint roleUniqueId, uint equipmentUniqueId)
    {
        if (!CanModifyEquipment()) return GridFightEquipDressResult.Failed;
        var role = Roles.FirstOrDefault(candidate => candidate.UniqueId == roleUniqueId);
        var equipment = Equipments.FirstOrDefault(candidate => candidate.UniqueId == equipmentUniqueId);
        if (role == null || equipment == null || role.EquippedEquipmentUniqueIds.Contains(equipmentUniqueId))
            return GridFightEquipDressResult.Failed;
        var previousWearer = FindEquipmentWearer(equipmentUniqueId);
        if (!CanDress(role, equipment)) return GridFightEquipDressResult.Failed;

        var previousMaxActiveRoles = MaxActiveRoles;
        var updatedRoles = new List<GridFightRoleState>();
        if (previousWearer != null)
        {
            previousWearer.EquippedEquipmentUniqueIds.Remove(equipmentUniqueId);
            updatedRoles.Add(previousWearer);
        }
        role.EquippedEquipmentUniqueIds.Add(equipmentUniqueId);
        if (!updatedRoles.Contains(role)) updatedRoles.Add(role);

        var removed = new List<GridFightEquipmentState>();
        var added = new List<GridFightEquipmentState>();
        var crafted = TryAutoCraftOnRole(role, equipmentUniqueId, removed, added);
        return new GridFightEquipDressResult(
            true,
            removed,
            added,
            updatedRoles,
            crafted,
            previousMaxActiveRoles != MaxActiveRoles);
    }

    public GridFightEquipCraftResult CraftEquipment(
        uint targetEquipmentId,
        IReadOnlyList<uint> materialUniqueIds)
    {
        if (!CanModifyEquipment() || materialUniqueIds.Count != 2 || materialUniqueIds.Distinct().Count() != 2)
            return GridFightEquipCraftResult.Failed;
        var materials = materialUniqueIds
            .Select(uniqueId => Equipments.FirstOrDefault(equipment => equipment.UniqueId == uniqueId))
            .ToList();
        if (materials.Any(equipment => equipment == null) ||
            materials.Any(equipment => FindEquipmentWearer(equipment!.UniqueId) != null))
            return GridFightEquipCraftResult.Failed;
        var materialIds = materials.Select(equipment => equipment!.EquipmentId).Order().ToList();
        var recipe = GameData.GridFightCraftConfigData.Values
            .Where(candidate => candidate.CraftEquipID == targetEquipmentId)
            .OrderBy(candidate => candidate.CraftID)
            .FirstOrDefault(candidate => candidate.CostEquipList.Order().SequenceEqual(materialIds));
        if (recipe == null || !GameData.GridFightEquipmentData.ContainsKey(recipe.CraftEquipID))
            return GridFightEquipCraftResult.Failed;

        var previousMaxActiveRoles = MaxActiveRoles;
        var removed = materials.Cast<GridFightEquipmentState>().ToList();
        foreach (var material in removed) Equipments.Remove(material);
        var added = CreateEquipment(recipe.CraftEquipID, CraftedEquipmentSource);
        return new GridFightEquipCraftResult(
            true,
            removed,
            added,
            previousMaxActiveRoles != MaxActiveRoles);
    }

    public GridFightConsumableUseResult UseConsumable(
        uint groupId,
        uint itemId,
        uint roleUniqueId,
        uint equipmentUniqueId)
    {
        if (!CanModifyEquipment() ||
            !GameData.GridFightConsumablesData.TryGetValue(itemId, out var config))
            return GridFightConsumableUseResult.Failed;
        var consumable = Consumables.FirstOrDefault(item => item.GroupId == groupId && item.ItemId == itemId);
        if (consumable == null || consumable.Count == 0) return GridFightConsumableUseResult.Failed;

        var previousMaxActiveRoles = MaxActiveRoles;
        var removed = new List<GridFightEquipmentState>();
        var added = new List<GridFightEquipmentState>();
        var updatedRoles = new List<GridFightRoleState>();
        GridFightRoleAcquisitionResult? acquisition = null;
        var recommendationStarted = false;
        var success = config.ConsumableRule switch
        {
            GridFightConsumeTypeEnum.Remove => RemoveEquipmentFromRole(roleUniqueId, updatedRoles),
            GridFightConsumeTypeEnum.Roll => RollEquipment(roleUniqueId, equipmentUniqueId, removed, added, updatedRoles),
            GridFightConsumeTypeEnum.Upgrade => UpgradeEquipment(equipmentUniqueId, removed, added, updatedRoles),
            GridFightConsumeTypeEnum.Copy => CopyRole(roleUniqueId, config.ConsumableParamList, out acquisition),
            GridFightConsumeTypeEnum.GainRecommendEquip => StartEquipmentRecommendation(
                roleUniqueId,
                config.ConsumableParamList,
                out recommendationStarted),
            _ => false,
        };
        if (!success) return GridFightConsumableUseResult.Failed;

        var inventoryChange = ConsumeConsumable(consumable, config.IfConsume);
        return new GridFightConsumableUseResult(
            true,
            itemId,
            config.ConsumableRule,
            inventoryChange,
            removed,
            added,
            updatedRoles,
            acquisition,
            recommendationStarted,
            previousMaxActiveRoles != MaxActiveRoles);
    }

    public GridFightRecommendEquipmentSelectionResult SelectRecommendedEquipment(
        uint queuePosition,
        uint equipmentId)
    {
        var recommendation = EquipmentRecommendation;
        if (recommendation == null || recommendation.QueuePosition != queuePosition ||
            !recommendation.EquipmentIds.Contains(equipmentId) ||
            !GameData.GridFightEquipmentData.ContainsKey(equipmentId))
            return GridFightRecommendEquipmentSelectionResult.Failed;
        var equipment = CreateEquipment(equipmentId, RecommendedEquipmentSource);
        EquipmentRecommendation = null;
        PendingQueuePosition++;
        return new GridFightRecommendEquipmentSelectionResult(true, queuePosition, equipment);
    }

    public IReadOnlyList<uint> ResolveRoleTraitIds(GridFightRoleState role)
    {
        var result = GameData.GridFightRoleBasicInfoData.GetValueOrDefault(role.RoleId)?.TraitList.ToHashSet() ?? [];
        foreach (var equipment in GetEquippedEquipment(role))
        {
            var config = GameData.GridFightEquipmentData.GetValueOrDefault(equipment.EquipmentId);
            if (config?.EquipFunc is GridFightEquipFuncTypeEnum.OriginEmblem or
                GridFightEquipFuncTypeEnum.ClassEmblem)
            {
                var traitId = config.EquipFuncParamList.FirstOrDefault();
                if (traitId != 0) result.Add(traitId);
            }
        }
        return result.Order().ToList();
    }

    public uint ResolveTraitExtraMemberCount(uint traitId, IEnumerable<GridFightRoleState> roles)
    {
        return (uint)roles.Sum(role => GetEquippedEquipment(role).Count(equipment =>
        {
            var config = GameData.GridFightEquipmentData.GetValueOrDefault(equipment.EquipmentId);
            return config?.EquipFunc == GridFightEquipFuncTypeEnum.AddTraitLayer &&
                   config.EquipFuncParamList.FirstOrDefault() == traitId;
        }));
    }

    public IReadOnlyList<GridFightEquipmentState> GetEquippedEquipment(GridFightRoleState role)
    {
        return role.EquippedEquipmentUniqueIds
            .Select(uniqueId => Equipments.FirstOrDefault(equipment => equipment.UniqueId == uniqueId))
            .Where(equipment => equipment != null)
            .Cast<GridFightEquipmentState>()
            .ToList();
    }

    private bool CanModifyEquipment()
    {
        return Phase == GridFightSessionPhase.Preparing && EquipmentRecommendation == null;
    }

    private GridFightRoleState? FindEquipmentWearer(uint equipmentUniqueId)
    {
        return Roles.FirstOrDefault(role => role.EquippedEquipmentUniqueIds.Contains(equipmentUniqueId));
    }

    private bool CanDress(GridFightRoleState role, GridFightEquipmentState equipment)
    {
        if (!GameData.GridFightEquipmentData.TryGetValue(equipment.EquipmentId, out var config)) return false;
        var equipped = GetEquippedEquipment(role)
            .Where(candidate => candidate.UniqueId != equipment.UniqueId)
            .ToList();
        var normalEquipped = equipped
            .Where(candidate => GameData.GridFightEquipmentData[candidate.EquipmentId].EquipType !=
                                GridFightEquipmentTypeEnum.Implants)
            .ToList();
        if (config.EquipType != GridFightEquipmentTypeEnum.Implants)
        {
            var willAutoCraft = normalEquipped.Any(candidate =>
                ResolveCraftedEquipmentId(equipment.EquipmentId, candidate.EquipmentId) != 0);
            if (normalEquipped.Count >= NormalEquipmentSlotCount && !willAutoCraft) return false;
            if (normalEquipped.Any(candidate =>
                    GameData.GridFightEquipmentData[candidate.EquipmentId].DressRule ==
                    GridFightEquipDressTypeEnum.DressRuleAllSlotEmpty))
                return false;
        }

        return config.DressRule switch
        {
            GridFightEquipDressTypeEnum.DressRuleNotUnique => true,
            GridFightEquipDressTypeEnum.DressRuleUnique =>
                equipped.All(candidate => candidate.EquipmentId != equipment.EquipmentId),
            GridFightEquipDressTypeEnum.DressRuleAllSlotEmpty => normalEquipped.Count == 0,
            GridFightEquipDressTypeEnum.DressRuleLeader => role.Position == 1,
            GridFightEquipDressTypeEnum.DressRuleTraitOnly =>
                config.DressRuleParamList.Any(ResolveRoleTraitIds(role).Contains),
            GridFightEquipDressTypeEnum.DressRuleUniqueAndExclusiveTrait =>
                equipped.All(candidate => candidate.EquipmentId != equipment.EquipmentId) &&
                config.DressRuleParamList.All(traitId => !ResolveRoleTraitIds(role).Contains(traitId)),
            GridFightEquipDressTypeEnum.DressRuleRoleOnly => config.DressRuleParamList.Contains(role.RoleId),
            _ => false,
        };
    }

    private bool TryAutoCraftOnRole(
        GridFightRoleState role,
        uint newlyEquippedUniqueId,
        ICollection<GridFightEquipmentState> removed,
        ICollection<GridFightEquipmentState> added)
    {
        var newlyEquipped = Equipments.First(equipment => equipment.UniqueId == newlyEquippedUniqueId);
        var craftCandidates = GetEquippedEquipment(role)
            .Where(equipment => equipment.UniqueId != newlyEquippedUniqueId)
            .Where(equipment => GameData.GridFightEquipmentData[equipment.EquipmentId].EquipType !=
                                GridFightEquipmentTypeEnum.Implants)
            .ToList();
        foreach (var other in craftCandidates)
        {
            var craftedEquipmentId = ResolveCraftedEquipmentId(newlyEquipped.EquipmentId, other.EquipmentId);
            if (craftedEquipmentId == 0) continue;

            var insertIndex = Math.Min(
                role.EquippedEquipmentUniqueIds.IndexOf(newlyEquipped.UniqueId),
                role.EquippedEquipmentUniqueIds.IndexOf(other.UniqueId));
            role.EquippedEquipmentUniqueIds.Remove(newlyEquipped.UniqueId);
            role.EquippedEquipmentUniqueIds.Remove(other.UniqueId);
            Equipments.Remove(newlyEquipped);
            Equipments.Remove(other);
            removed.Add(newlyEquipped);
            removed.Add(other);
            var crafted = CreateEquipment(craftedEquipmentId, CraftedEquipmentSource);
            role.EquippedEquipmentUniqueIds.Insert(Math.Max(0, insertIndex), crafted.UniqueId);
            added.Add(crafted);
            return true;
        }
        return false;
    }

    private static uint ResolveCraftedEquipmentId(uint firstEquipmentId, uint secondEquipmentId)
    {
        var materialIds = new[] { firstEquipmentId, secondEquipmentId }.Order().ToList();
        var recipe = GameData.GridFightCraftConfigData.Values
            .OrderBy(candidate => candidate.CraftID)
            .FirstOrDefault(candidate => candidate.CostEquipList.Order().SequenceEqual(materialIds));
        return recipe != null && GameData.GridFightEquipmentData.ContainsKey(recipe.CraftEquipID)
            ? recipe.CraftEquipID
            : 0;
    }

    private bool RemoveEquipmentFromRole(uint roleUniqueId, ICollection<GridFightRoleState> updatedRoles)
    {
        var role = Roles.FirstOrDefault(candidate => candidate.UniqueId == roleUniqueId);
        if (role == null || role.EquippedEquipmentUniqueIds.Count == 0) return false;
        role.EquippedEquipmentUniqueIds.Clear();
        updatedRoles.Add(role);
        return true;
    }

    private bool RollEquipment(
        uint roleUniqueId,
        uint equipmentUniqueId,
        ICollection<GridFightEquipmentState> removed,
        ICollection<GridFightEquipmentState> added,
        ICollection<GridFightRoleState> updatedRoles)
    {
        if (equipmentUniqueId != 0)
        {
            var equipment = Equipments.FirstOrDefault(candidate => candidate.UniqueId == equipmentUniqueId);
            if (equipment == null || FindEquipmentWearer(equipmentUniqueId) != null) return false;
            return RollOneEquipment(equipment, removed, added);
        }

        var role = Roles.FirstOrDefault(candidate => candidate.UniqueId == roleUniqueId);
        if (role == null) return false;
        var replacements = GetEquippedEquipment(role)
            .Select(equipment =>
            {
                var config = GameData.GridFightEquipmentData[equipment.EquipmentId];
                var replacementId = config.EquipType == GridFightEquipmentTypeEnum.Implants
                    ? 0
                    : _catalog.RollEquipment(config.EquipCategory, equipment.EquipmentId);
                return (Equipment: equipment, ReplacementId: replacementId);
            })
            .Where(replacement => replacement.ReplacementId != 0)
            .ToList();
        if (replacements.Count == 0) return false;
        foreach (var replacement in replacements)
        {
            role.EquippedEquipmentUniqueIds.Remove(replacement.Equipment.UniqueId);
            Equipments.Remove(replacement.Equipment);
            removed.Add(replacement.Equipment);
            added.Add(CreateEquipment(replacement.ReplacementId, RolledEquipmentSource));
        }
        updatedRoles.Add(role);
        return true;
    }

    private bool RollOneEquipment(
        GridFightEquipmentState equipment,
        ICollection<GridFightEquipmentState> removed,
        ICollection<GridFightEquipmentState> added)
    {
        var config = GameData.GridFightEquipmentData[equipment.EquipmentId];
        if (config.EquipType == GridFightEquipmentTypeEnum.Implants) return false;
        var replacementId = _catalog.RollEquipment(config.EquipCategory, equipment.EquipmentId);
        if (replacementId == 0) return false;
        Equipments.Remove(equipment);
        removed.Add(equipment);
        added.Add(CreateEquipment(replacementId, RolledEquipmentSource));
        return true;
    }

    private bool UpgradeEquipment(
        uint equipmentUniqueId,
        ICollection<GridFightEquipmentState> removed,
        ICollection<GridFightEquipmentState> added,
        ICollection<GridFightRoleState> updatedRoles)
    {
        var equipment = Equipments.FirstOrDefault(candidate => candidate.UniqueId == equipmentUniqueId);
        if (equipment == null ||
            !GameData.GridFightEquipUpgradeData.TryGetValue(equipment.EquipmentId, out var upgrade) ||
            !GameData.GridFightEquipmentData.ContainsKey(upgrade.UpgradeID))
            return false;
        var wearer = FindEquipmentWearer(equipmentUniqueId);
        if (wearer != null)
        {
            wearer.EquippedEquipmentUniqueIds.Remove(equipmentUniqueId);
            updatedRoles.Add(wearer);
        }
        Equipments.Remove(equipment);
        removed.Add(equipment);
        added.Add(CreateEquipment(upgrade.UpgradeID, equipment.Source));
        return true;
    }

    private bool CopyRole(
        uint roleUniqueId,
        IReadOnlyList<uint> parameters,
        out GridFightRoleAcquisitionResult? acquisition)
    {
        acquisition = null;
        var sourceRole = Roles.FirstOrDefault(role => role.UniqueId == roleUniqueId);
        if (sourceRole == null || parameters.Count < 2 ||
            !GameData.GridFightRoleBasicInfoData.TryGetValue(sourceRole.RoleId, out var roleConfig) ||
            roleConfig.Rarity > parameters[0] || parameters[1] != 1 || GetFreeBenchPosition() == 0)
            return false;
        var acquired = new List<GridFightRoleState>();
        var mergeSteps = new List<GridFightRoleMergeStep>();
        for (var index = 0u; index < parameters[1]; index++)
        {
            var role = new GridFightRoleState
            {
                UniqueId = _nextEntityUniqueId++,
                RoleId = sourceRole.RoleId,
                Star = 1,
                Position = GetFreeBenchPosition(),
            };
            if (role.Position == 0) return false;
            Roles.Add(role);
            acquired.Add(role);
            mergeSteps.AddRange(MergeRoles(role.RoleId, role));
        }
        acquisition = FinalizeRoleAcquisition(new GridFightRoleAcquisitionResult(
            true,
            acquired,
            mergeSteps,
            GetFinalMergedRoles(mergeSteps)));
        return true;
    }

    private bool StartEquipmentRecommendation(
        uint roleUniqueId,
        IReadOnlyList<uint> parameters,
        out bool started)
    {
        started = false;
        var role = Roles.FirstOrDefault(candidate => candidate.UniqueId == roleUniqueId);
        if (role == null || parameters.Count == 0 || parameters[0] == 0 ||
            !GameData.GridFightRoleBasicInfoData.TryGetValue(role.RoleId, out var roleConfig))
            return false;
        var equipmentIds = _catalog.RollRecommendedEquipment(
            role.RoleId,
            roleConfig.FrontBackType,
            checked((int)parameters[0]));
        if (equipmentIds.Count != parameters[0] ||
            equipmentIds.Any(id => !GameData.GridFightEquipmentData.ContainsKey(id)))
            return false;
        EquipmentRecommendation = new GridFightEquipmentRecommendationState(
            PendingQueuePosition,
            roleUniqueId,
            equipmentIds);
        started = true;
        return true;
    }

    private GridFightConsumableInventoryChange? ConsumeConsumable(
        GridFightConsumableState consumable,
        bool shouldConsume)
    {
        if (!shouldConsume) return null;
        consumable.Count--;
        var removed = consumable.Count == 0;
        if (removed) Consumables.Remove(consumable);
        return new GridFightConsumableInventoryChange(
            consumable.GroupId,
            consumable.ItemId,
            consumable.Count,
            removed);
    }

    private GridFightEquipmentState CreateEquipment(uint equipmentId, uint source)
    {
        var equipment = new GridFightEquipmentState
        {
            UniqueId = _nextEntityUniqueId++,
            EquipmentId = equipmentId,
            Source = source,
        };
        Equipments.Add(equipment);
        return equipment;
    }
}
