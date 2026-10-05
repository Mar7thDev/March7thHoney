using March7thHoney.Data;
using March7thHoney.Enums.GridFight;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed record GridFightJadeShopGoodState(uint SlotId, uint GoodId, IReadOnlyList<GridFightRewardEntry> Rewards);

public sealed record GridFightTrailblazeGoodState(uint GoodId, uint Cost);

/// <summary>
///     Season-2 shop-like traits: 黑市主理人 (Jade, 3004021), 开拓银轨 (1005021) and 千星商会 (1006021), plus the free
///     shop refreshes several of them hand out. Wire layout: artifacts/gf-season2/re-g1-shops.md. Pools, unlocks and
///     node triggers are not encoded in the client and follow the trait descriptions (marked where guessed).
/// </summary>
public sealed partial class GridFightSession
{
    public const uint JadeTraitId = 300402;
    public const uint JadeEffectId = 3004021;
    public const uint JadeRoleId = 131402;
    public const uint JadeSpecialSlotId = 1;
    public const uint TrailblazeTraitId = 100502;
    public const uint TrailblazeEffectId = 1005021;
    public const uint TrailblazeRefreshGoodId = 999;
    public const uint AstropolisTraitId = 100602;
    public const uint AstropolisEffectId = 1006021;
    private const int TrailblazeGoodsCount = 5;

    private readonly List<GridFightJadeShopGoodState> _jadeGoods = [];
    private (uint ChapterId, uint SectionId) _jadeGoodsNode;
    private uint _jadeGoodsStar;
    private bool _jadeAutoOpen;
    private uint _trailblazeCoin;
    private readonly List<GridFightTrailblazeGoodState> _trailblazeGoods = [];
    private readonly Dictionary<uint, uint> _trailblazeBought = [];
    private (uint ChapterId, uint SectionId) _trailblazeGoodsNode;
    private bool _trailblazeActivated;
    private (uint ChapterId, uint SectionId) _astropolisNode;
    private uint _freeShopRefreshCount;

    public IReadOnlyList<GridFightJadeShopGoodState> JadeGoods => _jadeGoods;
    public bool JadeRefreshUnlocked => ResolveJadeStar() >= 2;
    public uint TrailblazeCoin => _trailblazeCoin;
    public IReadOnlyList<GridFightTrailblazeGoodState> TrailblazeGoods => _trailblazeGoods;
    public IReadOnlyDictionary<uint, uint> TrailblazeBought => _trailblazeBought;
    public uint FreeShopRefreshCount => _freeShopRefreshCount;
    public uint EffectiveShopRefreshCost => _freeShopRefreshCount > 0 ? 0 : _catalog.RefreshCost;

    /// <summary>Returns and clears the "auto-open the Jade shop" flag (JADHHGHGMPH field 14).</summary>
    public bool TakeJadeAutoOpen()
    {
        var value = _jadeAutoOpen;
        _jadeAutoOpen = false;
        return value;
    }

    private void InitializeSeasonTwoShops()
    {
        _jadeGoodsNode = (0, 0);
        _trailblazeGoodsNode = (0, 0);
        _astropolisNode = (ChapterId, SectionId);
    }

    private bool TryConsumeFreeShopRefresh()
    {
        if (_freeShopRefreshCount == 0) return false;
        _freeShopRefreshCount--;
        return true;
    }

    private void RefreshSeasonTwoShops(GridFightSeasonTwoChanges changes)
    {
        var layers = ResolveTraitLayers();
        RefreshJadeShop(layers, changes);
        RefreshTrailblazeShop(layers, changes);
        RefreshAstropolis(layers, changes);
    }

    // ---------------------------------------------------------------- 黑市主理人

    private uint ResolveJadeStar() =>
        Roles.Where(role => role.RoleId == JadeRoleId && IsActivePosition(role.Position))
            .Select(role => role.Star)
            .DefaultIfEmpty(0u)
            .Max();

    private void RefreshJadeShop(
        IReadOnlyDictionary<uint, GridFightTraitLayerState> layers,
        GridFightSeasonTwoChanges changes)
    {
        if (!layers.TryGetValue(JadeTraitId, out var state) || state.Layer == 0) return;
        var star = Math.Clamp(ResolveJadeStar(), 1u, 3u);
        if (_jadeGoods.Count > 0 && _jadeGoodsNode == (ChapterId, SectionId) && _jadeGoodsStar >= star) return;
        RollJadeGoods(star);
        _jadeAutoOpen = true;
        changes.ShopChanged = true;
    }

    /// <summary>
    ///     Layer params [131402, 1, 2, 3]: number of rotating goods for a 1/2/3★ Jade (guess). Rotating goods come
    ///     from the 1xxxx table group at 1★, 2xxxx at 2★, 2xxxx + 3xxxx at 3★; the signature good (slot 1) is the
    ///     "#1金币 for 10 HP" row of the group.
    /// </summary>
    private void RollJadeGoods(uint star)
    {
        _jadeGoods.Clear();
        _jadeGoodsNode = (ChapterId, SectionId);
        _jadeGoodsStar = star;
        var signatureId = star >= 2 ? 20001u : 10006u;
        AddJadeGood(JadeSpecialSlotId, signatureId);
        var layerParams = GetTraitEffectLayerParams(JadeEffectId, 1);
        var count = (int)(layerParams.ElementAtOrDefault((int)star) is var value and > 0 ? value : star);
        var pool = GridFightSeasonTwoTables.JadeShop
            .Where(row => row.Id != signatureId && IsJadeGoodSellable(row) && (star switch
            {
                1 => row.Id / 10000 == 1,
                2 => row.Id / 10000 == 2,
                _ => row.Id / 10000 is 2 or 3,
            }))
            .ToList();
        for (var index = 0; index < count && pool.Count > 0; index++)
        {
            var row = pool[Random.Shared.Next(pool.Count)];
            pool.Remove(row);
            AddJadeGood((uint)(index + 2), row.Id);
        }
    }

    private static bool IsJadeGoodSellable(GridFightJadeShopRow row) =>
        GameData.GridFightBasicBonusPoolV2Data.TryGetValue(row.BonusId, out var bonus) &&
        bonus.BonusType != GridFightBonusTypeEnum.GlobalHp;

    private void AddJadeGood(uint slotId, uint goodId)
    {
        var row = GridFightSeasonTwoTables.JadeShop.FirstOrDefault(entry => entry.Id == goodId);
        if (row == null) return;
        _jadeGoods.Add(new GridFightJadeShopGoodState(slotId, goodId, ResolveJadeRewards(row)));
    }

    private static IReadOnlyList<GridFightRewardEntry> ResolveJadeRewards(GridFightJadeShopRow row)
    {
        var multiplier = row.Params.FirstOrDefault() is var param and > 0 ? param : 1u;
        if (!GameData.GridFightBasicBonusPoolV2Data.TryGetValue(row.BonusId, out var bonus)) return [];
        if (bonus.BonusType == GridFightBonusTypeEnum.Refresh)
            return [new GridFightRewardEntry(GridFightBonusTypeEnum.Refresh, 0, multiplier)];
        if (!GridFightSpecialFeatures.TryResolveBonus(row.BonusId, out var rewards)) return [];
        if (multiplier == 1) return rewards;
        return rewards.Select(reward => reward with { Count = reward.Count * multiplier }).ToList();
    }

    /// <summary>JadeShop BuyGood: TraitUpdate 30 { EAKAGIKFENM = slot_id }. Slot 1 never sells out.</summary>
    public GridFightRewardApplication BuyJadeGood(uint slotId)
    {
        var failed = new GridFightRewardApplication();
        if (Phase != GridFightSessionPhase.Preparing || ResolveTraitLayer(JadeTraitId) == 0) return failed;
        var good = _jadeGoods.FirstOrDefault(entry => entry.SlotId == slotId);
        var row = good == null ? null : GridFightSeasonTwoTables.JadeShop.FirstOrDefault(entry => entry.Id == good.GoodId);
        if (good == null || good.GoodId == 0 || row == null || good.Rewards.Count == 0) return failed;
        var paysHp = row.CostType == 2;
        if (paysHp ? LineupHp <= row.CostParam : Gold < row.CostParam) return failed;

        var snapshot = CaptureSeasonTwoRewardState();
        if (paysHp) LineupHp -= row.CostParam;
        else Gold -= row.CostParam;
        var application = ApplySeasonTwoRewards(good.Rewards);
        if (!application.Success)
        {
            RestoreSeasonTwoRewardState(snapshot);
            return failed;
        }
        if (paysHp) application.LineupHpChanged = true;
        else application.GoldChanged = true;

        var index = _jadeGoods.IndexOf(good);
        _jadeGoods[index] = slotId == JadeSpecialSlotId
            ? good with { Rewards = ResolveJadeRewards(row) }
            : new GridFightJadeShopGoodState(slotId, 0, []);
        return application;
    }

    /// <summary>JadeShop RefreshGoods: TraitUpdate 30 { AHAIBDAJAIB = true }; costs one shop refresh.</summary>
    public bool RefreshJadeGoods(out bool goldChanged)
    {
        goldChanged = false;
        if (Phase != GridFightSessionPhase.Preparing || ResolveTraitLayer(JadeTraitId) == 0 || !JadeRefreshUnlocked)
            return false;
        if (!TryConsumeFreeShopRefresh())
        {
            if (Gold < _catalog.RefreshCost) return false;
            Gold -= _catalog.RefreshCost;
            goldChanged = true;
        }
        RollJadeGoods(Math.Clamp(ResolveJadeStar(), 1u, 3u));
        return true;
    }

    private (GridFightRewardStateSnapshot Base, uint LineupHp, uint FreeRefreshes, uint TrailblazeCoin)
        CaptureSeasonTwoRewardState() =>
        (CaptureRewardState(), LineupHp, _freeShopRefreshCount, _trailblazeCoin);

    private void RestoreSeasonTwoRewardState(
        (GridFightRewardStateSnapshot Base, uint LineupHp, uint FreeRefreshes, uint TrailblazeCoin) snapshot)
    {
        RestoreRewardState(snapshot.Base);
        LineupHp = snapshot.LineupHp;
        _freeShopRefreshCount = snapshot.FreeRefreshes;
        _trailblazeCoin = snapshot.TrailblazeCoin;
    }

    /// <summary>ApplyRewards plus the reward kinds only season-2 goods hand out (free refreshes, team HP).</summary>
    private GridFightRewardApplication ApplySeasonTwoRewards(IReadOnlyList<GridFightRewardEntry> rewards)
    {
        var regular = rewards.Where(reward => reward.Type is not (GridFightBonusTypeEnum.Refresh or
            GridFightBonusTypeEnum.GlobalHp or GridFightBonusTypeEnum.TrailblazeShopCoin)).ToList();
        var application = regular.Count == 0 ? new GridFightRewardApplication { Success = true } : ApplyRewards(regular);
        if (!application.Success) return application;
        foreach (var reward in rewards)
            switch (reward.Type)
            {
                case GridFightBonusTypeEnum.Refresh:
                    _freeShopRefreshCount += reward.Count;
                    application.Drops.Add(reward);
                    break;
                case GridFightBonusTypeEnum.GlobalHp:
                    LineupHp = (uint)Math.Min(MaxLineupHp, (ulong)LineupHp + reward.Count);
                    application.LineupHpChanged = true;
                    application.Drops.Add(reward);
                    break;
                case GridFightBonusTypeEnum.TrailblazeShopCoin:
                    _trailblazeCoin += reward.Count;
                    application.Drops.Add(reward);
                    break;
            }
        return application;
    }

    // ---------------------------------------------------------------- 开拓银轨

    private void RefreshTrailblazeShop(
        IReadOnlyDictionary<uint, GridFightTraitLayerState> layers,
        GridFightSeasonTwoChanges changes)
    {
        if (!layers.TryGetValue(TrailblazeTraitId, out var state) || state.Layer == 0) return;
        if (!_trailblazeActivated)
        {
            // 星穹列车，跃迁！(101) is the base train every later MazeBuff good upgrades (FatherGoodID chain).
            _trailblazeActivated = true;
            _trailblazeBought[101] = 1;
        }
        if (_trailblazeGoods.Count > 0 && _trailblazeGoodsNode == (ChapterId, SectionId)) return;
        RollTrailblazeGoods(state.Layer);
        changes.ShopChanged = true;
    }

    /// <summary>
    ///     Layer params [tier, coin per battle, ?, 999]. Goods of the hundreds group up to tier + 1 are offered
    ///     (guess: the table has no tier column); MazeBuff upgrades follow their FatherGoodID chain and are sold
    ///     once, Property goods repeat. The refresh good (999) is always appended.
    /// </summary>
    private void RollTrailblazeGoods(uint layer)
    {
        _trailblazeGoods.Clear();
        _trailblazeGoodsNode = (ChapterId, SectionId);
        var tier = (uint)GetTraitEffectLayerParams(TrailblazeEffectId, layer).FirstOrDefault();
        var pool = GridFightSeasonTwoTables.TrailblazeGoods
            .Where(row => row.Id != TrailblazeRefreshGoodId && row.Cost > 0 && row.Id / 100 <= tier + 1 &&
                          IsTrailblazeGoodAvailable(row))
            .ToList();
        for (var index = 0; index < TrailblazeGoodsCount && pool.Count > 0; index++)
        {
            var row = pool[Random.Shared.Next(pool.Count)];
            pool.Remove(row);
            _trailblazeGoods.Add(new GridFightTrailblazeGoodState(row.Id, row.Cost));
        }
        var refresh = GridFightSeasonTwoTables.TrailblazeGoods.FirstOrDefault(row => row.Id == TrailblazeRefreshGoodId);
        if (refresh != null) _trailblazeGoods.Add(new GridFightTrailblazeGoodState(refresh.Id, Math.Max(refresh.Cost, 1)));
    }

    private bool IsTrailblazeGoodAvailable(GridFightTrailblazeGoodsRow row)
    {
        switch (row.Type)
        {
            case "MazeBuff":
                if (_trailblazeBought.ContainsKey(row.Id)) return false;
                return row.FatherGoodId == 0 || _trailblazeBought.ContainsKey(row.FatherGoodId);
            case "TrailFuel":
                // +30/+60 开拓力 for 1 开拓力 would be a free loop; how fuel is really priced is unknown.
                return false;
            case "Role":
                return row.Params.Length > 0 && GameData.GridFightRoleBasicInfoData.ContainsKey(row.Params[0]);
            case "":
            case "Bonus":
                return row.Params.Length > 0 && GridFightSpecialFeatures.TryResolveBonus(row.Params[0], out _);
            default:
                return true;
        }
    }

    /// <summary>Trailblaze BuyGood: TraitUpdate 367 { pos = 0-based index into the last goods list }.</summary>
    public GridFightRewardApplication BuyTrailblazeGood(uint index)
    {
        var failed = new GridFightRewardApplication();
        var layer = ResolveTraitLayer(TrailblazeTraitId);
        if (Phase != GridFightSessionPhase.Preparing || layer == 0 || index >= _trailblazeGoods.Count) return failed;
        var good = _trailblazeGoods[(int)index];
        var row = GridFightSeasonTwoTables.TrailblazeGoods.FirstOrDefault(entry => entry.Id == good.GoodId);
        if (row == null || _trailblazeCoin < good.Cost) return failed;

        var snapshot = CaptureSeasonTwoRewardState();
        _trailblazeCoin -= good.Cost;
        GridFightRewardApplication application;
        switch (row.Type)
        {
            case "RefreshGoods":
                RollTrailblazeGoods(layer);
                return new GridFightRewardApplication { Success = true };
            case "TrailFuel":
                _trailblazeCoin += row.Params.FirstOrDefault();
                application = new GridFightRewardApplication { Success = true };
                break;
            case "MazeBuff":
            case "Property":
                _trailblazeBought[row.Id] = _trailblazeBought.GetValueOrDefault(row.Id) + 1;
                application = new GridFightRewardApplication { Success = true };
                break;
            case "Role":
                var star = row.Params.ElementAtOrDefault(1) is var roleStar and > 0 ? roleStar : 1u;
                application = ApplyRewards([new GridFightRewardEntry(GridFightBonusTypeEnum.SpecificAvatar, row.Params[0], 1, star)]);
                break;
            default:
                application = GridFightSpecialFeatures.TryResolveBonus(row.Params.FirstOrDefault(), out var rewards)
                    ? ApplySeasonTwoRewards(rewards)
                    : failed;
                break;
        }
        if (!application.Success)
        {
            RestoreSeasonTwoRewardState(snapshot);
            return failed;
        }
        if (row.Type != "Property") _trailblazeGoods.RemoveAt((int)index);
        return application;
    }

    /// <summary>每个战斗阶段后积攒【开拓力】: layer param 1 per finished battle (20/45/75/125/225).</summary>
    private void GrantTrailblazeBattleCoin()
    {
        var layer = ResolveTraitLayer(TrailblazeTraitId);
        if (!IsSeasonTwoRules || layer == 0) return;
        _trailblazeCoin += (uint)GetTraitEffectLayerParams(TrailblazeEffectId, layer).ElementAtOrDefault(1);
    }

    /// <summary>Battle MazeBuffs bought in the Trailblaze shop (train upgrades).</summary>
    public IReadOnlyList<int> GetTrailblazeBattleBuffIds()
    {
        if (!IsSeasonTwoRules || ResolveTraitLayer(TrailblazeTraitId) == 0) return [];
        var bought = GridFightSeasonTwoTables.TrailblazeGoods
            .Where(row => row.Type == "MazeBuff" && _trailblazeBought.ContainsKey(row.Id) && row.Params.Length > 0)
            .ToList();
        // A bought upgrade supersedes its father (101 -> 201 -> 301 -> 400).
        return bought
            .Where(row => bought.All(other => other.FatherGoodId != row.Id))
            .Select(row => (int)row.Params[0])
            .ToList();
    }

    // ---------------------------------------------------------------- 千星商会

    public uint ResolveAstropolisTier()
    {
        if (!IsSeasonTwoRules) return 0;
        var layers = ResolveTraitLayers();
        if (!layers.TryGetValue(AstropolisTraitId, out var state) || state.Layer == 0) return 0;
        return (uint)state.Members.Count(role => role.Star >= 3);
    }

    /// <summary>
    ///     Tier rewards from GridFightTraitAstropolis that only need lobby state: tier 2 gives one free refresh per
    ///     new node. (Tier 1 exp per refresh is applied in RefreshShop; tiers 3-6 are not implemented.)
    /// </summary>
    private void RefreshAstropolis(
        IReadOnlyDictionary<uint, GridFightTraitLayerState> layers,
        GridFightSeasonTwoChanges changes)
    {
        if (_astropolisNode == (ChapterId, SectionId)) return;
        _astropolisNode = (ChapterId, SectionId);
        if (ResolveAstropolisTier() < 2) return;
        _freeShopRefreshCount++;
        changes.ShopChanged = true;
    }

    private void ApplyAstropolisRefreshBonus()
    {
        if (ResolveAstropolisTier() >= 1) AddPlayerExp(1);
    }
}
