using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.GridFight;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed record GridFightRouteNode(
    uint RouteId,
    uint ChapterId,
    uint SectionId,
    GridFightNodeTemplateExcel Template)
{
    public uint StageId => Template.StageID;
    public GridFightNodeTypeEnum NodeType => Template.NodeType;
    public uint BasicGoldReward => Template.BasicGoldRewardNum;
    public uint PenaltyBonusRuleId => Template.PenaltyBonusRuleID;
    public IReadOnlyList<uint> Parameters => Template.ParamList;
}

public sealed class GridFightResourceCatalog
{
    private readonly Random _random;

    public uint CurrentSeason { get; }
    public uint CurrentSubSeason { get; }
    public uint ActivityModuleId { get; }
    public uint ContentModuleId { get; }
    public GridFightDivisionInfoExcel HighestDivision { get; }
    public uint StandardRouteId { get; }
    public IReadOnlyList<GridFightRouteNode> StandardRoute { get; }
    public IReadOnlyList<GridFightRoleBasicInfoExcel> RolePool { get; }
    public IReadOnlyList<GridFightCampExcel> CampPool { get; }
    public IReadOnlyList<GridFightCampExcel> RewardCampPool { get; }
    public IReadOnlyList<uint> PortalPool { get; }
    public IReadOnlyList<uint> AugmentPool { get; }
    public IReadOnlyList<uint> BasicEquipmentPool { get; }
    public IReadOnlySet<uint> InitialExcludedRoleIds { get; }
    public uint ShopSize => GetUInt("GridFight_CardNumberPerRefresh");
    public uint RefreshCost => GetUInt("GridFight_LotteryRefreshGold");
    public uint BuyExpCost => GetUInt("GridFight_LevelUpGold");
    public uint BuyExpAmount => GetUInt("GridFight_LevelUpExp");
    public uint BattleWinExp => RequireUInt("GridFight_GainExpWhenWaveEnd");
    public uint InitialHp => GetUInt("GridFight_InitGlobalPlayerHP");
    public uint InitialPlayerLevel => RequireUInt("GridFight_LotteryShopStartLevel");
    public uint MaxPlayerLevel => GameData.GridFightPlayerLevelData.Keys.Max();
    public uint InitialRoleCount => RequireUInt("GridFight_BaseSupplyRoleNum");
    public uint BenchSize => RequireUInt("GridFight_Bench_AvatarNum");
    public uint BenchOverflowSize => RequireUInt("GridFight_Bench_OverFlow_AvatarNum");
    public uint OffFieldInitialCount => RequireUInt("GridFight_Back_AvatarInitialNum");
    public uint DepositPerInterest => RequireUInt("GridFight_DepositPerInterest");
    public uint MaxInterestGold => RequireUInt("GridFight_GainInterestMax");
    public uint SeasonExpLimit => RequireUInt("GridFight_ExtraSeasonExp_Limit");
    public IReadOnlyList<uint> InitialEquipmentIds => GetUIntList("GridFight_PhainonEquipPreset");
    public uint InitialGold { get; }
    public uint TutorialCompletion { get; }

    public GridFightResourceCatalog(Random? random = null)
    {
        _random = random ?? Random.Shared;
        CurrentSeason = RequireUInt("GridFight_Season_ID");
        CurrentSubSeason = RequireUInt("GridFight_Sub_Season_ID");
        ActivityModuleId = GameData.GridFightSeasonModuleData.Values
            .Single(row => row.SeasonID == CurrentSeason && row.SubSeasonID == CurrentSubSeason)
            .ActivityModuleID;
        ContentModuleId = ResolveContentModuleId();
        HighestDivision = GameData.GridFightDivisionInfoData.Values
            .Where(row => row.SeasonID == CurrentSeason)
            .OrderBy(row => row.Progress)
            .ThenBy(row => row.ID)
            .Last();
        StandardRouteId = ResolveStandardRouteId();
        StandardRoute = BuildRoute(StandardRouteId);

        var bannedRoles = GameData.GridFightModuleBanRoleData
            .Where(row => row.ModuleId == ContentModuleId)
            .Select(row => row.RoleId)
            .ToHashSet();
        RolePool = GameData.GridFightRoleBasicInfoData.Values
            .Where(row => row.SeasonID == CurrentSeason && row.IsInPool && !bannedRoles.Contains(row.ID))
            .Where(row => row.AvatarID is >= 1000 and < 2000 && row.RoleSavedValueList.Count > 0)
            .OrderBy(row => row.ID)
            .ToList();
        CampPool = GameData.GridFightCampData.Values
            .Where(row => row.SeasonID == CurrentSeason && row.IfRandomEnabled != 0 && row.MonsterList.Count > 0)
            .Where(row => row.MonsterList.All(GameData.GridFightMonsterData.ContainsKey))
            .Where(row => row.MonsterList.Any(id => GameData.GridFightMonsterData[id].MonsterTier >= 5))
            .OrderBy(row => row.ID)
            .ToList();
        var rewardCampCodes = StandardRoute
            .Where(node => node.NodeType == GridFightNodeTypeEnum.Monster)
            .SelectMany(node => node.Parameters.Take(1))
            .Select(parameter => parameter / 100)
            .Where(code => code != 0)
            .ToHashSet();
        RewardCampPool = GameData.GridFightCampData.Values
            .Where(row => row.SeasonID == CurrentSeason && row.IfRandomEnabled == 0)
            .Where(row => rewardCampCodes.Contains(row.InitialRandomCode))
            .Where(row => row.MonsterList.Count > 0 && row.MonsterList.All(GameData.GridFightMonsterData.ContainsKey))
            .OrderBy(row => row.ID)
            .ToList();

        var bannedPortals = GameData.GridFightModuleBanPortalData
            .Where(row => row.ModuleId == ContentModuleId)
            .Select(row => row.BanPortalId)
            .ToHashSet();
        PortalPool = GameData.GridFightSeasonPortalData.GetValueOrDefault(CurrentSeason, [])
            .Where(id => !bannedPortals.Contains(id))
            .Distinct()
            .Order()
            .ToList();

        var bannedAugments = GameData.GridFightModuleBanAugmentData
            .Where(row => row.ModuleId == ContentModuleId)
            .Select(row => row.BanAugmentId)
            .ToHashSet();
        AugmentPool = GameData.GridFightSeasonAugmentData.GetValueOrDefault(CurrentSeason, [])
            .Where(id => !bannedAugments.Contains(id))
            .Distinct()
            .Order()
            .ToList();
        BasicEquipmentPool = GameData.GridFightEquipmentData.Values
            .Where(row => row.EquipCategory == GridFightEquipCategoryEnum.Basic)
            .Select(row => row.ID)
            .Distinct()
            .Order()
            .ToList();

        InitialExcludedRoleIds = GetUIntList("GridFight_InitialExceptRoleIdList").ToHashSet();
        InitialGold = ResolveInitialGold();
        TutorialCompletion = GameData.GridFightTutorialStageNodeData.Max(row => row.Unlock);
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (HighestDivision.Progress == 0) errors.Add("highest division progress is zero");
        if (SeasonExpLimit == 0) errors.Add("season exp limit is zero");
        if (InitialEquipmentIds.Count == 0) errors.Add("initial equipment list is empty");
        if (InitialEquipmentIds.Any(id => !GameData.GridFightEquipmentData.ContainsKey(id)))
            errors.Add("initial equipment list references missing equipment");
        if (RolePool.Count == 0) errors.Add("role pool is empty");
        if (RolePool.Any(row => row.AvatarID is < 1000 or >= 2000 || row.RoleSavedValueList.Count == 0))
            errors.Add("role pool contains an unusable runtime role");
        if (RolePool.Count(row => !InitialExcludedRoleIds.Contains(row.ID)) < InitialRoleCount)
            errors.Add("initial role pool is too small");
        if (CampPool.Count == 0) errors.Add("monster camp pool is empty");
        if (RewardCampPool.Count == 0) errors.Add("reward monster camp pool is empty");
        if (PortalPool.Count == 0) errors.Add("portal pool is empty");
        if (AugmentPool.Count == 0) errors.Add("augment pool is empty");
        if (Enum.GetValues<GridFightAugmentQualityEnum>()
            .Where(quality => quality != GridFightAugmentQualityEnum.None)
            .Any(quality => AugmentPool.Count(id => GameData.GridFightAugmentData[id].Quality == quality) < 3))
            errors.Add("augment quality pool has fewer than three entries");
        if (BasicEquipmentPool.Count == 0) errors.Add("basic equipment pool is empty");
        if (RolePool.Count < 5) errors.Add("supply role pool has fewer than five entries");
        if (StandardRoute.Count < 2) errors.Add("standard route has fewer than two nodes");
        if (!StandardRoute.Any(node => node.ChapterId == 1 && node.SectionId == 1))
            errors.Add("standard route has no 1-1 node");
        if (!StandardRoute.Any(node => node.ChapterId == 1 && node.SectionId == 2))
            errors.Add("standard route has no 1-2 node");
        if (StandardRoute.Any(node => node.StageId == 0)) errors.Add("standard route has missing stage ids");
        if (StandardRoute.Any(node => !GameData.StageConfigData.ContainsKey((int)node.StageId)))
            errors.Add("standard route references missing stage configs");
        if (GameData.GridFightPlayerLevelData.Count == 0) errors.Add("player level table is empty");
        if (GameData.GridFightShopPriceData.Count == 0) errors.Add("shop price table is empty");
        if (!GameData.GridFightDivisionStageData.ContainsKey(HighestDivision.ID))
            errors.Add("highest division has no stage difficulty row");
        if (GameData.GridFightCampData.Values.All(row => row.SeasonID != CurrentSeason))
            errors.Add("current season has no monster camp");
        return errors;
    }

    public GridFightRouteNode GetNode(uint chapterId, uint sectionId)
    {
        return StandardRoute.Single(node => node.ChapterId == chapterId && node.SectionId == sectionId);
    }

    public GridFightRouteNode? GetNextNode(uint chapterId, uint sectionId)
    {
        var index = StandardRoute.ToList().FindIndex(node =>
            node.ChapterId == chapterId && node.SectionId == sectionId);
        return index >= 0 && index + 1 < StandardRoute.Count ? StandardRoute[index + 1] : null;
    }

    public GridFightPlayerLevelExcel GetPlayerLevel(uint level)
    {
        return GameData.GridFightPlayerLevelData[level];
    }

    public uint GetEnemyDifficultyLevel(uint divisionId)
    {
        return GameData.GridFightDivisionStageData[divisionId].EnemyDifficultyLevel;
    }

    public uint GetBuyPrice(uint roleId, uint star = 1)
    {
        var role = GameData.GridFightRoleBasicInfoData[roleId];
        var prices = GameData.GridFightShopPriceData[role.Rarity].BuyGoldList;
        return prices[(int)Math.Clamp(star, 1u, (uint)prices.Count) - 1];
    }

    public uint GetSellPrice(uint roleId, uint star)
    {
        var role = GameData.GridFightRoleBasicInfoData[roleId];
        var prices = GameData.GridFightShopPriceData[role.Rarity].SellGoldList;
        return prices[(int)Math.Clamp(star, 1u, (uint)prices.Count) - 1];
    }

    public IReadOnlyList<GridFightRoleBasicInfoExcel> RollShop(uint level)
    {
        var count = checked((int)ShopSize);
        var weights = GetUIntList($"GridFight_CardWeight_Lv{level}");
        if (weights.Count == 0) weights = GetPlayerLevel(level).RarityWeights;
        var result = new List<GridFightRoleBasicInfoExcel>(count);
        for (var index = 0; index < count; index++)
        {
            var rarity = RollIndex(weights) + 1u;
            var candidates = RolePool.Where(role => role.Rarity == rarity).ToList();
            if (candidates.Count == 0) candidates = RolePool.ToList();
            result.Add(candidates[_random.Next(candidates.Count)]);
        }
        return result;
    }

    public IReadOnlyList<GridFightRoleBasicInfoExcel> RollInitialRoles(int count)
    {
        var candidates = RolePool.Where(role => !InitialExcludedRoleIds.Contains(role.ID)).ToList();
        return TakeRandomDistinct(candidates, count);
    }

    public IReadOnlyList<uint> RollPortalOffer(int count)
    {
        return TakeRandomDistinct(PortalPool.ToList(), count);
    }

    public GridFightAugmentQualityEnum RollInvestmentQuality()
    {
        var qualities = Enum.GetValues<GridFightAugmentQualityEnum>()
            .Where(quality => quality != GridFightAugmentQualityEnum.None)
            .Where(quality => AugmentPool.Count(id => GameData.GridFightAugmentData[id].Quality == quality) >= 3)
            .ToList();
        return qualities[_random.Next(qualities.Count)];
    }

    public IReadOnlyList<uint> RollInvestmentOffer(
        GridFightAugmentQualityEnum quality,
        int count,
        IEnumerable<uint>? exclude = null)
    {
        var excluded = exclude?.ToHashSet() ?? [];
        var candidates = AugmentPool
            .Where(id => GameData.GridFightAugmentData[id].Quality == quality && !excluded.Contains(id))
            .ToList();
        return TakeRandomDistinct(candidates, count);
    }

    public IReadOnlyList<(uint RoleId, uint EquipmentId)> RollSupplyOffer(int count)
    {
        var roles = TakeRandomDistinct(RolePool.ToList(), count);
        return roles.Select(role =>
            (role.ID, BasicEquipmentPool[_random.Next(BasicEquipmentPool.Count)])).ToList();
    }

    public uint GetInvestmentDifficultyAdd(uint divisionId, uint augmentId)
    {
        if (!GameData.GridFightDivisionInfoData.TryGetValue(divisionId, out var division) ||
            !GameData.GridFightAugmentData.TryGetValue(augmentId, out var augment) ||
            !GameData.GridFightAugmentMonsterData.TryGetValue(division.DivisionLevel, out var byQuality) ||
            !byQuality.TryGetValue(augment.Quality, out var difficulty))
            return 0;
        return difficulty.EnemyDiffLvAdd;
    }

    public IReadOnlyList<uint> RollCampIds(int count)
    {
        return TakeRandomDistinct(CampPool.ToList(), count).Select(camp => camp.ID).ToList();
    }

    public uint RollRewardCampId()
    {
        return RewardCampPool[_random.Next(RewardCampPool.Count)].ID;
    }

    public GridFightMonsterExcel RollBossMonster(uint campId)
    {
        var candidates = GameData.GridFightCampData[campId].MonsterList
            .Select(id => GameData.GridFightMonsterData[id])
            .Where(monster => monster.MonsterTier >= 5)
            .ToList();
        return candidates[_random.Next(candidates.Count)];
    }

    public IReadOnlyList<uint> RollAffixIds(uint divisionId)
    {
        var counts = GameData.GridFightDivisionStageData[divisionId].AffixChooseNumList;
        var result = new List<uint>();
        for (var index = 0; index < counts.Count; index++)
        {
            var group = (uint)index + 1;
            var candidates = GameData.GridFightAffixConfigData.Values
                .Where(affix => affix.ID / 1000 == group)
                .OrderBy(affix => affix.ID)
                .ToList();
            result.AddRange(TakeRandomDistinct(candidates, checked((int)counts[index])).Select(affix => affix.ID));
        }
        return result;
    }

    public IReadOnlyList<GridFightMonsterExcel> RollEnemyRoster(uint campId, GridFightRouteNode node)
    {
        if (node.NodeType is GridFightNodeTypeEnum.GridFightNodeNone or GridFightNodeTypeEnum.Supply)
            return [];
        if (!GameData.GridFightCampData.TryGetValue(campId, out var camp)) return [];
        var candidates = camp.MonsterList
            .Select(id => GameData.GridFightMonsterData.GetValueOrDefault(id))
            .OfType<GridFightMonsterExcel>()
            .ToList();
        if (candidates.Count == 0) return [];
        var lowTier = candidates.Where(monster => monster.MonsterTier == 1).ToList();
        if (node.NodeType == GridFightNodeTypeEnum.Monster && lowTier.Count > 0) candidates = lowTier;
        var count = node.NodeType == GridFightNodeTypeEnum.Monster
            ? checked((int)InitialPlayerLevel)
            : checked((int)Math.Min(ShopSize, (uint)candidates.Count));
        return Enumerable.Range(0, count)
            .Select(_ => candidates[_random.Next(candidates.Count)])
            .ToList();
    }

    private uint ResolveContentModuleId()
    {
        var moduleIds = GameData.GridFightModuleBanRoleData.Select(row => row.ModuleId)
            .Concat(GameData.GridFightModuleBanPortalData.Select(row => row.ModuleId))
            .Concat(GameData.GridFightModuleBanAugmentData.Select(row => row.ModuleId))
            .Distinct()
            .Order()
            .ToList();
        return moduleIds.Count > 0 ? moduleIds[^1] : ActivityModuleId;
    }

    private uint ResolveStandardRouteId()
    {
        var excluded = GetUIntList("GridFight_TutorialStageList")
            .Select(divisionId => divisionId * 100)
            .ToHashSet();
        excluded.UnionWith(GameData.GridFightTutorialStageData.Keys.Select(id => id * 100));
        excluded.Add(GetUInt("GridFight_FirstPlay_StageRouteID"));

        return GameData.GridFightStageRouteData
            .Where(pair => !excluded.Contains(pair.Key))
            .Where(pair => pair.Value.Values.All(row =>
                GameData.GridFightNodeTemplateData.ContainsKey(row.NodeTemplateID)))
            .OrderBy(pair => pair.Value.Count)
            .ThenBy(pair => pair.Key)
            .Last().Key;
    }

    private static IReadOnlyList<GridFightRouteNode> BuildRoute(uint routeId)
    {
        return GameData.GridFightStageRouteData[routeId].Values
            .OrderBy(row => row.ChapterID)
            .ThenBy(row => row.SectionID)
            .Select(row => new GridFightRouteNode(
                row.ID,
                row.ChapterID,
                row.SectionID,
                GameData.GridFightNodeTemplateData[row.NodeTemplateID]))
            .ToList();
    }

    private uint ResolveInitialGold()
    {
        var poolId = RequireUInt("GridFight_InitRandomBonusPool");
        if (!GameData.GridFightBonusPoolV2Data.TryGetValue(poolId, out var pool)) return 0;
        return pool.BonusList.Any(id => GameData.GridFightBasicBonusPoolV2Data.TryGetValue(id, out var bonus) &&
                                       bonus.BonusType == GridFightBonusTypeEnum.Gold)
            ? pool.TotalValue
            : 0;
    }

    private uint RollIndex(IReadOnlyList<uint> weights)
    {
        var total = weights.Aggregate(0UL, (value, weight) => value + weight);
        if (total == 0) return 0;
        var roll = (ulong)_random.NextInt64((long)total);
        for (var index = 0; index < weights.Count; index++)
        {
            if (roll < weights[index]) return (uint)index;
            roll -= weights[index];
        }
        return (uint)(weights.Count - 1);
    }

    private IReadOnlyList<T> TakeRandomDistinct<T>(List<T> candidates, int count)
    {
        var result = new List<T>(Math.Min(count, candidates.Count));
        while (result.Count < count && candidates.Count > 0)
        {
            var index = _random.Next(candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return result;
    }

    private static uint RequireUInt(string key)
    {
        if (!GameData.GridFightConstValueData.TryGetValue(key, out var value))
            throw new InvalidDataException($"Missing GridFight constant: {key}");
        return value.AsUInt32();
    }

    private static uint GetUInt(string key)
    {
        return GameData.GridFightConstValueData.GetValueOrDefault(key)?.AsUInt32() ?? 0;
    }

    private static IReadOnlyList<uint> GetUIntList(string key)
    {
        return GameData.GridFightConstValueData.GetValueOrDefault(key)?.AsUInt32List() ?? [];
    }
}
