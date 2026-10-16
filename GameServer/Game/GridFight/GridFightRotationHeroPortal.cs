using March7thHoney.Data;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

/// <summary>
/// Official GridFight portal runtime state reconstructed from the 2026-09-06 captures.
/// Portal 114 (轮岗): one cost tier is selected at each round begin, its shop probability is doubled,
/// and all other non-selected tiers proportionally share the remaining probability.
/// Portal 133 (英雄登场): grants a random 2-star 5-cost role but bans it from active positions until
/// the node countdown reaches zero. Perfect battle clears consume one extra countdown point.
/// </summary>
public sealed partial class GridFightSession
{
    public const uint RotationPortalId = 114;
    public const uint HeroEntrancePortalId = 133;
    public const uint PortalRuntimeItemUniqueId = 101;
    public const uint HeroEntranceInitialCountdown = 20;
    public const uint HeroEntrancePositionBanRetcode = 6937; // RET_GRID_FIGHT_POS_BAN

    private bool _rotationPortalInitialized;
    private uint _rotationBoostedRarity;

    private bool _heroEntranceInitialized;
    private uint _heroEntranceRemaining;
    private uint _heroEntranceRoleUniqueId;
    private uint _heroEntranceRoleId;
    private bool _heroEntrancePerfectAdvancePending;
    private bool _heroEntranceRoundStateDirty;
    private bool _heroEntranceStateDirty;

    public bool HasRotationPortal => SelectedPortalId == RotationPortalId && _rotationPortalInitialized;
    public bool HasHeroEntrancePortal => SelectedPortalId == HeroEntrancePortalId && _heroEntranceInitialized;
    public uint RotationBoostedRarity => _rotationBoostedRarity;
    public uint HeroEntranceRemaining => _heroEntranceRemaining;
    public uint HeroEntranceRoleUniqueId => _heroEntranceRoleUniqueId;
    public uint HeroEntranceRoleId => _heroEntranceRoleId;
    public GridFightRoleState? HeroEntranceRole =>
        Roles.FirstOrDefault(role => role.UniqueId == _heroEntranceRoleUniqueId);

    private void InitializeRotationHeroPortal()
    {
        if (SelectedPortalId == RotationPortalId && !_rotationPortalInitialized)
        {
            _rotationBoostedRarity = RollRotationRarity();
            _rotationPortalInitialized = true;
        }

        if (SelectedPortalId != HeroEntrancePortalId || _heroEntranceInitialized) return;

        var candidates = _catalog.RolePool
            .Where(role => role.Rarity == 5 &&
                           GameData.GridFightRoleStarData.ContainsKey(role.ID << 4 | 2u))
            .ToList();
        if (candidates.Count == 0) return;

        var selected = candidates[Random.Shared.Next(candidates.Count)];
        var position = GetFreeBenchPosition();
        if (position == 0) return;

        var role = new GridFightRoleState
        {
            UniqueId = _nextEntityUniqueId++,
            RoleId = selected.ID,
            Star = 2,
            Position = position,
        };
        Roles.Add(role);
        UnlockRestrictedRoleForRun(role.RoleId);
        _heroEntranceRoleUniqueId = role.UniqueId;
        _heroEntranceRoleId = role.RoleId;
        _heroEntranceRemaining = HeroEntranceInitialCountdown;
        _heroEntrancePerfectAdvancePending = false;
        _heroEntranceRoundStateDirty = false;
        _heroEntranceStateDirty = false;
        _heroEntranceInitialized = true;
        RecalculateRoleSwitches();
    }

    /// <summary>
    /// Called for every official roundBeginAction. The official capture updates both portal states
    /// before the shop payload is emitted for the new preparation stage.
    /// </summary>
    private void AdvanceRotationHeroPortalRound()
    {
        if (HasRotationPortal)
            _rotationBoostedRarity = RollRotationRarity();

        if (!HasHeroEntrancePortal || _heroEntranceRemaining == 0)
        {
            _heroEntrancePerfectAdvancePending = false;
            return;
        }

        var decrement = _heroEntrancePerfectAdvancePending ? 2u : 1u;
        _heroEntranceRemaining = _heroEntranceRemaining > decrement
            ? _heroEntranceRemaining - decrement
            : 0;
        _heroEntrancePerfectAdvancePending = false;
        _heroEntranceRoundStateDirty = true;
    }

    private static uint RollRotationRarity() => (uint)Random.Shared.Next(1, 6);

    private void RecordHeroEntranceBattleResult(bool isPerfectFinish)
    {
        if (!HasHeroEntrancePortal || _heroEntranceRemaining == 0) return;
        _heroEntrancePerfectAdvancePending = isPerfectFinish;
    }

    private bool IsHeroEntrancePositionBanned(uint uniqueId, uint position)
    {
        return HasHeroEntrancePortal &&
               _heroEntranceRemaining > 0 &&
               uniqueId == _heroEntranceRoleUniqueId &&
               IsActivePosition(position);
    }

    private uint ResolveHeroEntranceMergedPosition(
        IReadOnlyCollection<GridFightRoleState> mergedRoles,
        uint fallbackPosition)
    {
        if (!HasHeroEntrancePortal || _heroEntranceRemaining == 0) return fallbackPosition;
        var protectedRole = mergedRoles.FirstOrDefault(role => role.UniqueId == _heroEntranceRoleUniqueId);
        // A merge must not smuggle the still-locked hero onto the battlefield by inheriting the
        // position of another copy. Keep the protected entity's bench position while transferring
        // the portal lock to the newly merged unique id.
        return protectedRole?.Position ?? fallbackPosition;
    }

    private void RebindHeroEntranceAfterMerge(
        IReadOnlyCollection<uint> removedRoleUniqueIds,
        GridFightRoleState mergedRole)
    {
        if (!HasHeroEntrancePortal || _heroEntranceRemaining == 0 ||
            !removedRoleUniqueIds.Contains(_heroEntranceRoleUniqueId))
            return;

        _heroEntranceRoleUniqueId = mergedRole.UniqueId;
        // The official portal item keeps the original/base role id even when role-switch logic
        // temporarily changes the visible role id. Only the runtime unique id is inherited.
        _heroEntranceStateDirty = true;
    }

    public bool ConsumeHeroEntranceRoundStateDirty()
    {
        var dirty = _heroEntranceRoundStateDirty;
        _heroEntranceRoundStateDirty = false;
        return dirty;
    }

    public bool ConsumeHeroEntranceStateDirty()
    {
        var dirty = _heroEntranceStateDirty;
        _heroEntranceStateDirty = false;
        return dirty;
    }

    /// <summary>
    /// Returns the integer weights that the official server sends to the UI and uses for shop rolls.
    /// The capture shows that each cumulative boundary is truncated, rather than independently
    /// rounding each tier. This preserves a total of exactly 100 and reproduces samples such as
    /// 45/33/20/2 -> tier2 => 22/66/10/2 and tier1 => 90/6/3/1.
    /// </summary>
    public IReadOnlyList<uint> GetEffectiveShopRarityWeights(IReadOnlyList<uint> baseWeights)
    {
        var weights = baseWeights.ToList();
        if (!HasRotationPortal || _rotationBoostedRarity == 0 ||
            _rotationBoostedRarity > weights.Count || weights.Count == 0)
            return weights;

        var total = weights.Aggregate(0UL, (sum, value) => sum + value);
        if (total == 0) return weights;

        var selectedIndex = checked((int)_rotationBoostedRarity - 1);
        var selectedBase = (ulong)weights[selectedIndex];
        var selectedBoosted = Math.Min(total, selectedBase * 2UL);
        var remaining = total - selectedBoosted;
        var otherBase = total - selectedBase;

        if (otherBase == 0)
        {
            var only = Enumerable.Repeat(0u, weights.Count).ToList();
            only[selectedIndex] = (uint)Math.Min(total, uint.MaxValue);
            return only;
        }

        // Put every contribution over the same denominator so cumulative truncation can be exact.
        var result = new List<uint>(weights.Count);
        ulong cumulativeNumerator = 0;
        ulong previousBoundary = 0;
        for (var index = 0; index < weights.Count; index++)
        {
            var contributionNumerator = index == selectedIndex
                ? selectedBoosted * otherBase
                : (ulong)weights[index] * remaining;
            cumulativeNumerator += contributionNumerator;
            var boundary = index == weights.Count - 1
                ? total
                : cumulativeNumerator / otherBase;
            result.Add((uint)Math.Min(boundary - previousBoundary, uint.MaxValue));
            previousBoundary = boundary;
        }
        return result;
    }

    public IReadOnlyList<GridFightGameItemInfo> BuildPortalRuntimeGameItems()
    {
        var items = new List<GridFightGameItemInfo>();
        if (HasRotationPortal)
            items.Add(BuildRotationPortalGameItem(includeValue: true));
        if (HasHeroEntrancePortal)
            items.Add(BuildHeroEntranceGameItem(includeValue: true));
        return items;
    }

    public GridFightGameItemInfo BuildRotationPortalGameItem(bool includeValue)
    {
        var item = NewPortalRuntimeItem(RotationPortalId);
        item.LBFBINFMGNF = includeValue
            ? new ILDEFHMIDKP { CKKDAEMFDPA = _rotationBoostedRarity }
            : new ILDEFHMIDKP();
        return item;
    }

    public GridFightGameItemInfo BuildHeroEntranceGameItem(bool includeValue)
    {
        var item = NewPortalRuntimeItem(HeroEntrancePortalId);
        item.NIOCABOLGLO = includeValue
            ? new KPMOCBIIMHH
            {
                IEOGEFBLNEL = _heroEntranceRemaining,
                MDNAGANGNKK = _heroEntranceRoleUniqueId,
                RoleId = _heroEntranceRoleId,
            }
            : new KPMOCBIIMHH();
        return item;
    }

    private static GridFightGameItemInfo NewPortalRuntimeItem(uint portalId)
    {
        var descriptor = new GridFightGameItemDescriptor
        {
            Category = GridFightGameItemCategory.JdcapbnpookLdpmdpagchm,
        };
        descriptor.ParameterList.Add(portalId);
        return new GridFightGameItemInfo
        {
            UniqueId = PortalRuntimeItemUniqueId,
            Descriptor_ = descriptor,
        };
    }
}

public sealed partial class GridFightManager
{
    private void AppendRotationHeroPortalInitialResults(GridFightSyncUpdateResultScNotify notify)
    {
        var session = Session;
        if (session == null) return;

        if (session.HasRotationPortal)
        {
            var createResult = new GridFightSyncResultData
            {
                UpdateSource = GridFightUpdateSrcType.KgridFightSrcSelectPortalBuff,
            };
            createResult.UpdateDynamicList.Add(new GridFightSyncData
            {
                GameDataItemInfo = session.BuildRotationPortalGameItem(includeValue: false),
            });
            notify.ResultList.Add(createResult);

            var initResult = new GridFightSyncResultData
            {
                UpdateSource = GridFightUpdateSrcType.KgridFightSrcPreInitialSupplySelect,
            };
            initResult.EffectParamList.Add(new[]
            {
                GridFightSession.RotationPortalId,
                GridFightSession.PortalRuntimeItemUniqueId,
            });
            initResult.UpdateDynamicList.Add(new GridFightSyncData
            {
                GameDataItemInfo = session.BuildRotationPortalGameItem(includeValue: true),
            });
            // Official portal-selection packet carries a weights-only shop payload here.
            initResult.UpdateDynamicList.Add(new GridFightSyncData
            {
                ShopInfo = new GridFightShopSyncInfo
                {
                    RefreshCost = Session?.EffectiveShopRefreshCost ?? Catalog.RefreshCost,
                    RarityWeightInfo = BuildRarityWeights(),
                },
            });
            notify.ResultList.Add(initResult);
            return;
        }

        if (!session.HasHeroEntrancePortal) return;

        var createHeroResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcSelectPortalBuff,
        };
        createHeroResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            GameDataItemInfo = session.BuildHeroEntranceGameItem(includeValue: false),
        });
        notify.ResultList.Add(createHeroResult);

        var heroInitResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcPreInitialSupplySelect,
        };
        heroInitResult.EffectParamList.Add(new[]
        {
            GridFightSession.HeroEntrancePortalId,
            GridFightSession.PortalRuntimeItemUniqueId,
        });
        if (session.HeroEntranceRole is { } role)
            heroInitResult.UpdateDynamicList.Add(new GridFightSyncData { AddRoleInfo = BuildRole(role) });
        heroInitResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            GameDataItemInfo = session.BuildHeroEntranceGameItem(includeValue: true),
        });
        notify.ResultList.Add(heroInitResult);
    }

    private void AppendRotationHeroRoundBeginResult(GridFightSyncUpdateResultScNotify notify)
    {
        var session = Session;
        if (session == null) return;

        if (session.HasRotationPortal)
        {
            var result = new GridFightSyncResultData
            {
                UpdateSource = GridFightUpdateSrcType.KgridFightSrcPortalBuff,
            };
            result.EffectParamList.Add(new[]
            {
                GridFightSession.RotationPortalId,
                GridFightSession.PortalRuntimeItemUniqueId,
            });
            result.UpdateDynamicList.Add(new GridFightSyncData
            {
                GameDataItemInfo = session.BuildRotationPortalGameItem(includeValue: true),
            });
            notify.ResultList.Add(result);
        }

        if (session.HasHeroEntrancePortal && session.ConsumeHeroEntranceRoundStateDirty())
        {
            // Once zero has been sent, the official capture stops emitting further countdown updates.
            // The zero-valued IEOGEFBLNEL field naturally disappears from protobuf serialization.
            var result = new GridFightSyncResultData
            {
                UpdateSource = GridFightUpdateSrcType.KgridFightSrcPortalBuff,
            };
            result.EffectParamList.Add(new[]
            {
                GridFightSession.HeroEntrancePortalId,
                GridFightSession.PortalRuntimeItemUniqueId,
            });
            result.UpdateDynamicList.Add(new GridFightSyncData
            {
                GameDataItemInfo = session.BuildHeroEntranceGameItem(includeValue: true),
            });
            notify.ResultList.Add(result);
        }
    }

    private void AppendHeroEntranceDirtyResult(GridFightSyncUpdateResultScNotify notify)
    {
        var session = Session;
        if (session == null || !session.HasHeroEntrancePortal ||
            !session.ConsumeHeroEntranceStateDirty())
            return;

        var result = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcPortalBuff,
        };
        result.EffectParamList.Add(new[]
        {
            GridFightSession.HeroEntrancePortalId,
            GridFightSession.PortalRuntimeItemUniqueId,
        });
        result.UpdateDynamicList.Add(new GridFightSyncData
        {
            GameDataItemInfo = session.BuildHeroEntranceGameItem(includeValue: true),
        });
        notify.ResultList.Add(result);
    }
}
