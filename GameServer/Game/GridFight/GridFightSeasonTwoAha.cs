using March7thHoney.Data;
using March7thHoney.Enums.GridFight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

/// <summary>
///     幻乐之主 (Aha) shop influence and 7-cost progress. "星神★阿哈将持续投下瞥视，对商店施加影响": while an Aha is on the
///     board, a shop roll may turn one slot into an Aha special good (GridFightSpecialGoods group 2, the upgraded group 3
///     once Aha reaches 5 cost). How often the official server does this is not known; see AhaShopGlanceChance.
///     Only goods whose effect maps onto existing server state are offered.
/// </summary>
public sealed partial class GridFightSession
{
    private const uint AhaGoodsBaseGroup = 2;
    private const uint AhaGoodsUpgradedGroup = 3;
    private const uint AhaGoodsFirstId = 300;
    private const uint MoonGameEmblemId = 355100302;
    private const uint ElationEmblemId = 355200702;

    private static readonly HashSet<uint> ImplementedAhaGoods =
    [
        301, 302, 303, 306, 308, 309, 311, 312, 325, 332, 333, 337, 339, 340, 341, 342, 346, 347, 349, 353, 354,
    ];

    private uint _ahaModifierUniqueId;
    private bool _ahaUltraConfirmed;
    private bool _ahaUltraIssued;
    private readonly Dictionary<uint, uint> _ahaUltraItemIds = [];

    public uint AhaModifierUniqueId => _ahaModifierUniqueId;

    /// <summary>星神★阿哈 3星7费 confirmed: everyone joins 幻月游戏 and 欢愉.</summary>
    public bool AhaUltraConfirmed => _ahaUltraConfirmed;

    public bool IsAhaMaxStarReached =>
        IsSeasonTwoRules && Roles.Any(role => role.RoleId == AhaSevenCostRoleId && role.Star >= 3 &&
                                              IsActivePosition(role.Position));

    /// <summary>
    ///     达到3星7费后迎来真正的主人. The client opens GridFightS2MainCharacterUltraBuffPanel for a pending block action
    ///     (GridFightPendingAction oneof 1240, type 2) showing both GridFightAhaMaxStar rows (幻月之主, 欢愉之主) with a single
    ///     confirm and no choice, so confirming applies both: every fielded role joins 幻月游戏 and 欢愉.
    /// </summary>
    private bool TryStartAhaUltraPending(GridFightSeasonTwoChanges changes)
    {
        if (_ahaUltraIssued || !IsAhaMaxStarReached) return false;
        _ahaUltraIssued = true;
        StartSeasonTwoPending(GridFightSeasonTwoPendingKind.AhaUltra, AhaTraitId, 0, [], changes);
        return true;
    }

    /// <summary>HandlePendingAction 230 { type = 2 }.</summary>
    public uint ConfirmAhaUltra(uint queuePosition)
    {
        if (queuePosition == 0 || !MatchesSeasonTwoPending(GridFightSeasonTwoPendingKind.AhaUltra, queuePosition)) return 0;
        _ahaUltraConfirmed = true;
        return FinishSeasonTwoPending(GridFightSeasonTwoPendingKind.AhaUltra);
    }

    /// <summary>Trait-member grants the client must see (GridFightUnlockTraitMember items): (item id, trait, role ids).</summary>
    public IReadOnlyList<(uint ItemUniqueId, uint TraitId, IReadOnlyList<uint> RoleIds)> GetAhaUltraTraitGrants()
    {
        if (!_ahaUltraConfirmed || !IsAhaMaxStarReached) return [];
        var roleIds = Roles.Where(role => IsActivePosition(role.Position)).Select(role => role.RoleId).Distinct().ToList();
        var result = new List<(uint, uint, IReadOnlyList<uint>)>();
        foreach (var traitId in new[] { MoonGameTraitId, ElationTraitId })
        {
            if (!_ahaUltraItemIds.TryGetValue(traitId, out var itemId))
                _ahaUltraItemIds[traitId] = itemId = _nextEntityUniqueId++;
            result.Add((itemId, traitId, roleIds));
        }
        return result;
    }

    public static bool IsAhaGoods(uint specialGoodsId) => specialGoodsId > AhaGoodsFirstId;

    private bool IsAhaOnBoard => Roles.Any(role => IsAhaRole(role.RoleId) && IsActivePosition(role.Position));

    private static uint AhaGoodsBaseId(GridFightAhaGoodsRow row) => row.LowerGoodId != 0 ? row.LowerGoodId : row.Id;

    /// <summary>7-cost leading-trait progress: (trait, target total stars, current total stars).</summary>
    public IReadOnlyList<(uint TraitId, uint Target, uint Progress)> GetAhaSevenCostProgress() =>
    [
        (ElationTraitId, 10, GetTraitTotalStars(ElationTraitId)),
        (MoonGameTraitId, 12, GetTraitTotalStars(MoonGameTraitId)),
    ];

    private void EnsureAhaModifier()
    {
        if (_ahaModifierUniqueId == 0 && IsSeasonTwoRules && Roles.Any(role => IsAhaRole(role.RoleId)))
            _ahaModifierUniqueId = _nextEntityUniqueId++;
    }

    /// <summary>
    ///     Chance that a shop roll gets an Aha good, by the highest Aha tier on the board. The client only says "可能"; the
    ///     official numbers are server-side, so 3-cost 20% / 4-cost 30% / 5- and 7-cost 40% is a chosen value.
    /// </summary>
    public double AhaShopGlanceChance
    {
        get
        {
            var ahaIds = Roles.Where(role => IsAhaRole(role.RoleId) && IsActivePosition(role.Position))
                .Select(role => role.RoleId).ToList();
            if (ahaIds.Count == 0) return 0;
            return ahaIds.Max() switch
            {
                AhaThreeCostRoleId => 0.2,
                AhaFourCostRoleId => 0.3,
                _ => 0.4,
            };
        }
    }

    private void ApplyAhaShopGlance()
    {
        if (!IsSeasonTwoRules || !IsAhaOnBoard || Shop.Count == 0) return;
        if (Random.Shared.NextDouble() >= AhaShopGlanceChance) return;
        var upgraded = Roles.Any(role => role.RoleId is AhaFiveCostRoleId or AhaSevenCostRoleId);
        var group = upgraded ? AhaGoodsUpgradedGroup : AhaGoodsBaseGroup;
        var pool = GridFightSeasonTwoTables.AhaGoods
            .Where(row => row.GroupId == group && ImplementedAhaGoods.Contains(AhaGoodsBaseId(row)))
            .ToList();
        if (pool.Count == 0) return;
        var row = pool[Random.Shared.Next(pool.Count)];
        var index = Shop.FindLastIndex(slot => slot.SpecialGoodsId == 0);
        if (index < 0) return;
        Shop[index] = new GridFightShopSlot { Index = Shop[index].Index, SpecialGoodsId = row.Id, Price = row.Cost };
    }

    private GridFightRewardApplication ApplyAhaSpecialGoods(uint goodsId)
    {
        var row = GridFightSeasonTwoTables.AhaGoods.FirstOrDefault(entry => entry.Id == goodsId);
        if (row == null) return new GridFightRewardApplication();
        var p = row.Params;
        uint Param(int index) => index < p.Length ? (uint)p[index] : 0;
        GridFightRewardApplication Give(params GridFightRewardEntry[] rewards) => ApplySeasonTwoRewards(rewards);
        GridFightRewardEntry Item(uint id) => new(GridFightBonusTypeEnum.Item, id, 1);
        GridFightRewardEntry Role(uint id, uint star = 1) => new(GridFightBonusTypeEnum.SpecificAvatar, id, 1, star);
        List<uint> RolesOfRarity(uint rarity) =>
            _catalog.RolePool.Where(role => role.Rarity == rarity).Select(role => role.ID).ToList();
        uint Pick(List<uint> ids) => ids.Count == 0 ? 0 : ids[Random.Shared.Next(ids.Count)];

        switch (AhaGoodsBaseId(row))
        {
            case 301: // 零钱入袋
                return Give(new GridFightRewardEntry(GridFightBonusTypeEnum.Gold, 0, Param(0)));
            case 302: // 整备补贴: 免费商店刷新
            case 340: // 幸运七连
                return Give(new GridFightRewardEntry(GridFightBonusTypeEnum.Refresh, 0, Param(0)));
            case 303: // 三费专场 (+ gold on the upgraded row)
            case 341: // 五费专场
            case 342: // 四费专场
            {
                var rarity = AhaGoodsBaseId(row) switch { 303 => 3u, 341 => 5u, _ => 4u };
                RefillShopWithRarity(rarity);
                return row.Id == 403 && Param(0) > 0
                    ? Give(new GridFightRewardEntry(GridFightBonusTypeEnum.Gold, 0, Param(0)))
                    : new GridFightRewardApplication { Success = true };
            }
            case 306: // 漫画家: 员工投影仪
                return Give(Item(350105));
            case 339: // 完美光影
                return Give(Item(350106));
            case 312: // 快换一个: 冶金炉
                return Give(Item(350103));
            case 308: // 高阶援军
            case 309: // 王牌空降
                return Pick(RolesOfRarity(Param(0))) is var role and > 0 ? Give(Role(role, Math.Max(Param(2), 1))) : new();
            case 311: // 新面孔: 2 名尚未拥有、互不相同的 1 星 4 费角色
            {
                var owned = Roles.Select(entry => entry.RoleId).ToHashSet();
                var candidates = RolesOfRarity(Param(1)).Where(id => !owned.Contains(id)).ToList();
                var picks = new List<GridFightRewardEntry>();
                for (var i = 0; i < Param(0) && candidates.Count > 0; i++)
                {
                    var id = Pick(candidates);
                    candidates.Remove(id);
                    picks.Add(Role(id, Math.Max(Param(2), 1)));
                }
                return picks.Count == 0 ? new GridFightRewardApplication() : Give([.. picks]);
            }
            case 325: // 掷币交易
                return Give(new GridFightRewardEntry(GridFightBonusTypeEnum.Gold, 0,
                    Random.Shared.NextDouble() < 0.5 ? Math.Max(Param(0), 1) : Param(1)));
            case 332: // 回点血！
                return Give(new GridFightRewardEntry(GridFightBonusTypeEnum.GlobalHp, 0, Param(0)));
            case 333: // 多回点血！: 获得#1点小队生命值和生命上限
            {
                MaxLineupHp = (uint)Math.Min(uint.MaxValue, (ulong)MaxLineupHp + Param(0));
                var heal = Give(new GridFightRewardEntry(GridFightBonusTypeEnum.GlobalHp, 0, Param(0)));
                heal.LineupHpChanged = true;
                return heal;
            }
            case 337: // 我5费呢？！
                return Give([.. RolesOfRarity(5).Where(id => !IsAhaRole(id)).Select(id => Role(id))]);
            case 346: // 星神降临
                return Give(Role(AhaThreeCostRoleId));
            case 349: // 球球了！: 3 星【星神★阿哈】
                return Give(Role(AhaSevenCostRoleId, 3));
            case 347: // 开拓之旅 (only the 开拓力 part)
                return Give(new GridFightRewardEntry(GridFightBonusTypeEnum.TrailblazeShopCoin, 0, Param(0)));
            case 353: // 邀月入局
                return Give(Item(MoonGameEmblemId));
            case 354: // 欢愉加席
                return Give(Item(ElationEmblemId));
            default:
                return new GridFightRewardApplication();
        }
    }

    /// <summary>
    ///     三费/四费/五费专场: "立即刷新商店，使其充满N费角色" refreshes the whole shop, including the slot of the good being
    ///     bought, from the same pool an ordinary refresh uses (roles already at 3★ stay out of it).
    /// </summary>
    private void RefillShopWithRarity(uint rarity)
    {
        var excluded = GetMaxStarExcludedRoleIds();
        var candidates = _catalog.RolePool
            .Where(role => role.Rarity == rarity && IsRestrictedRoleUnlockedForRun(role.ID) && !excluded.Contains(role.ID))
            .Select(role => role.ID)
            .ToList();
        if (candidates.Count == 0) return;
        for (var index = 0; index < Shop.Count; index++)
        {
            var roleId = candidates[Random.Shared.Next(candidates.Count)];
            var star = ResolveMarketVitalityStar(roleId);
            Shop[index] = new GridFightShopSlot
            {
                Index = Shop[index].Index,
                RoleId = roleId,
                Star = star,
                Price = _catalog.GetBuyPrice(roleId, star),
            };
        }
    }
}

public sealed partial class GridFightManager
{
    private string _sentAhaProgress = "";

    /// <summary>
    ///     GridFightUpgradeAhaRarityModifier (GridFightGameItemInfo oneof 448): the client shows the leading trait's
    ///     total stars against the target as the second 7-cost unlock condition.
    /// </summary>
    private GridFightGameItemInfo? BuildAhaModifierItem(GridFightSession session)
    {
        if (session.AhaModifierUniqueId == 0) return null;
        var modifier = new IDEDIHLMEHL();
        modifier.GridFightTraitInfo.Add(session.GetAhaSevenCostProgress().Select(entry => new GHKEBIOFDHI
        {
            TraitId = entry.TraitId,
            Target = entry.Target,
            Progress = entry.Progress,
        }));
        return new GridFightGameItemInfo
        {
            UniqueId = session.AhaModifierUniqueId,
            Descriptor_ = BuildGameItemDescriptor(GridFightGameItemCategory.HackConsole, GridFightSession.AhaTraitId),
            GEKCCCPGBGJ = modifier,
        };
    }

    private void AppendAhaModifierDiff(GridFightSyncResultData result, GridFightSession session)
    {
        if (BuildAhaModifierItem(session) is not { } item) return;
        var key = string.Join(',', item.GEKCCCPGBGJ.GridFightTraitInfo.Select(entry => $"{entry.TraitId}:{entry.Progress}"));
        if (key == _sentAhaProgress) return;
        _sentAhaProgress = key;
        result.UpdateDynamicList.Add(new GridFightSyncData { GameDataItemInfo = item });
    }
}
