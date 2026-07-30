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

public sealed class GridFightSession
{
    private readonly GridFightResourceCatalog _catalog;
    private uint _nextRoleUniqueId = 1;
    private uint _nextEquipmentUniqueId = 1;
    private uint _nextOrbUniqueId = 1;
    private bool _orbClaimed;

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
    public IReadOnlyList<GridFightMonsterExcel> Enemies { get; private set; }
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
        Enemies = catalog.RollEnemyRoster(FightCampId, CurrentNode);
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

    public bool TryAddEquipment(uint equipmentId, out GridFightEquipmentState equipment)
    {
        equipment = null!;
        if (!GameData.GridFightEquipmentData.ContainsKey(equipmentId)) return false;
        equipment = new GridFightEquipmentState
        {
            UniqueId = _nextEquipmentUniqueId++,
            EquipmentId = equipmentId,
            Source = 1,
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
        Enemies = _catalog.RollEnemyRoster(FightCampId, CurrentNode);
        RefreshShop(false);
        Phase = GridFightSessionPhase.Preparing;
        return true;
    }

    public GridFightSessionChange BuyGoods(IReadOnlyList<uint> indices)
    {
        if (Phase != GridFightSessionPhase.Preparing || indices.Count == 0 || indices.Distinct().Count() != indices.Count)
            return GridFightSessionChange.Failed;
        var slots = indices.Select(index => Shop.FirstOrDefault(slot => slot.Index == index)).ToList();
        if (slots.Any(slot => slot == null || slot.SoldOut)) return GridFightSessionChange.Failed;
        var cost = slots.OfType<GridFightShopSlot>().Aggregate(0UL, (sum, slot) => sum + slot.Price);
        if (cost > Gold) return GridFightSessionChange.Failed;

        Gold -= (uint)cost;
        var added = new List<GridFightRoleState>();
        var removed = new List<uint>();
        var updated = new List<GridFightRoleState>();
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
            added.Add(role);
            MergeRoles(role.RoleId, removed, updated);
        }
        return new GridFightSessionChange(true, added, removed, updated);
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
        if (Phase != GridFightSessionPhase.Preparing ||
            Roles.Count(role => IsActivePosition(role.Position)) != MaxActiveRoles)
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
    public uint FightCampId => CurrentNode.NodeType == GridFightNodeTypeEnum.Monster ? RewardCampId : CampId;
    public uint EliteBranchId => 1;

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

    private void MergeRoles(uint roleId, ICollection<uint> removed, ICollection<GridFightRoleState> updated)
    {
        while (true)
        {
            var group = Roles.Where(role => role.RoleId == roleId)
                .GroupBy(role => role.Star)
                .OrderBy(grouping => grouping.Key)
                .FirstOrDefault(grouping => grouping.Count() >= 3);
            if (group == null) return;
            var merge = group.OrderBy(role => role.UniqueId).Take(3).ToList();
            var keeper = merge[0];
            keeper.Star++;
            foreach (var role in merge.Skip(1))
            {
                Roles.Remove(role);
                removed.Add(role.UniqueId);
            }
            updated.Add(keeper);
        }
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
        Enemies = _catalog.RollEnemyRoster(FightCampId, next);
        RefreshShop(false);
        return true;
    }

    private uint GetFreeBenchPosition()
    {
        var max = 13u + _catalog.BenchSize;
        for (var position = 14u; position <= max; position++)
            if (Roles.All(role => role.Position != position)) return position;
        return 0;
    }

    private bool IsValidPosition(uint position)
    {
        return position == 0 || IsActivePosition(position) ||
               position is >= 14 && position <= 13 + _catalog.BenchSize;
    }

    private static bool IsActivePosition(uint position) => position is >= 1 and <= 13;
}
