using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.GridFight;

namespace March7thHoney.GameServer.Game.GridFight;

public enum GridFightSessionPhase
{
    SelectingPortal,
    AwaitingInitialRound,
    AwaitingInitialOrb,
    AwaitingInitialReturn,
    Preparing,
    InBattle,
    AwaitingBattleRound,
    SelectingInvestment,
    SelectingSupply,
    SelectingEncounter,
    AwaitingSupplyRound,
    AwaitingBattleReturn,
    AwaitingBackToPrepare,
}

public enum GridFightPendingRequestKind
{
    PortalReroll,
    PortalSelect,
    RoundBegin,
    ReturnPreparation,
}

public sealed record GridFightSupplyOfferState(uint RoleId, uint EquipmentId);

public sealed record GridFightSupplySelectionResult(
    bool Success,
    GridFightRoleAcquisitionResult Acquisition,
    GridFightEquipmentState? Equipment)
{
    public static GridFightSupplySelectionResult Failed { get; } =
        new(false, GridFightRoleAcquisitionResult.Failed, null);
}

public sealed class GridFightRoleState
{
    public uint UniqueId { get; init; }
    public uint RoleId { get; init; }
    public uint Star { get; set; } = 1;
    public uint Position { get; set; }
}

public sealed class GridFightEquipmentState
{
    public uint UniqueId { get; init; }
    public uint EquipmentId { get; init; }
    public uint Source { get; init; }
}

public sealed class GridFightOrbState
{
    public uint UniqueId { get; init; }
    public uint OrbItemId { get; init; }
}

public sealed class GridFightConsumableState
{
    public uint ItemId { get; init; }
    public uint Count { get; set; }
}

public sealed class GridFightShopSlot
{
    public uint Index { get; init; }
    public uint RoleId { get; init; }
    public uint Star { get; init; } = 1;
    public uint Price { get; init; }
    public bool SoldOut { get; set; }
}

public sealed record GridFightSessionChange(
    bool Success,
    IReadOnlyList<GridFightRoleState> AddedRoles,
    IReadOnlyList<uint> RemovedRoleIds,
    IReadOnlyList<GridFightRoleState> UpdatedRoles)
{
    public static GridFightSessionChange Failed { get; } = new(false, [], [], []);
    public static GridFightSessionChange Succeeded { get; } = new(true, [], [], []);
}

public sealed record GridFightRoleMergeStep(
    IReadOnlyList<uint> RemovedRoleIds,
    GridFightRoleState AddedRole);

public sealed record GridFightRoleAcquisitionResult(
    bool Success,
    IReadOnlyList<GridFightRoleState> AcquiredRoles,
    IReadOnlyList<GridFightRoleMergeStep> MergeSteps,
    IReadOnlyList<GridFightRoleState> FinalMergedRoles)
{
    public static GridFightRoleAcquisitionResult Failed { get; } = new(false, [], [], []);
}

public sealed class GridFightSession
{
    private const int InvestmentOfferCount = 3;
    private const int SupplyOfferCount = 5;
    private const uint SupplySelectionQueueAdvance = 4;
    private const uint SupplyEquipmentSource = 9;

    private readonly GridFightResourceCatalog _catalog;
    private uint _nextRoleUniqueId = 1;
    private uint _nextEquipmentUniqueId = 1;
    private uint _nextOrbUniqueId = 1;
    private bool _orbClaimed;
    private readonly HashSet<uint> _rerolledInvestmentIds = [];
    private uint _encounterRerollCount;

    public uint Season { get; }
    public uint DivisionId { get; }
    public bool IsOverlock { get; }
    public uint UniqueId { get; }
    public GridFightSessionPhase Phase { get; private set; } = GridFightSessionPhase.SelectingPortal;
    public uint PendingQueuePosition { get; private set; } = 1;
    public uint ChapterId { get; private set; } = 1;
    public uint SectionId { get; private set; } = 1;
    public uint Gold { get; private set; }
    public uint PlayerLevel { get; private set; }
    public uint PlayerExp { get; private set; }
    public uint LineupHp { get; private set; }
    public uint MaxLineupHp { get; }
    public uint KeepWinCount { get; private set; }
    public uint BattlesFinished { get; private set; }
    public uint SelectedPortalId { get; private set; }
    public IReadOnlyList<uint> CampIds { get; }
    public uint RewardCampId { get; }
    public IReadOnlyList<GridFightMonsterExcel> Bosses { get; }
    public IReadOnlyList<uint> AffixIds { get; }
    public IReadOnlyList<uint> PortalOffer { get; private set; }
    public IReadOnlyList<uint> InvestmentOffer { get; private set; } = [];
    public IReadOnlyList<GridFightSupplyOfferState> SupplyOffer { get; private set; } = [];
    public List<uint> ActiveInvestmentIds { get; } = [];
    public uint SupplyRerollCount { get; private set; }
    public GridFightBattlePlan? BattlePlan { get; private set; }
    public IReadOnlyList<GridFightEncounterOptionState> EncounterOptions { get; private set; } = [];
    public IReadOnlyList<GridFightMonsterPlan> Enemies => BattlePlan?.Monsters ?? [];
    public List<GridFightRoleState> Roles { get; } = [];
    public List<GridFightEquipmentState> Equipments { get; } = [];
    public List<GridFightOrbState> Orbs { get; } = [];
    public List<GridFightConsumableState> Consumables { get; } = [];
    public List<GridFightShopSlot> Shop { get; } = [];

    public GridFightSession(
        GridFightResourceCatalog catalog,
        uint season,
        uint divisionId,
        bool isOverlock,
        uint uniqueId)
    {
        _catalog = catalog;
        Season = season;
        DivisionId = divisionId;
        IsOverlock = isOverlock;
        UniqueId = uniqueId;
        PlayerLevel = catalog.InitialPlayerLevel;
        LineupHp = catalog.InitialHp;
        MaxLineupHp = catalog.InitialHp;
        PortalOffer = catalog.RollPortalOffer(3);
        var chapterCount = checked((int)catalog.StandardRoute.Max(node => node.ChapterId));
        CampIds = catalog.RollCampIds(chapterCount);
        RewardCampId = catalog.RollRewardCampId();
        Bosses = CampIds.Select(catalog.RollBossMonster).ToList();
        AffixIds = catalog.RollAffixIds(divisionId);
        PrepareCurrentNode();
    }

    public uint MaxActiveRoles => _catalog.GetPlayerLevel(PlayerLevel).AvatarMaxNumber;

    public bool HandlePending(uint queuePosition, GridFightPendingRequestKind kind, uint portalId = 0)
    {
        if (queuePosition != PendingQueuePosition) return false;
        switch (kind)
        {
            case GridFightPendingRequestKind.PortalReroll
                when Phase == GridFightSessionPhase.SelectingPortal:
                PortalOffer = _catalog.RollPortalOffer(3);
                return true;
            case GridFightPendingRequestKind.PortalSelect
                when Phase == GridFightSessionPhase.SelectingPortal && PortalOffer.Contains(portalId):
                SelectedPortalId = portalId;
                MaterializeInitialRoles();
                Gold += _catalog.InitialGold;
                Phase = GridFightSessionPhase.AwaitingInitialRound;
                PendingQueuePosition++;
                return true;
            case GridFightPendingRequestKind.RoundBegin
                when Phase == GridFightSessionPhase.AwaitingInitialRound:
                RefreshShop(false);
                Phase = GridFightSessionPhase.AwaitingInitialOrb;
                PendingQueuePosition = 10;
                return true;
            case GridFightPendingRequestKind.ReturnPreparation
                when Phase is GridFightSessionPhase.AwaitingInitialOrb or GridFightSessionPhase.AwaitingInitialReturn:
                Phase = GridFightSessionPhase.Preparing;
                PendingQueuePosition++;
                return true;
            case GridFightPendingRequestKind.RoundBegin
                when Phase == GridFightSessionPhase.AwaitingBattleRound:
                PendingQueuePosition++;
                if (CurrentNode.NodeType == GridFightNodeTypeEnum.Supply)
                {
                    PrepareSupplyOffer();
                    Phase = GridFightSessionPhase.SelectingSupply;
                }
                else if (CurrentNode.NodeType == GridFightNodeTypeEnum.EliteBranch)
                {
                    Phase = GridFightSessionPhase.SelectingEncounter;
                }
                else if (CurrentNode.Template.IsAugment != 0)
                {
                    PrepareInvestmentOffer();
                    Phase = GridFightSessionPhase.SelectingInvestment;
                }
                else
                {
                    Phase = GridFightSessionPhase.AwaitingBattleReturn;
                }
                return true;
            case GridFightPendingRequestKind.RoundBegin
                when Phase == GridFightSessionPhase.AwaitingSupplyRound:
                Phase = GridFightSessionPhase.AwaitingBattleReturn;
                PendingQueuePosition++;
                return true;
            case GridFightPendingRequestKind.ReturnPreparation
                when Phase == GridFightSessionPhase.AwaitingBattleReturn:
                Phase = GridFightSessionPhase.AwaitingBackToPrepare;
                return true;
            default:
                return false;
        }
    }

    public bool IsInvestmentRerolled(uint augmentId) => _rerolledInvestmentIds.Contains(augmentId);

    public bool RerollInvestment(uint queuePosition, uint augmentId)
    {
        if (Phase != GridFightSessionPhase.SelectingInvestment ||
            queuePosition != PendingQueuePosition ||
            _rerolledInvestmentIds.Contains(augmentId))
            return false;
        var index = InvestmentOffer.ToList().IndexOf(augmentId);
        if (index < 0) return false;
        var quality = GameData.GridFightAugmentData[augmentId].Quality;
        var excluded = InvestmentOffer.Concat(ActiveInvestmentIds).ToHashSet();
        var replacement = _catalog.RollInvestmentOffer(quality, 1, excluded).SingleOrDefault();
        if (replacement == 0) return false;
        var offer = InvestmentOffer.ToList();
        offer[index] = replacement;
        InvestmentOffer = offer;
        _rerolledInvestmentIds.Add(replacement);
        return true;
    }

    public bool SelectInvestment(uint queuePosition, uint augmentId)
    {
        if (Phase != GridFightSessionPhase.SelectingInvestment ||
            queuePosition != PendingQueuePosition ||
            !InvestmentOffer.Contains(augmentId))
            return false;
        if (!ActiveInvestmentIds.Contains(augmentId)) ActiveInvestmentIds.Add(augmentId);
        InvestmentOffer = [];
        _rerolledInvestmentIds.Clear();
        Phase = GridFightSessionPhase.AwaitingBattleReturn;
        PendingQueuePosition++;
        return true;
    }

    public bool RerollSupply(uint queuePosition)
    {
        if (Phase != GridFightSessionPhase.SelectingSupply ||
            queuePosition != PendingQueuePosition ||
            SupplyRerollCount != 0)
            return false;
        SupplyOffer = _catalog.RollSupplyOffer(SupplyOfferCount)
            .Select(offer => new GridFightSupplyOfferState(offer.RoleId, offer.EquipmentId))
            .ToList();
        SupplyRerollCount = 1;
        return true;
    }

    public bool RerollEncounter(uint queuePosition)
    {
        if (Phase != GridFightSessionPhase.SelectingEncounter ||
            queuePosition != PendingQueuePosition ||
            _encounterRerollCount != 0)
            return false;
        EncounterOptions = _catalog.RollEncounterOptions(CurrentNode, CampIds, DivisionId);
        _encounterRerollCount = 1;
        return EncounterOptions.Count == 2;
    }

    public bool SelectEncounter(uint queuePosition, uint eliteBranchId)
    {
        if (Phase != GridFightSessionPhase.SelectingEncounter ||
            queuePosition != PendingQueuePosition)
            return false;
        var selected = EncounterOptions.FirstOrDefault(option => option.EliteBranchId == eliteBranchId);
        if (selected == null) return false;
        BattlePlan = selected.BattlePlan;
        Phase = GridFightSessionPhase.AwaitingBattleReturn;
        PendingQueuePosition++;
        return true;
    }

    public bool UpdateEncounterSelection(uint eliteBranchId)
    {
        if (CurrentNode.NodeType != GridFightNodeTypeEnum.EliteBranch ||
            BattlePlan == null ||
            Phase is not (GridFightSessionPhase.AwaitingBattleReturn or
                GridFightSessionPhase.AwaitingBackToPrepare or
                GridFightSessionPhase.Preparing))
            return false;
        var selected = EncounterOptions.FirstOrDefault(option => option.EliteBranchId == eliteBranchId);
        if (selected == null) return false;
        BattlePlan = selected.BattlePlan;
        return true;
    }

    public GridFightSupplySelectionResult SelectSupply(uint queuePosition, IReadOnlyList<uint> indices)
    {
        if (Phase != GridFightSessionPhase.SelectingSupply ||
            queuePosition != PendingQueuePosition ||
            indices.Count != 1 ||
            indices[0] == 0 ||
            indices[0] > SupplyOffer.Count ||
            _catalog.GetNextNode(ChapterId, SectionId) == null)
            return GridFightSupplySelectionResult.Failed;

        var selected = SupplyOffer[checked((int)indices[0] - 1)];
        if (!TryAddEquipment(selected.EquipmentId, out var equipment, SupplyEquipmentSource))
            return GridFightSupplySelectionResult.Failed;

        var role = new GridFightRoleState
        {
            UniqueId = _nextRoleUniqueId++,
            RoleId = selected.RoleId,
            Position = GetFreeBenchPosition(),
        };
        Roles.Add(role);
        var mergeSteps = MergeRoles(role.RoleId);
        var acquisition = new GridFightRoleAcquisitionResult(
            true,
            [role],
            mergeSteps,
            GetFinalMergedRoles(mergeSteps));

        SupplyOffer = [];
        SupplyRerollCount = 0;
        if (!AdvanceToNextNode()) return GridFightSupplySelectionResult.Failed;
        Phase = GridFightSessionPhase.AwaitingSupplyRound;
        PendingQueuePosition += SupplySelectionQueueAdvance;
        return new GridFightSupplySelectionResult(
            true,
            acquisition,
            equipment);
    }

    public bool ClaimInitialOrb()
    {
        if (_orbClaimed) return true;
        if (Phase != GridFightSessionPhase.AwaitingInitialOrb) return false;
        _orbClaimed = true;
        Phase = GridFightSessionPhase.AwaitingInitialReturn;
        return true;
    }

    public uint AdjustGold(int delta)
    {
        if (delta >= 0)
        {
            Gold = (uint)Math.Min(uint.MaxValue, (ulong)Gold + (uint)delta);
            return Gold;
        }

        var amount = (ulong)(-(long)delta);
        Gold = amount >= Gold ? 0 : Gold - (uint)amount;
        return Gold;
    }

    public bool TryAddRole(uint roleId, uint star, out GridFightSessionChange change)
    {
        change = GridFightSessionChange.Failed;
        if (!GameData.GridFightRoleBasicInfoData.ContainsKey(roleId) ||
            !GameData.GridFightRoleStarData.ContainsKey(roleId << 4 | star))
            return false;

        var role = new GridFightRoleState
        {
            UniqueId = _nextRoleUniqueId++,
            RoleId = roleId,
            Star = star,
            Position = GetFreeBenchPosition(),
        };
        Roles.Add(role);
        change = new GridFightSessionChange(true, [role], [], []);
        return true;
    }

    public bool TryAddEquipment(uint equipmentId, out GridFightEquipmentState equipment, uint source = 1)
    {
        equipment = null!;
        if (!GameData.GridFightEquipmentData.ContainsKey(equipmentId)) return false;
        equipment = new GridFightEquipmentState
        {
            UniqueId = _nextEquipmentUniqueId++,
            EquipmentId = equipmentId,
            Source = source,
        };
        Equipments.Add(equipment);
        return true;
    }

    public bool TryAddOrb(uint orbItemId, out GridFightOrbState orb)
    {
        orb = null!;
        if (!GameData.GridFightOrbData.ContainsKey(orbItemId)) return false;
        orb = new GridFightOrbState
        {
            UniqueId = _nextOrbUniqueId++,
            OrbItemId = orbItemId,
        };
        Orbs.Add(orb);
        return true;
    }

    public bool TryAddConsumable(
        uint itemId,
        out GridFightConsumableState consumable,
        out bool isNew)
    {
        consumable = null!;
        isNew = false;
        if (!GameData.GridFightConsumablesData.ContainsKey(itemId)) return false;

        consumable = Consumables.FirstOrDefault(item => item.ItemId == itemId)!;
        if (consumable == null)
        {
            consumable = new GridFightConsumableState { ItemId = itemId, Count = 1 };
            Consumables.Add(consumable);
            isNew = true;
            return true;
        }

        if (consumable.Count < uint.MaxValue) consumable.Count++;
        return true;
    }

    public bool TryMoveToSection(uint chapterId, uint sectionId, out uint finishedPendingPosition)
    {
        finishedPendingPosition = 0;
        if (Phase == GridFightSessionPhase.InBattle ||
            !_catalog.StandardRoute.Any(node => node.ChapterId == chapterId && node.SectionId == sectionId))
            return false;

        if (Phase is GridFightSessionPhase.SelectingPortal or
            GridFightSessionPhase.AwaitingInitialRound or
            GridFightSessionPhase.AwaitingInitialOrb or
            GridFightSessionPhase.AwaitingInitialReturn or
            GridFightSessionPhase.AwaitingBattleRound or
            GridFightSessionPhase.AwaitingBattleReturn)
            finishedPendingPosition = PendingQueuePosition;

        ChapterId = chapterId;
        SectionId = sectionId;
        InvestmentOffer = [];
        SupplyOffer = [];
        _rerolledInvestmentIds.Clear();
        SupplyRerollCount = 0;
        PrepareCurrentNode();
        RefreshShop(false);
        Phase = GridFightSessionPhase.Preparing;
        return true;
    }

    public GridFightRoleAcquisitionResult BuyGoods(IReadOnlyList<uint> indices)
    {
        if (Phase != GridFightSessionPhase.Preparing || indices.Count == 0 || indices.Distinct().Count() != indices.Count)
            return GridFightRoleAcquisitionResult.Failed;
        var slots = indices.Select(index => Shop.FirstOrDefault(slot => slot.Index == index)).ToList();
        if (slots.Any(slot => slot == null || slot.SoldOut)) return GridFightRoleAcquisitionResult.Failed;
        var cost = slots.OfType<GridFightShopSlot>().Aggregate(0UL, (sum, slot) => sum + slot.Price);
        if (cost > Gold) return GridFightRoleAcquisitionResult.Failed;

        Gold -= (uint)cost;
        var acquiredRoles = new List<GridFightRoleState>();
        var mergeSteps = new List<GridFightRoleMergeStep>();
        foreach (var slot in slots.OfType<GridFightShopSlot>())
        {
            slot.SoldOut = true;
            var role = new GridFightRoleState
            {
                UniqueId = _nextRoleUniqueId++,
                RoleId = slot.RoleId,
                Star = slot.Star,
                Position = GetFreeBenchPosition(),
            };
            Roles.Add(role);
            acquiredRoles.Add(role);
            mergeSteps.AddRange(MergeRoles(role.RoleId));
        }
        return new GridFightRoleAcquisitionResult(
            true,
            acquiredRoles,
            mergeSteps,
            GetFinalMergedRoles(mergeSteps));
    }

    public GridFightSessionChange RecycleRole(uint uniqueId)
    {
        if (Phase != GridFightSessionPhase.Preparing) return GridFightSessionChange.Failed;
        var role = Roles.FirstOrDefault(candidate => candidate.UniqueId == uniqueId);
        if (role == null) return GridFightSessionChange.Failed;
        Gold += _catalog.GetSellPrice(role.RoleId, role.Star);
        Roles.Remove(role);
        return new GridFightSessionChange(true, [], [uniqueId], []);
    }

    public bool BuyExp()
    {
        if (Phase != GridFightSessionPhase.Preparing || Gold < _catalog.BuyExpCost) return false;
        var current = _catalog.GetPlayerLevel(PlayerLevel);
        if (current.LevelUpExp == 0) return false;

        Gold -= _catalog.BuyExpCost;
        AddPlayerExp(_catalog.BuyExpAmount);
        return true;
    }

    private void AddPlayerExp(uint amount)
    {
        PlayerExp += amount;
        while (GameDataHasNextLevel() && PlayerExp >= _catalog.GetPlayerLevel(PlayerLevel).LevelUpExp)
        {
            PlayerExp -= _catalog.GetPlayerLevel(PlayerLevel).LevelUpExp;
            PlayerLevel++;
        }
    }

    public bool UpdatePositions(IReadOnlyList<(uint UniqueId, uint Position)> updates)
    {
        if (Phase != GridFightSessionPhase.Preparing || updates.Count == 0 ||
            updates.Select(update => update.UniqueId).Distinct().Count() != updates.Count)
            return false;
        if (updates.Any(update => Roles.All(role => role.UniqueId != update.UniqueId) || !IsValidPosition(update.Position)))
            return false;

        var positions = Roles.ToDictionary(role => role.UniqueId, role => role.Position);
        foreach (var update in updates) positions[update.UniqueId] = update.Position;
        var occupied = positions.Values.Where(position => position != 0).ToList();
        if (occupied.Distinct().Count() != occupied.Count) return false;
        if (positions.Values.Count(IsActivePosition) > MaxActiveRoles) return false;

        foreach (var update in updates)
            Roles.Single(role => role.UniqueId == update.UniqueId).Position = update.Position;
        return true;
    }

    public bool EnterBattle()
    {
        var activeRoleCount = Roles.Count(role => IsActivePosition(role.Position));
        if (Phase != GridFightSessionPhase.Preparing ||
            !IsCombatNode(CurrentNode) ||
            BattlePlan == null ||
            activeRoleCount == 0 ||
            activeRoleCount > MaxActiveRoles)
            return false;
        Phase = GridFightSessionPhase.InBattle;
        return true;
    }

    public bool ResolveBattle(bool win)
    {
        if (Phase != GridFightSessionPhase.InBattle) return false;
        if (!win)
        {
            BattlesFinished++;
            KeepWinCount = 0;
            Phase = GridFightSessionPhase.Preparing;
            return true;
        }

        Gold += CurrentNode.BasicGoldReward;
        AddPlayerExp(_catalog.BattleWinExp);
        if (!AdvanceToNextNode()) return false;
        BattlesFinished++;
        KeepWinCount++;
        Phase = GridFightSessionPhase.AwaitingBattleRound;
        PendingQueuePosition = 19;
        return true;
    }

    public void AbortBattleStart()
    {
        if (Phase == GridFightSessionPhase.InBattle) Phase = GridFightSessionPhase.Preparing;
    }

    public bool BackToPrepare()
    {
        if (Phase != GridFightSessionPhase.AwaitingBackToPrepare) return false;
        Phase = GridFightSessionPhase.Preparing;
        return true;
    }

    public bool RefreshShop(bool chargeGold)
    {
        if (chargeGold && (Phase != GridFightSessionPhase.Preparing || Gold < _catalog.RefreshCost)) return false;
        if (chargeGold) Gold -= _catalog.RefreshCost;
        Shop.Clear();
        var roles = _catalog.RollShop(PlayerLevel);
        for (var index = 0; index < roles.Count; index++)
            Shop.Add(new GridFightShopSlot
            {
                Index = (uint)index,
                RoleId = roles[index].ID,
                Price = _catalog.GetBuyPrice(roles[index].ID),
            });
        return true;
    }

    public GridFightRouteNode CurrentNode => _catalog.GetNode(ChapterId, SectionId);
    public uint CampId => CampIds[checked((int)ChapterId - 1)];
    public uint FightCampId => BattlePlan?.CampId ??
                               (CurrentNode.NodeType == GridFightNodeTypeEnum.Monster ? RewardCampId : 0);
    public uint EliteBranchId => EncounterOptions.FirstOrDefault(option => option.BattlePlan == BattlePlan)?.EliteBranchId ?? 0;
    public uint EncounterRerollCount => _encounterRerollCount;
    public uint CurrentPenaltyRuleId => BattlePlan?.PenaltyRuleId ?? CurrentNode.PenaltyBonusRuleId;

    public uint EffectiveEnemyDifficultyLevel
    {
        get
        {
            var value = (ulong)_catalog.GetEnemyDifficultyLevel(DivisionId) +
                        ActiveInvestmentIds.Aggregate(0UL,
                            (sum, id) => sum + _catalog.GetInvestmentDifficultyAdd(DivisionId, id)) +
                        (BattlePlan?.DifficultyAdd ?? 0);
            return (uint)Math.Min(value, uint.MaxValue);
        }
    }

    private void PrepareInvestmentOffer()
    {
        var quality = _catalog.RollInvestmentQuality();
        InvestmentOffer = _catalog.RollInvestmentOffer(quality, InvestmentOfferCount, ActiveInvestmentIds);
        _rerolledInvestmentIds.Clear();
    }

    private void PrepareSupplyOffer()
    {
        SupplyOffer = _catalog.RollSupplyOffer(SupplyOfferCount)
            .Select(offer => new GridFightSupplyOfferState(offer.RoleId, offer.EquipmentId))
            .ToList();
        SupplyRerollCount = 0;
    }

    private void MaterializeInitialRoles()
    {
        if (Roles.Count > 0) return;
        var roles = _catalog.RollInitialRoles(checked((int)_catalog.InitialRoleCount));
        for (var index = 0; index < roles.Count; index++)
            Roles.Add(new GridFightRoleState
            {
                UniqueId = _nextRoleUniqueId++,
                RoleId = roles[index].ID,
                Position = 14u + (uint)index,
            });
    }

    private List<GridFightRoleMergeStep> MergeRoles(uint roleId)
    {
        var steps = new List<GridFightRoleMergeStep>();
        while (true)
        {
            var group = Roles.Where(role => role.RoleId == roleId)
                .GroupBy(role => role.Star)
                .OrderBy(grouping => grouping.Key)
                .FirstOrDefault(grouping => grouping.Count() >= 3 &&
                    GameData.GridFightRoleStarData.ContainsKey(roleId << 4 | (grouping.Key + 1)));
            if (group == null) return steps;
            var merge = group.OrderBy(role => role.UniqueId).Take(3).ToList();
            foreach (var role in merge) Roles.Remove(role);
            var mergedRole = new GridFightRoleState
            {
                UniqueId = _nextRoleUniqueId++,
                RoleId = roleId,
                Star = group.Key + 1,
                Position = merge[0].Position,
            };
            Roles.Add(mergedRole);
            steps.Add(new GridFightRoleMergeStep(
                merge.Select(role => role.UniqueId).ToList(),
                mergedRole));
        }
    }

    private IReadOnlyList<GridFightRoleState> GetFinalMergedRoles(
        IEnumerable<GridFightRoleMergeStep> mergeSteps)
    {
        return mergeSteps
            .Select(step => step.AddedRole)
            .Where(role => Roles.Contains(role))
            .ToList();
    }

    private bool GameDataHasNextLevel()
    {
        try
        {
            _ = _catalog.GetPlayerLevel(PlayerLevel + 1);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    private bool AdvanceToNextNode()
    {
        var next = _catalog.GetNextNode(ChapterId, SectionId);
        if (next == null) return false;
        ChapterId = next.ChapterId;
        SectionId = next.SectionId;
        PrepareCurrentNode();
        RefreshShop(false);
        return true;
    }

    private void PrepareCurrentNode()
    {
        BattlePlan = null;
        EncounterOptions = [];
        _encounterRerollCount = 0;
        if (CurrentNode.NodeType == GridFightNodeTypeEnum.EliteBranch)
        {
            EncounterOptions = _catalog.RollEncounterOptions(CurrentNode, CampIds, DivisionId);
            return;
        }
        BattlePlan = _catalog.RollBattlePlan(CurrentNode, CampIds, RewardCampId, Bosses);
    }

    private uint GetFreeBenchPosition()
    {
        var max = 13u + _catalog.BenchSize + _catalog.BenchOverflowSize;
        for (var position = 14u; position <= max; position++)
            if (Roles.All(role => role.Position != position)) return position;
        return 0;
    }

    private bool IsValidPosition(uint position)
    {
        return position == 0 || IsActivePosition(position) ||
               position is >= 14 && position <= 13 + _catalog.BenchSize + _catalog.BenchOverflowSize;
    }

    private static bool IsActivePosition(uint position) => position is >= 1 and <= 13;

    private static bool IsCombatNode(GridFightRouteNode node)
    {
        return node.NodeType is GridFightNodeTypeEnum.Monster or
            GridFightNodeTypeEnum.CampMonster or
            GridFightNodeTypeEnum.EliteBranch or
            GridFightNodeTypeEnum.Boss;
    }
}
