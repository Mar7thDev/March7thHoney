namespace March7thHoney.GameServer.Game.GridFight;

public sealed class GridFightSpotLightState
{
    public uint UniqueId { get; init; }
    public uint SpotLightId { get; set; }
    public uint RoleId { get; set; }
    public uint RoleUniqueId { get; set; }
    public bool IsLoss { get; set; }
}

/// <summary>Spotlight changes the client has to be told about (SyncData 1433 add / 1651 update / 166 remove).</summary>
public sealed class GridFightSpotLightChanges
{
    public List<GridFightSpotLightState> Added { get; } = [];
    public List<GridFightSpotLightState> Updated { get; } = [];
    public List<GridFightSpotLightState> Removed { get; } = [];
    public bool CountersChanged { get; set; }

    /// <summary>Spotlight nodes whose route param 4 changed (SyncData 1779 route node update).</summary>
    public List<(uint ChapterId, uint SectionId)> RouteNodes { get; } = [];
}

/// <summary>
///     主角光环 (SpotLight). Layout and flow: artifacts/gf-season2/re-g6-spotlight.md. The client marks spotlight nodes
///     only through route param 4; which nodes the official server flags, the candidate weighting and the reroll price are
///     not in the client and follow the spec's guesses (1-2 and 2-1, two candidates, 2 gold, 2 rerolls). Per-spotlight
///     lobby/battle effects live in server JSON that is not shipped; the bound role gets the crown in battle and
///     trait-granting spotlights count their role for the Spotlight trait.
/// </summary>
public sealed partial class GridFightSession
{
    private const int SpotLightCandidateCount = 2;
    private const uint SpotLightRerollCost = 2;
    private const uint SpotLightRerollsPerSelection = 2;
    private const int SpotLightNormalSlots = 2;
    private const uint SpotLightConsumableId = 350111;
    private static readonly (uint ChapterId, uint SectionId)[] SpotLightNodes = [(1, 2), (2, 1)];

    /// <summary>Spotlights that make their role a member of a Spotlight-type trait (matched by name/description).</summary>
    private static readonly Dictionary<uint, uint> SpotLightTraits = new()
    {
        [100800] = 400102,
        [110801] = 400202,
        [100801] = 400302,
        [120902] = 400402,
    };

    private readonly List<GridFightSpotLightState> _spotLights = [];
    private readonly HashSet<(uint ChapterId, uint SectionId)> _spotLightNodesVisited = [];
    private readonly HashSet<(uint ChapterId, uint SectionId)> _spotLightNodesDone = [];
    private readonly List<uint> _spotLightCandidates = [];
    private readonly Dictionary<uint, uint> _spotLightTraitItemIds = [];
    private uint _spotLightRerollsLeft;
    private uint _spotLightTargetUniqueId;
    private uint _spotLightGrantRoleId;
    private uint _spotLightReplaceTotal;
    private uint _spotLightReplaceUsed;
    private uint _spotLightFreeRefreshTotal;
    private uint _spotLightFreeRefreshUsed;
    private int _spotLightPendingSelections;

    public IReadOnlyList<GridFightSpotLightState> SpotLights => _spotLights;
    public IReadOnlyList<uint> SpotLightCandidates => _spotLightCandidates;
    public uint SpotLightRerollsLeft => _spotLightRerollsLeft;
    public uint SpotLightTargetUniqueId => _spotLightTargetUniqueId;
    public uint SpotLightGrantRoleId => _spotLightGrantRoleId;
    public uint SpotLightReplaceTotal => _spotLightReplaceTotal;
    public uint SpotLightReplaceUsed => _spotLightReplaceUsed;
    public uint SpotLightFreeRefreshTotal => _spotLightFreeRefreshTotal;
    public uint SpotLightFreeRefreshUsed => _spotLightFreeRefreshUsed;
    public uint SpotLightRerollCostNow => _spotLightFreeRefreshUsed < _spotLightFreeRefreshTotal ? 0 : SpotLightRerollCost;

    public bool IsSpotLightNode(uint chapterId, uint sectionId) =>
        IsSeasonTwoRules && SpotLightNodes.Contains((chapterId, sectionId));

    /// <summary>
    ///     Route param 4 (HasSpotLight) while the node's selection is still due. The S2 preparation panel's battle button
    ///     sends GridFightSpotLightEnterCsReq (8508) instead of starting the battle while the current node has it
    ///     (GridFightPreparationS2SeasonPanel.try_handle_start_battle), so it must be cleared once the node is done.
    /// </summary>
    public bool IsSpotLightNodePending(uint chapterId, uint sectionId) =>
        IsSpotLightNode(chapterId, sectionId) && !_spotLightNodesDone.Contains((chapterId, sectionId));

    /// <summary>The current spotlight node is done once no selection for it is queued or open.</summary>
    private void ResolveSpotLightNode(GridFightSpotLightChanges changes)
    {
        if (!IsSpotLightNode(ChapterId, SectionId) || _spotLightPendingSelections > 0 || IsSpotLightSelecting) return;
        if (_spotLightNodesDone.Add((ChapterId, SectionId))) changes.RouteNodes.Add((ChapterId, SectionId));
    }

    /// <summary>8508 without an open selection: settle the current node so the battle button starts the battle.</summary>
    public GridFightSpotLightChanges? ResolveStaleSpotLightNode()
    {
        if (Phase != GridFightSessionPhase.Preparing || !IsSpotLightNodePending(ChapterId, SectionId)) return null;
        var changes = new GridFightSpotLightChanges();
        ResolveSpotLightNode(changes);
        return changes.RouteNodes.Count == 0 ? null : changes;
    }

    public uint GetSpotLightIdForRole(uint roleUniqueId) =>
        _spotLights.FirstOrDefault(entry => entry.RoleUniqueId == roleUniqueId && !entry.IsLoss)?.SpotLightId ?? 0;

    /// <summary>Spotlight enemy difficulty (EnemyDiffLvAdd of every owned spotlight).</summary>
    public uint SpotLightEnemyDifficulty => (uint)Math.Max(0, _spotLights.Where(entry => !entry.IsLoss)
        .Sum(entry => GridFightSeasonTwoTables.SpotLights.FirstOrDefault(row => row.Id == entry.SpotLightId)?.EnemyDiffLvAdd ?? 0));

    /// <summary>(trait id, role config ids) granted by owned spotlights, with a stable game item id per spotlight.</summary>
    public IReadOnlyList<(uint ItemUniqueId, uint SpotLightId, uint TraitId, uint RoleId)> GetSpotLightTraitGrants()
    {
        var result = new List<(uint, uint, uint, uint)>();
        foreach (var entry in _spotLights.Where(entry => !entry.IsLoss && SpotLightTraits.ContainsKey(entry.SpotLightId)))
        {
            if (!_spotLightTraitItemIds.TryGetValue(entry.UniqueId, out var itemId))
                _spotLightTraitItemIds[entry.UniqueId] = itemId = _nextEntityUniqueId++;
            result.Add((itemId, entry.SpotLightId, SpotLightTraits[entry.SpotLightId], entry.RoleId));
        }
        return result;
    }

    private void AddSpotLightGrantedTraitIds(GridFightRoleState role, HashSet<uint> traits)
    {
        foreach (var entry in _spotLights)
            if (!entry.IsLoss && entry.RoleUniqueId == role.UniqueId &&
                SpotLightTraits.TryGetValue(entry.SpotLightId, out var traitId))
                traits.Add(traitId);
    }

    /// <summary>On entering a spotlight node, queue a selection while a normal slot is free.</summary>
    private void RefreshSpotLight(GridFightSpotLightChanges spotChanges)
    {
        foreach (var entry in _spotLights)
        {
            var lost = Roles.All(role => role.UniqueId != entry.RoleUniqueId);
            if (lost == entry.IsLoss) continue;
            entry.IsLoss = lost;
            spotChanges.Updated.Add(entry);
        }
        if (!IsSpotLightNode(ChapterId, SectionId) || !_spotLightNodesVisited.Add((ChapterId, SectionId))) return;
        var unlocked = SpotLightNodes.Count(node => _spotLightNodesVisited.Contains(node));
        if (_spotLights.Count(entry => !entry.IsLoss) + _spotLightPendingSelections < Math.Min(unlocked, SpotLightNormalSlots))
            _spotLightPendingSelections++;
        else
            ResolveSpotLightNode(spotChanges);
    }

    private bool TryStartSpotLightPending(GridFightSeasonTwoChanges changes)
    {
        if (_spotLightPendingSelections == 0) return false;
        _spotLightPendingSelections--;
        BeginSpotLightSelection(0, 0);
        StartSeasonTwoPending(GridFightSeasonTwoPendingKind.SpotLightSelect, 0, 0, [], changes);
        return true;
    }

    private void BeginSpotLightSelection(uint replaceUniqueId, uint grantRoleId)
    {
        _spotLightTargetUniqueId = replaceUniqueId;
        _spotLightGrantRoleId = grantRoleId;
        _spotLightRerollsLeft = SpotLightRerollsPerSelection;
        _spotLightCandidates.Clear();
        while (_spotLightCandidates.Count < SpotLightCandidateCount && RollSpotLight() is var id and > 0)
            _spotLightCandidates.Add(id);
    }

    /// <summary>Weighted toward spotlights whose SuitableAvatarList has a fielded role (guess); overclock keeps OC rows.</summary>
    private uint RollSpotLight()
    {
        var owned = _spotLights.Select(entry => entry.SpotLightId).Concat(_spotLightCandidates).ToHashSet();
        var board = Roles.Where(role => IsActivePosition(role.Position)).Select(role => role.RoleId).ToHashSet();
        var pool = GridFightSeasonTwoTables.SpotLights
            .Where(row => !owned.Contains(row.Id) && (!IsOverlock || row.IsOcEffective))
            .Select(row => (row.Id, Weight: row.SuitableAvatars.Any(board.Contains) ? 3 : 1))
            .ToList();
        if (pool.Count == 0) return 0;
        var roll = Random.Shared.Next(pool.Sum(entry => entry.Weight));
        foreach (var (id, weight) in pool)
        {
            roll -= weight;
            if (roll < 0) return id;
        }
        return pool[^1].Id;
    }

    /// <summary>HandlePendingAction 263 { spotlight id, role config id }.</summary>
    public GridFightSpotLightChanges? SelectSpotLight(uint queuePosition, uint spotLightId, uint roleId, out uint finished)
    {
        finished = 0;
        if (queuePosition == 0 || !MatchesSeasonTwoPending(GridFightSeasonTwoPendingKind.SpotLightSelect, queuePosition) ||
            !_spotLightCandidates.Contains(spotLightId))
            return null;
        var role = Roles.Where(entry => entry.RoleId == roleId && IsActivePosition(entry.Position))
            .OrderByDescending(entry => entry.Star).ThenBy(entry => entry.Position).FirstOrDefault();
        if (role == null) return null;
        var changes = new GridFightSpotLightChanges();
        var replaced = _spotLights.FirstOrDefault(entry => entry.UniqueId == _spotLightTargetUniqueId);
        var other = _spotLights.FirstOrDefault(entry => entry.RoleUniqueId == role.UniqueId && entry != replaced && !entry.IsLoss);
        if (other != null) return null; // one spotlight per role (OldSpotLightReplaceHint)
        if (replaced != null)
        {
            replaced.SpotLightId = spotLightId;
            replaced.RoleId = role.RoleId;
            replaced.RoleUniqueId = role.UniqueId;
            replaced.IsLoss = false;
            changes.Updated.Add(replaced);
        }
        else
        {
            var entry = new GridFightSpotLightState
            {
                UniqueId = _nextEntityUniqueId++,
                SpotLightId = spotLightId,
                RoleId = role.RoleId,
                RoleUniqueId = role.UniqueId,
            };
            _spotLights.Add(entry);
            changes.Added.Add(entry);
        }
        _spotLightCandidates.Clear();
        _spotLightTargetUniqueId = 0;
        _spotLightGrantRoleId = 0;
        finished = FinishSeasonTwoPending(GridFightSeasonTwoPendingKind.SpotLightSelect);
        ResolveSpotLightNode(changes);
        return changes;
    }

    /// <summary>HandlePendingAction 1930 { spotlight id of the card }: rerolls that card.</summary>
    public bool RerollSpotLight(uint queuePosition, uint spotLightId, out bool goldChanged)
    {
        goldChanged = false;
        var index = _spotLightCandidates.IndexOf(spotLightId);
        if (queuePosition == 0 || !MatchesSeasonTwoPending(GridFightSeasonTwoPendingKind.SpotLightSelect, queuePosition) ||
            index < 0 || _spotLightRerollsLeft == 0)
            return false;
        var cost = SpotLightRerollCostNow;
        if (Gold < cost) return false;
        var next = RollSpotLight();
        if (next == 0) return false;
        if (cost == 0) _spotLightFreeRefreshUsed++;
        else
        {
            Gold -= cost;
            goldChanged = true;
        }
        _spotLightRerollsLeft--;
        _spotLightCandidates[index] = next;
        return true;
    }

    /// <summary>HandlePendingAction 944: leave a replace flow; the replace charge is refunded (guess).</summary>
    public uint AbandonSpotLightReplace(uint queuePosition)
    {
        if (queuePosition == 0 || !MatchesSeasonTwoPending(GridFightSeasonTwoPendingKind.SpotLightSelect, queuePosition) ||
            _spotLightTargetUniqueId == 0)
            return 0;
        if (_spotLightReplaceUsed > 0) _spotLightReplaceUsed--;
        _spotLightTargetUniqueId = 0;
        _spotLightCandidates.Clear();
        return FinishSeasonTwoPending(GridFightSeasonTwoPendingKind.SpotLightSelect);
    }

    /// <summary>GridFightSpotLightReplaceCsReq (8740): spend a replace charge and reopen the selection for that spotlight.</summary>
    public bool StartSpotLightReplace(uint uniqueId)
    {
        if (Phase != GridFightSessionPhase.Preparing || HasPendingSelection ||
            _spotLightReplaceUsed >= _spotLightReplaceTotal ||
            _spotLights.All(entry => entry.UniqueId != uniqueId))
            return false;
        _spotLightReplaceUsed++;
        BeginSpotLightSelection(uniqueId, 0);
        _seasonTwoPending = new GridFightSeasonTwoPendingState(
            GridFightSeasonTwoPendingKind.SpotLightSelect, PendingQueuePosition, 0, 0, []);
        return true;
    }

    /// <summary>Consumable 350111 主角光环: a granted selection for the target role.</summary>
    public bool StartSpotLightGrant(uint roleUniqueId)
    {
        var role = Roles.FirstOrDefault(entry => entry.UniqueId == roleUniqueId);
        if (role == null || Phase != GridFightSessionPhase.Preparing || HasPendingSelection) return false;
        BeginSpotLightSelection(0, role.RoleId);
        _seasonTwoPending = new GridFightSeasonTwoPendingState(
            GridFightSeasonTwoPendingKind.SpotLightSelect, PendingQueuePosition, 0, 0, []);
        return true;
    }

    public static bool IsSpotLightConsumable(uint itemId) => itemId == SpotLightConsumableId;

    /// <summary>350111 used on a role (target oneof 53): consume it and open a granted selection for that role.</summary>
    public GridFightConsumableUseResult UseSpotLightConsumable(uint groupId, uint roleUniqueId)
    {
        var consumable = Consumables.FirstOrDefault(item => item.GroupId == groupId);
        if (consumable == null || !IsSpotLightConsumable(consumable.ItemId) || !StartSpotLightGrant(roleUniqueId))
            return GridFightConsumableUseResult.Failed;
        var change = ConsumeConsumable(consumable, true);
        return new GridFightConsumableUseResult(true, consumable.ItemId, Enums.GridFight.GridFightConsumeTypeEnum.SpotLight,
            change, [], [], [], null, false, false)
        {
            TargetRoleUniqueId = roleUniqueId,
        };
    }
}
