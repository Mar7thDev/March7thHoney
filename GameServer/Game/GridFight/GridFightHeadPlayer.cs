using March7thHoney.Data;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed record GridFightHeadPlayerPendingState(
    uint QueuePosition,
    uint RoleUniqueId,
    uint RoleId,
    uint EffectId,
    IReadOnlyList<uint> OptionIds,
    bool IsFinalHack);

public sealed record GridFightHeadPlayerEnhanceResult(
    bool Success,
    uint FinishedQueuePosition,
    uint OptionId,
    uint EffectId,
    GridFightRoleState? UpdatedRole,
    IReadOnlyList<uint> RemovedRoleIds,
    GridFightEquipmentState? Equipment,
    bool GoldChanged,
    bool ShopChanged,
    bool IsFinalHack)
{
    public GridFightRoleAcquisitionResult? JoyContractReward { get; init; }
    public uint JoyContractEffectParam { get; init; }

    public static GridFightHeadPlayerEnhanceResult Failed { get; } =
        new(false, 0, 0, 0, null, [], null, false, false, false);
}

public sealed partial class GridFightSession
{
    private readonly HashSet<(uint UniqueId, uint RoleId, uint Star)> _handledHeadPlayerStates = [];
    private GridFightHeadPlayerPendingState? _headPlayerPending;
    private bool _silverWolfTraitInitialized;
    private bool _silverWolfUpgradeRouteClosed;
    private uint _silverWolfCurrentRoleId;

    public GridFightHeadPlayerPendingState? HeadPlayerPending => _headPlayerPending;
    public bool SilverWolfTraitInitialized => _silverWolfTraitInitialized;
    public bool SilverWolfUpgradeRouteClosed => _silverWolfUpgradeRouteClosed;
    public uint SilverWolfCurrentRoleId => _silverWolfCurrentRoleId;

    private void ObserveSilverWolfProgression()
    {
        var highestRoleId = Roles
            .Where(role => GridFightSpecialFeatures.IsSilverWolfLv999Role(role.RoleId))
            .Select(role => role.RoleId)
            .OrderByDescending(roleId => GridFightSpecialFeatures.GetSilverWolfCostTier(roleId))
            .FirstOrDefault();
        if (highestRoleId == 0) return;

        if (_silverWolfCurrentRoleId == 0 ||
            GridFightSpecialFeatures.GetSilverWolfCostTier(highestRoleId) >
            GridFightSpecialFeatures.GetSilverWolfCostTier(_silverWolfCurrentRoleId))
            _silverWolfCurrentRoleId = highestRoleId;
    }

    private bool TryInitializeSilverWolfTrait()
    {
        if (_silverWolfTraitInitialized) return false;
        if (!Roles.Any(role =>
                GridFightSpecialFeatures.IsSilverWolfLv999Role(role.RoleId) && IsActivePosition(role.Position)))
            return false;
        _silverWolfTraitInitialized = true;
        return true;
    }

    private bool TryScheduleHeadPlayerPending()
    {
        if (_headPlayerPending != null || _fateDivinerPending != null || EquipmentRecommendation != null ||
            Phase != GridFightSessionPhase.Preparing) return false;

        // Five-cost three-star is the already-implemented final HackConsole window and has
        // priority over the ordinary two-choice windows if several eligible copies coexist.
        var candidates = Roles
            .Where(role =>
                GridFightSpecialFeatures.IsSilverWolfLv999Role(role.RoleId) &&
                IsActivePosition(role.Position) &&
                role.Star >= 2)
            .OrderByDescending(role =>
                role.RoleId == GridFightSpecialFeatures.SilverWolfFiveCostRoleId && role.Star >= 3)
            .ThenBy(role => role.Position)
            .ThenByDescending(role => role.Star)
            .ThenBy(role => role.UniqueId)
            .ToList();

        foreach (var role in candidates)
        {
            var stateKey = (role.UniqueId, role.RoleId, role.Star);
            if (_handledHeadPlayerStates.Contains(stateKey)) continue;

            var isFinalHack = role.RoleId == GridFightSpecialFeatures.SilverWolfFiveCostRoleId && role.Star >= 3;
            if (isFinalHack && _silverWolfHackUnlocked) continue;

            var options = BuildHeadPlayerOptions(role, isFinalHack);
            if (options.Count == 0) continue;

            var effectId = GridFightSpecialFeatures.GetSilverWolfTraitEffectId(role.RoleId);
            if (effectId == 0) continue;

            _handledHeadPlayerStates.Add(stateKey);
            if (isFinalHack) _silverWolfHackUnlocked = true;
            _headPlayerPending = new GridFightHeadPlayerPendingState(
                PendingQueuePosition,
                role.UniqueId,
                role.RoleId,
                effectId,
                options,
                isFinalHack);
            _traitEffectSelectEnhanceQueuePosition = PendingQueuePosition;
            return true;
        }

        return false;
    }

    private IReadOnlyList<uint> BuildHeadPlayerOptions(GridFightRoleState role, bool isFinalHack)
    {
        if (isFinalHack) return [GridFightSpecialFeatures.SilverWolfEnhanceOptionId];

        var itemPool = GridFightSpecialFeatures.GetSilverWolfEquipmentOptionPool(role.RoleId);
        if (itemPool.Count == 0) return [];

        // Capture-confirmed layout: while the cost-up route is still available the random
        // equipment choice is on the left, and the fixed cost-up option is on the right.
        if (!_silverWolfUpgradeRouteClosed && role.RoleId == GridFightSpecialFeatures.SilverWolfThreeCostRoleId)
            return [PickOne(itemPool), GridFightSpecialFeatures.SilverWolfUpgradeToFourCostOptionId];
        if (!_silverWolfUpgradeRouteClosed && role.RoleId == GridFightSpecialFeatures.SilverWolfFourCostRoleId)
            return [PickOne(itemPool), GridFightSpecialFeatures.SilverWolfUpgradeToFiveCostOptionId];

        // Five-cost has no ordinary cost-up option. Once the player declines an earlier cost-up,
        // subsequent windows likewise contain only equipment choices. Keep the choices distinct.
        return PickTwoDistinct(itemPool);
    }

    private uint PickOne(IReadOnlyList<uint> pool) => _catalog.RollOne(pool);

    private static IReadOnlyList<uint> PickTwoDistinct(IReadOnlyList<uint> pool)
    {
        if (pool.Count == 1) return [pool[0]];
        var firstIndex = Random.Shared.Next(pool.Count);
        var secondIndex = Random.Shared.Next(pool.Count - 1);
        if (secondIndex >= firstIndex) secondIndex++;
        return [pool[firstIndex], pool[secondIndex]];
    }

    private (bool TraitActivated, bool PendingStarted) RefreshHeadPlayerAfterRoleStateChange()
    {
        ObserveSilverWolfProgression();
        var traitActivated = TryInitializeSilverWolfTrait();
        var pendingStarted = _silverWolfTraitInitialized && TryScheduleHeadPlayerPending();
        return (traitActivated, pendingStarted);
    }

    public GridFightHeadPlayerEnhanceResult ResolveHeadPlayerEnhance(uint queuePosition, uint optionId)
    {
        var pending = _headPlayerPending;
        if (pending == null || pending.QueuePosition != queuePosition || !pending.OptionIds.Contains(optionId))
            return GridFightHeadPlayerEnhanceResult.Failed;

        var role = Roles.FirstOrDefault(candidate => candidate.UniqueId == pending.RoleUniqueId);
        if (role == null || role.RoleId != pending.RoleId || !IsActivePosition(role.Position))
            return GridFightHeadPlayerEnhanceResult.Failed;

        if (pending.IsFinalHack)
            return AttachJoyContractReward(ResolveFinalSilverWolfHack(pending, optionId));

        if (optionId == GridFightSpecialFeatures.SilverWolfUpgradeToFourCostOptionId ||
            optionId == GridFightSpecialFeatures.SilverWolfUpgradeToFiveCostOptionId)
            return AttachJoyContractReward(ResolveSilverWolfCostUpgrade(pending, role, optionId));

        if (!GridFightSpecialFeatures.TryResolveSilverWolfOptionEquipmentId(optionId, out var equipmentId) ||
            !GridFightSpecialFeatures.GetSilverWolfEquipmentOptionPool(role.RoleId).Contains(optionId) ||
            !TryAddEquipment(equipmentId, out var equipment, GridFightSpecialFeatures.HeadPlayerEquipmentSource))
            return GridFightHeadPlayerEnhanceResult.Failed;

        // User-observed official rule: declining a cost-up once permanently removes future
        // cost-up choices for this run. Five-cost windows have no cost-up choice to close.
        if (role.RoleId is GridFightSpecialFeatures.SilverWolfThreeCostRoleId or
            GridFightSpecialFeatures.SilverWolfFourCostRoleId)
            _silverWolfUpgradeRouteClosed = true;

        ClearHeadPlayerPending();
        return AttachJoyContractReward(new GridFightHeadPlayerEnhanceResult(
            true,
            pending.QueuePosition,
            optionId,
            pending.EffectId,
            null,
            [],
            equipment,
            false,
            false,
            false));
    }

    private GridFightHeadPlayerEnhanceResult ResolveSilverWolfCostUpgrade(
        GridFightHeadPlayerPendingState pending,
        GridFightRoleState role,
        uint optionId)
    {
        if (_silverWolfUpgradeRouteClosed || role.Star < 2) return GridFightHeadPlayerEnhanceResult.Failed;

        var nextRoleId = (role.RoleId, optionId) switch
        {
            (GridFightSpecialFeatures.SilverWolfThreeCostRoleId,
                GridFightSpecialFeatures.SilverWolfUpgradeToFourCostOptionId) =>
                GridFightSpecialFeatures.SilverWolfFourCostRoleId,
            (GridFightSpecialFeatures.SilverWolfFourCostRoleId,
                GridFightSpecialFeatures.SilverWolfUpgradeToFiveCostOptionId) =>
                GridFightSpecialFeatures.SilverWolfFiveCostRoleId,
            _ => 0u,
        };
        if (nextRoleId == 0 || !GameData.GridFightRoleBasicInfoData.ContainsKey(nextRoleId))
            return GridFightHeadPlayerEnhanceResult.Failed;

        var removed = Roles
            .Where(candidate => candidate.UniqueId != role.UniqueId && candidate.RoleId == role.RoleId)
            .OrderBy(candidate => candidate.UniqueId)
            .ToList();
        var goldChanged = removed.Count > 0;
        foreach (var extra in removed)
        {
            Gold = (uint)Math.Min(uint.MaxValue,
                (ulong)Gold + _catalog.GetSellPrice(extra.RoleId, extra.Star));
            DestroyDiceFilledOnRole(extra);
            Roles.Remove(extra);
        }

        // Capture-confirmed cost-up semantics: mutate the same entity in place, retain position
        // and equipment, and reset the new cost tier to one star.
        role.RoleId = nextRoleId;
        role.Star = 1;
        _silverWolfCurrentRoleId = nextRoleId;
        UpgradeSilverWolfShop(nextRoleId);
        RecalculateRoleSwitches();
        ClearHeadPlayerPending();

        return new GridFightHeadPlayerEnhanceResult(
            true,
            pending.QueuePosition,
            optionId,
            pending.EffectId,
            role,
            removed.Select(candidate => candidate.UniqueId).ToList(),
            null,
            goldChanged,
            true,
            false);
    }

    private GridFightHeadPlayerEnhanceResult ResolveFinalSilverWolfHack(
        GridFightHeadPlayerPendingState pending,
        uint optionId)
    {
        if (optionId != GridFightSpecialFeatures.SilverWolfEnhanceOptionId || HackConsole != null ||
            !GridFightSpecialFeatures.TryResolveSilverWolfEquipmentId(out var equipmentId) ||
            !TryAddEquipment(equipmentId, out var equipment, GridFightSpecialFeatures.HackEquipmentSource))
            return GridFightHeadPlayerEnhanceResult.Failed;

        HackConsole = new GridFightHackConsoleState { GameItemUniqueId = _nextEntityUniqueId++ };
        ClearHeadPlayerPending();
        return new GridFightHeadPlayerEnhanceResult(
            true,
            pending.QueuePosition,
            optionId,
            pending.EffectId,
            null,
            [],
            equipment,
            false,
            false,
            true);
    }

    private void ClearHeadPlayerPending()
    {
        var finishedQueuePosition = _headPlayerPending?.QueuePosition;
        _headPlayerPending = null;
        _traitEffectSelectEnhanceQueuePosition = null;
        if (finishedQueuePosition.HasValue && PendingQueuePosition == finishedQueuePosition.Value)
            PendingQueuePosition++;
    }

    private void UpgradeSilverWolfShop(uint nextRoleId)
    {
        foreach (var slot in Shop.Where(slot =>
                     slot.SpecialGoodsId == 0 && GridFightSpecialFeatures.IsSilverWolfLv999Role(slot.RoleId)))
        {
            slot.RoleId = nextRoleId;
            slot.Price = _catalog.GetBuyPrice(nextRoleId, slot.Star);
        }
    }
}
