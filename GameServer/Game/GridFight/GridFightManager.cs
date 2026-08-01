using March7thHoney.Data;
using March7thHoney.Enums.Avatar;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed class GridFightManager : BasePlayerManager
{
    private uint _nextSessionUniqueId = 1;
    private readonly Lazy<GridFightResourceCatalog> _catalog;

    public GridFightResourceCatalog Catalog => _catalog.Value;
    public GridFightSession? Session { get; private set; }

    public GridFightManager(PlayerInstance player, Random? random = null) : base(player)
    {
        _catalog = new Lazy<GridFightResourceCatalog>(() =>
        {
            var catalog = new GridFightResourceCatalog(random);
            var errors = catalog.Validate();
            if (errors.Count > 0) throw new InvalidDataException(string.Join("; ", errors));
            return catalog;
        });
    }

    public bool Start(uint season, uint divisionId, bool isOverlock)
    {
        if (season != Catalog.CurrentSeason ||
            !GameData.GridFightDivisionInfoData.TryGetValue(divisionId, out var division) ||
            division.SeasonID != season)
            return false;
        Session = new GridFightSession(Catalog, season, divisionId, isOverlock, _nextSessionUniqueId++);
        return true;
    }

    public void ClearSession()
    {
        Session = null;
    }

    public GridFightSystemInfo BuildSystemInfo()
    {
        var handbook = new GridFightHandBookInfo
        {
            MonsterInfo = new GridFightHandBookMonsterInfo(),
            RoleInfo = new GridFightHandBookRoleInfo(),
            AugmentInfo = new GridFightHandBookAugmentInfo(),
            PortalInfo = new GridFightHandBookPortalInfo(),
            EquipInfo = new GridFightHandBookEquipInfo(),
        };
        handbook.MonsterInfo.MonsterIdList.Add(GameData.GridFightCampData.Values
            .Where(row => row.SeasonID == Catalog.CurrentSeason)
            .Select(row => row.ID)
            .Order());
        handbook.RoleInfo.RoleIdList.Add(GameData.GridFightRoleBasicInfoData.Values
            .Where(row => row.SeasonID == Catalog.CurrentSeason && row.IsInBook)
            .Select(row => row.ID)
            .Order());
        handbook.AugmentInfo.SeenAugmentIdList.Add(Catalog.AugmentPool);
        handbook.AugmentInfo.UnlockedAugmentIdList.Add(Catalog.AugmentPool);
        var portalIds = GameData.GridFightPortalBuffData.Values
            .Where(row => row.IfInBook)
            .Select(row => row.ID)
            .Order()
            .ToList();
        handbook.PortalInfo.SeenPortalIdList.Add(portalIds);
        handbook.PortalInfo.UnlockedPortalIdList.Add(portalIds);
        handbook.EquipInfo.GridFightItemList.Add(GameData.GridFightEquipmentData.Keys.Order());

        var seasonTalent = new GridFightTalentInfo();
        seasonTalent.TalentIdList.Add(GameData.GridFightSeasonTalentData.Values
            .Where(row => row.SeasonID == Catalog.CurrentSeason)
            .Select(row => row.ID)
            .Order());
        var permanentTalent = new GridFightTalentInfo();
        permanentTalent.TalentIdList.Add(GameData.GridFightTalentData.Keys.Order());

        return new GridFightSystemInfo
        {
            StaticInfo = new GridFightStaticGameInfo
            {
                Exp = new GridFightSeasonExpInfo
                {
                    Exp = Catalog.SeasonExpLimit,
                },
                TutorialCompletion = Catalog.TutorialCompletion,
                Handbook = handbook,
                DivisionId = Catalog.HighestDivision.ID,
                Season = Catalog.CurrentSeason,
                SubSeason = Catalog.CurrentSubSeason,
                SeasonTalent = seasonTalent,
            },
            PermanentInfo = new GridFightPermanentGameInfo
            {
                Talent = permanentTalent,
            },
        };
    }

    public GridFightCurrentInfo? BuildCurrentInfo()
    {
        var session = Session;
        if (session == null) return null;
        var current = new GridFightCurrentInfo
        {
            GameData = BuildGameData(),
            IsOverlock = session.IsOverlock,
            DivisionId = session.DivisionId,
            Season = session.Season,
            UniqueId = session.UniqueId,
            ContextData = new GridFightContextData(),
        };
        current.ContextData.InitialEquipmentIdList.Add(Catalog.InitialEquipmentIds);
        var pending = BuildPendingAction();
        if (pending != null) current.PendingAction = pending;
        current.GameInfoList.Add(BuildBasicInfo());
        current.GameInfoList.Add(new GridFightGameInfo { TeamInfo = BuildTeamInfo() });
        current.GameInfoList.Add(new GridFightGameInfo { ItemsInfo = BuildItemsInfo() });
        current.GameInfoList.Add(new GridFightGameInfo { ShopInfo = BuildShopInfo() });
        current.GameInfoList.Add(new GridFightGameInfo { OrbInfo = BuildOrbInfo() });
        current.GameInfoList.Add(new GridFightGameInfo { LevelInfo = BuildLevelInfo() });
        var augmentInfo = new GridFightGameAugmentSync();
        augmentInfo.SyncAugmentInfo.Add(session.ActiveInvestmentIds.Select(BuildInvestment));
        current.GameInfoList.Add(new GridFightGameInfo { AugmentInfo = augmentInfo });
        current.GameInfoList.Add(new GridFightGameInfo { TraitInfo = new GridFightGameTraitInfo() });
        current.GameInfoList.Add(new GridFightGameInfo());
        current.GameInfoList.Add(new GridFightGameInfo
        {
            ContextValueInfo = new GridFightContextValueInfo { ContextValueVersion = 97 },
        });
        current.GameInfoList.Add(new GridFightGameInfo { PrayQuestInfo = new GridFightPrayQuestInfo() });
        return current;
    }

    public GridFightSettleNotify BuildSettleNotify(GridFightSettleReason reason)
    {
        var session = Session ?? throw new InvalidOperationException("GridFight session is not active");
        var context = new GridFightContextData();
        context.InitialEquipmentIdList.Add(Catalog.InitialEquipmentIds);

        var avatars = new GridFightSettleAvatarInfo();
        avatars.GridFightAvatarList.Add(session.Roles
            .Select(role => role.RoleId)
            .Distinct()
            .Order());

        var bosses = new GridFightBossInfo();
        bosses.MonsterList.Add(session.Bosses.Select(monster => new GridFightMonsterInfo
        {
            MonsterId = monster.MonsterID,
            RoleStar = 1,
        }));

        var finish = new GridFightFinishInfo
        {
            Reason = reason,
            ContextData = context,
            AvatarInfo = avatars,
            IsOverlock = session.IsOverlock,
            Basic = new GridFightSettleBasicInfo
            {
                RouteId = Catalog.StandardRouteId,
                ChapterId = session.ChapterId,
                SectionId = session.SectionId,
                DivisionId = session.DivisionId,
                LineupHp = session.LineupHp,
                MaxLineupHp = session.MaxLineupHp,
                BattlesFinished = session.BattlesFinished,
            },
            BossInfo = bosses,
        };
        finish.AffixInfoList.Add(session.AffixIds.Select(id => new GridFightAffixInfo { AffixId = id }));
        finish.GridGameRoleList.Add(session.Roles
            .OrderBy(role => role.Position)
            .ThenBy(role => role.UniqueId)
            .Select(BuildRole));
        finish.SyncAugmentInfo.Add(session.ActiveInvestmentIds.Select(BuildInvestment));
        if (session.SelectedPortalId != 0)
            finish.PortalBuffInfoList.Add(new GridFightGamePortalBuffInfo
            {
                PortalBuffId = session.SelectedPortalId,
            });

        return new GridFightSettleNotify
        {
            DivisionId = session.DivisionId,
            HighestDivisionId = Catalog.HighestDivision.ID,
            FinishInfo = finish,
        };
    }

    private GridFightGameData BuildGameData()
    {
        var levelProgression = new GridFightPlayerLevelProgressionInfo();
        foreach (var row in GameData.GridFightPlayerLevelData.Values.OrderBy(row => row.PlayerLevel))
            levelProgression.LevelUpExpByLevel[row.PlayerLevel] = row.LevelUpExp;

        var data = new GridFightGameData();
        data.GameItemList.Add(new GridFightGameItemInfo
        {
            UniqueId = 4,
            EmptyInfo = new GridFightEmptyGameItemInfo(),
            Descriptor_ = BuildGameItemDescriptor(GridFightGameItemCategory.Type105, 10101),
        });
        data.GameItemList.Add(new GridFightGameItemInfo
        {
            UniqueId = 8,
            PlayerLevelProgression = levelProgression,
            Descriptor_ = BuildGameItemDescriptor(GridFightGameItemCategory.Type105, 10601),
        });
        data.GameItemList.Add(new GridFightGameItemInfo
        {
            UniqueId = 9,
            RoleHistoryInfo = new GridFightRoleHistoryInfo(),
            Descriptor_ = BuildGameItemDescriptor(GridFightGameItemCategory.Type5, 0),
        });
        data.GameItemList.Add(new GridFightGameItemInfo
        {
            UniqueId = 19,
            SingleValueInfo = new GridFightSingleValueItemInfo(),
            Descriptor_ = BuildGameItemDescriptor(GridFightGameItemCategory.Type111, 8009),
        });
        data.GameItemList.Add(new GridFightGameItemInfo
        {
            UniqueId = 56,
            CountInfo = new GridFightCountItemInfo { Count = 2 },
            Descriptor_ = BuildGameItemDescriptor(GridFightGameItemCategory.Type107, 3011),
        });
        data.GameItemList.Add(new GridFightGameItemInfo
        {
            UniqueId = 94,
            CountInfo = new GridFightCountItemInfo { Count = 1 },
            Descriptor_ = BuildGameItemDescriptor(GridFightGameItemCategory.Type107, 4061),
        });
        if (Session?.HackConsole is { } hackConsole)
            data.GameItemList.Add(BuildHackConsoleItem(hackConsole));
        return data;
    }

    private static GridFightGameItemDescriptor BuildGameItemDescriptor(GridFightGameItemCategory category, uint parameter)
    {
        var descriptor = new GridFightGameItemDescriptor { Category = category };
        descriptor.ParameterList.Add(parameter);
        return descriptor;
    }

    public GridFightPendingAction? BuildPendingAction()
    {
        var session = Session;
        if (session == null) return null;
        if (session.TraitEffectSelectEnhanceQueuePosition is { } traitQueuePosition)
        {
            var action = new GridFightTraitEffectSelectEnhanceActionInfo
            {
                TraitId = GridFightSpecialFeatures.SilverWolfTraitId,
                EffectId = GridFightSpecialFeatures.SilverWolfTraitEffectId,
                IsOptional = false,
            };
            action.EnhanceIndexList.Add(0);
            action.EnhanceOptionIdList.Add(GridFightSpecialFeatures.SilverWolfEnhanceOptionId);
            return new GridFightPendingAction
            {
                QueuePosition = traitQueuePosition,
                TraitEffectSelectEnhanceAction = action,
            };
        }
        var pending = new GridFightPendingAction { QueuePosition = session.PendingQueuePosition };
        switch (session.Phase)
        {
            case GridFightSessionPhase.SelectingPortal:
                pending.PortalAction = new GridFightPortalBuffActionInfo
                {
                    FreeRerollCount = 1,
                    SelectCount = 1,
                };
                pending.PortalAction.PortalIdList.Add(session.PortalOffer);
                break;
            case GridFightSessionPhase.AwaitingInitialRound:
            case GridFightSessionPhase.AwaitingBattleRound:
            case GridFightSessionPhase.AwaitingSupplyRound:
                pending.RoundBeginAction = new GridFightRoundBeginActionInfo();
                break;
            case GridFightSessionPhase.SelectingInvestment:
                pending.InvestmentAction = new GridFightAugmentActionInfo();
                pending.InvestmentAction.PendingInvestmentList.Add(session.InvestmentOffer.Select(augmentId =>
                    new GridFightPendingAugmentInfo
                    {
                        AugmentId = augmentId,
                        FreeRerollCount = 1,
                        RerollCount = session.IsInvestmentRerolled(augmentId) ? 1u : 0u,
                    }));
                break;
            case GridFightSessionPhase.SelectingSupply:
                pending.SupplyAction = new GridFightSupplyActionInfo
                {
                    SelectCount = 1,
                    FreeRerollCount = 1,
                    RerollCount = session.SupplyRerollCount,
                };
                pending.SupplyAction.SupplyRoleInfoList.Add(session.SupplyOffer.Select(offer =>
                {
                    var role = new GridFightSupplyRoleInfo { RoleId = offer.RoleId };
                    role.GridFightItemList.Add(offer.EquipmentId);
                    return role;
                }));
                break;
            case GridFightSessionPhase.SelectingEncounter:
                pending.EliteBranchAction = new GridFightEliteBranchActionInfo
                {
                    FreeRerollCount = 1,
                    RerollCount = session.EncounterRerollCount,
                };
                break;
            case GridFightSessionPhase.AwaitingInitialOrb:
            case GridFightSessionPhase.AwaitingInitialReturn:
            case GridFightSessionPhase.AwaitingBattleReturn:
                pending.ReturnPreparationAction = new GridFightReturnPreparationActionInfo();
                break;
            default:
                return null;
        }
        return pending;
    }

    public GridFightSyncUpdateResultScNotify BuildSync(
        GridFightUpdateSrcType source,
        GridFightSessionChange? change = null,
        bool includeGold = false,
        bool includeShop = false,
        bool includeLevel = false,
        bool levelChanged = false,
        bool includePending = false,
        bool includeRoute = false)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var session = Session;
        if (session == null) return notify;
        var result = new GridFightSyncResultData { UpdateSource = source };
        if (includeGold) result.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = session.Gold });
        if (levelChanged)
            result.UpdateDynamicList.Add(new GridFightSyncData { MaxActiveRoleCount = session.MaxActiveRoles });
        if (includeShop) result.UpdateDynamicList.Add(new GridFightSyncData { ShopInfo = BuildShopSyncInfo() });
        if (includeLevel)
            result.UpdateDynamicList.Add(new GridFightSyncData
            {
                PlayerLevel = new GridFightPlayerLevelSyncInfo
                {
                    Level = session.PlayerLevel,
                    Exp = session.PlayerExp,
                    MaxLevel = Catalog.MaxPlayerLevel,
                },
            });
        if (includePending && BuildPendingAction() is { } pending)
            result.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = pending });
        if (includeRoute)
            result.UpdateDynamicList.Add(new GridFightSyncData
            {
                LevelInfo = new GridFightLevelSyncInfo
                {
                    ChapterId = session.ChapterId,
                    SectionId = session.SectionId,
                    LayerInfo = BuildLayerInfo(),
                },
            });
        if (change != null) AppendRoleChanges(result, change);
        notify.ResultList.Add(result);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildGoldSync()
    {
        return BuildSync(GridFightUpdateSrcType.KgridFightSrcNone, includeGold: true);
    }

    public GridFightSyncUpdateResultScNotify BuildRoleAddedSync(GridFightSessionChange change)
    {
        return BuildSync(GridFightUpdateSrcType.KgridFightSrcNone, change);
    }

    public GridFightSyncUpdateResultScNotify BuildRoleAcquisitionSync(
        GridFightRoleAcquisitionResult acquisition)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null || !acquisition.Success) return notify;
        var result = new GridFightSyncResultData();
        AppendAcquiredRoles(result, acquisition);
        notify.ResultList.Add(result);
        AppendRoleMergeResults(notify, GridFightUpdateSrcType.KgridFightSrcNone, [], acquisition);
        AppendUnlockEffects(notify, acquisition);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildEquipmentAddedSync(GridFightEquipmentState equipment)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;
        var items = new GridFightGameItemSyncInfo();
        items.GridFightEquipmentList.Add(BuildEquipment(equipment));
        var result = new GridFightSyncResultData();
        result.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = items });
        notify.ResultList.Add(result);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildOrbAddedSync(GridFightOrbState orb)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;
        var result = new GridFightSyncResultData();
        result.UpdateDynamicList.Add(new GridFightSyncData
        {
            OrbInfo = new GridFightOrbSyncInfo
            {
                UniqueId = orb.UniqueId,
                OrbItemId = orb.OrbItemId,
            },
        });
        notify.ResultList.Add(result);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildConsumableSync(
        GridFightConsumableState consumable,
        bool isNew)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;
        var items = new GridFightGameItemSyncInfo();
        items.UpdateGridFightConsumableList.Add(new GridFightConsumableUpdateInfo
        {
            ItemId = consumable.ItemId,
            ItemStackCount = 1,
            Num = consumable.Count,
        });
        var result = new GridFightSyncResultData();
        result.UpdateDynamicList.Add(isNew
            ? new GridFightSyncData { AddGameItemInfo = items }
            : new GridFightSyncData { UpdateGameItemInfo = items });
        notify.ResultList.Add(result);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildSectionMoveSync(uint finishedPendingPosition)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;
        var finishResult = new GridFightSyncResultData();
        if (finishedPendingPosition != 0)
            finishResult.UpdateDynamicList.Add(new GridFightSyncData
            {
                FinishPendingActionPos = finishedPendingPosition,
            });
        finishResult.UpdateDynamicList.Add(new GridFightSyncData { LockInfo = new GridFightLockInfo() });
        notify.ResultList.Add(finishResult);

        var section = BuildSync(
            GridFightUpdateSrcType.KgridFightSrcEnterNode,
            includeShop: true,
            includeRoute: true);
        notify.ResultList.Add(section.ResultList);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildBuyGoodsSync(
        IReadOnlyList<uint> goodsIndices,
        GridFightRoleAcquisitionResult acquisition)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var session = Session;
        if (session == null || !acquisition.Success) return notify;

        var purchaseResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcBuyGoods,
        };
        purchaseResult.EffectParamList.Add(goodsIndices);
        purchaseResult.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = session.Gold });
        AppendAcquiredRoles(purchaseResult, acquisition);
        notify.ResultList.Add(purchaseResult);
        AppendRoleMergeResults(
            notify,
            GridFightUpdateSrcType.KgridFightSrcBuyGoods,
            goodsIndices,
            acquisition);
        AppendUnlockEffects(notify, acquisition);

        var shopResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcBuyGoods,
        };
        shopResult.EffectParamList.Add(goodsIndices);
        shopResult.UpdateDynamicList.Add(new GridFightSyncData { ShopInfo = BuildShopSyncInfo() });
        notify.ResultList.Add(shopResult);
        notify.ResultList.Add(new GridFightSyncResultData());
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildRefreshShopSync()
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var session = Session;
        if (session == null) return notify;

        var refreshResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcManualRefreshGoods,
        };
        refreshResult.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = session.Gold });
        refreshResult.UpdateDynamicList.Add(new GridFightSyncData { ShopInfo = BuildShopSyncInfo() });
        notify.ResultList.Add(refreshResult);
        notify.ResultList.Add(new GridFightSyncResultData());
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildSpecialGoodsSync(
        IReadOnlyList<GridFightSpecialGoodsPurchaseResult> purchases)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null || purchases.Count == 0) return notify;
        foreach (var purchase in purchases)
        {
            var result = new GridFightSyncResultData
            {
                UpdateSource = GridFightUpdateSrcType.KgridFightSrcSpecialGoods,
            };
            result.EffectParamList.Add(purchase.SpecialGoodsId);
            result.EffectParamList.Add(purchase.GoodsIndex);
            AppendRewardApplication(result, purchase.Application);
            foreach (var acquisition in purchase.Application.RoleAcquisitions)
                AppendAcquiredRoles(result, acquisition);
            notify.ResultList.Add(result);
            foreach (var acquisition in purchase.Application.RoleAcquisitions)
            {
                AppendRoleMergeResults(
                    notify,
                    GridFightUpdateSrcType.KgridFightSrcSpecialGoods,
                    [purchase.SpecialGoodsId, purchase.GoodsIndex],
                    acquisition);
                AppendUnlockEffects(notify, acquisition);
            }
        }

        var shopResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcBuyGoods,
        };
        shopResult.EffectParamList.Add(purchases.Select(purchase => purchase.GoodsIndex));
        shopResult.UpdateDynamicList.Add(new GridFightSyncData { ShopInfo = BuildShopSyncInfo() });
        notify.ResultList.Add(shopResult);
        notify.ResultList.Add(new GridFightSyncResultData());
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildOrbOpenSync(GridFightRewardApplication application)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null || !application.Success) return notify;
        var result = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcUseOrb,
        };
        result.EffectParamList.Add(application.SourceUniqueId);
        result.EffectParamList.Add(application.SourceItemId);
        result.UpdateDynamicList.Add(new GridFightSyncData
        {
            RemoveOrbUniqueId = application.SourceUniqueId,
        });
        AppendRewardApplication(result, application);
        foreach (var acquisition in application.RoleAcquisitions)
            AppendAcquiredRoles(result, acquisition);
        notify.ResultList.Add(result);
        foreach (var acquisition in application.RoleAcquisitions)
        {
            AppendRoleMergeResults(
                notify,
                GridFightUpdateSrcType.KgridFightSrcUseOrb,
                [application.SourceUniqueId, application.SourceItemId],
                acquisition);
            AppendUnlockEffects(notify, acquisition);
        }
        return notify;
    }

    public GridFightOpenOrbResultScNotify BuildOpenOrbResult(GridFightRewardApplication application)
    {
        var result = new GridFightOpenOrbResultScNotify
        {
            UniqueId = application.SourceUniqueId,
            DropInfo = new GridFightDropInfo(),
        };
        result.DropInfo.DropList.Add(application.Drops.Select(BuildDropEntry));
        return result;
    }

    public GridFightSyncUpdateResultScNotify BuildTraitEnhanceActivationSync(
        GridFightTraitEnhanceActivationResult activation)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session?.HackConsole == null || !activation.Success || activation.Equipment == null) return notify;
        var result = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcTraitEffectSelectEnhance,
        };
        result.EffectParamList.Add(GridFightSpecialFeatures.SilverWolfEnhanceOptionId);
        var items = new GridFightGameItemSyncInfo();
        items.GridFightEquipmentList.Add(BuildEquipment(activation.Equipment));
        result.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = items });
        result.UpdateDynamicList.Add(new GridFightSyncData
        {
            GameDataItemInfo = BuildHackConsoleItem(Session.HackConsole),
        });
        result.UpdateDynamicList.Add(new GridFightSyncData
        {
            FinishPendingActionPos = activation.FinishedQueuePosition,
        });
        result.UpdateDynamicList.Add(new GridFightSyncData { LockInfo = new GridFightLockInfo() });
        notify.ResultList.Add(result);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildHackOptionSync(GridFightHackOptionResult option)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session?.HackConsole == null || !option.Success) return notify;
        var result = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcTraitEffectSelectEnhance,
        };
        result.EffectParamList.Add(GridFightSpecialFeatures.SilverWolfEnhanceOptionId);
        result.EffectParamList.Add(Session.HackConsole.GameItemUniqueId);
        AppendRewardApplication(result, option.Application);
        result.UpdateDynamicList.Add(new GridFightSyncData
        {
            GameDataItemInfo = BuildHackConsoleItem(Session.HackConsole),
        });
        notify.ResultList.Add(result);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildPreparationSync()
    {
        var notify = BuildSync(GridFightUpdateSrcType.KgridFightSrcNone, includePending: true);
        if (Session?.HackConsole is not { } hackConsole) return notify;
        var result = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcTraitEffectSelectEnhance,
        };
        result.UpdateDynamicList.Add(new GridFightSyncData
        {
            GameDataItemInfo = BuildHackConsoleItem(hackConsole),
        });
        notify.ResultList.Add(result);
        return notify;
    }

    private void AppendRewardApplication(
        GridFightSyncResultData result,
        GridFightRewardApplication application)
    {
        var session = Session!;
        if (application.GoldChanged)
            result.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = session.Gold });
        if (application.PlayerLevelChanged || application.PlayerExpChanged)
            result.UpdateDynamicList.Add(new GridFightSyncData
            {
                PlayerLevel = new GridFightPlayerLevelSyncInfo
                {
                    Level = session.PlayerLevel,
                    Exp = session.PlayerExp,
                    MaxLevel = Catalog.MaxPlayerLevel,
                },
            });
        if (application.PlayerLevelChanged)
            result.UpdateDynamicList.Add(new GridFightSyncData { MaxActiveRoleCount = session.MaxActiveRoles });
        if (application.LineupHpChanged)
            result.UpdateDynamicList.Add(new GridFightSyncData
            {
                LineupHp = new GridFightLineupHpSyncInfo
                {
                    CurrentHp = session.LineupHp,
                    MaxHp = session.MaxLineupHp,
                },
            });
        if (application.AddedEquipments.Count > 0)
        {
            var items = new GridFightGameItemSyncInfo();
            items.GridFightEquipmentList.Add(application.AddedEquipments.Select(BuildEquipment));
            result.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = items });
        }
        foreach (var change in application.ConsumableChanges)
        {
            var items = new GridFightGameItemSyncInfo();
            items.UpdateGridFightConsumableList.Add(new GridFightConsumableUpdateInfo
            {
                ItemId = change.Consumable.ItemId,
                ItemStackCount = 1,
                Num = change.Consumable.Count,
            });
            result.UpdateDynamicList.Add(change.IsNew
                ? new GridFightSyncData { AddGameItemInfo = items }
                : new GridFightSyncData { UpdateGameItemInfo = items });
        }
        foreach (var orb in application.AddedOrbs)
            result.UpdateDynamicList.Add(new GridFightSyncData
            {
                OrbInfo = new GridFightOrbSyncInfo
                {
                    UniqueId = orb.UniqueId,
                    OrbItemId = orb.OrbItemId,
                },
            });
        foreach (var forgeItem in application.AddedForgeItems)
            result.UpdateDynamicList.Add(new GridFightSyncData { AddForgeItemInfo = BuildForgeItem(forgeItem) });
        if (application.RolePropertiesChanged)
            foreach (var role in session.Roles)
                result.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRole(role) });
    }

    private static GridFightDropEntry BuildDropEntry(GridFightRewardEntry reward)
    {
        var entry = new GridFightDropEntry
        {
            DropType = reward.Type switch
            {
                Enums.GridFight.GridFightBonusTypeEnum.Gold => GridFightDropType.Coin,
                Enums.GridFight.GridFightBonusTypeEnum.Exp => GridFightDropType.Exp,
                Enums.GridFight.GridFightBonusTypeEnum.SpecificAvatar => GridFightDropType.Role,
                Enums.GridFight.GridFightBonusTypeEnum.Item => GridFightDropType.Item,
                Enums.GridFight.GridFightBonusTypeEnum.Orb => GridFightDropType.Orb,
                _ => GridFightDropType.None,
            },
            ItemId = reward.ItemId,
            Num = reward.Count,
        };
        if (reward.RoleStar != 0)
            entry.Param = new GridFightDropParam { RoleStar = reward.RoleStar };
        return entry;
    }

    private void AppendRoleChanges(GridFightSyncResultData result, GridFightSessionChange change)
    {
        foreach (var role in change.AddedRoles)
            result.UpdateDynamicList.Add(new GridFightSyncData { AddRoleInfo = BuildRole(role) });
        foreach (var uniqueId in change.RemovedRoleIds)
            result.UpdateDynamicList.Add(new GridFightSyncData { RemoveRoleUniqueId = uniqueId });
        foreach (var role in change.UpdatedRoles)
            result.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRole(role) });
    }

    private void AppendAcquiredRoles(
        GridFightSyncResultData result,
        GridFightRoleAcquisitionResult acquisition)
    {
        foreach (var role in acquisition.AcquiredRoles)
            result.UpdateDynamicList.Add(new GridFightSyncData { AddRoleInfo = BuildRole(role) });
    }

    private void AppendRoleMergeResults(
        GridFightSyncUpdateResultScNotify notify,
        GridFightUpdateSrcType acquisitionSource,
        IEnumerable<uint> effectParameters,
        GridFightRoleAcquisitionResult acquisition)
    {
        foreach (var step in acquisition.MergeSteps)
        {
            var mergeResult = new GridFightSyncResultData
            {
                UpdateSource = GridFightUpdateSrcType.KgridFightSrcMergeRole,
            };
            foreach (var uniqueId in step.RemovedRoleIds)
                mergeResult.UpdateDynamicList.Add(new GridFightSyncData { RemoveRoleUniqueId = uniqueId });
            mergeResult.UpdateDynamicList.Add(new GridFightSyncData { AddRoleInfo = BuildRole(step.AddedRole) });
            notify.ResultList.Add(mergeResult);
        }

        if (acquisition.FinalMergedRoles.Count == 0) return;
        var finalResult = new GridFightSyncResultData { UpdateSource = acquisitionSource };
        finalResult.EffectParamList.Add(effectParameters);
        foreach (var role in acquisition.FinalMergedRoles)
            finalResult.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRole(role) });
        notify.ResultList.Add(finalResult);
    }

    private void AppendUnlockEffects(
        GridFightSyncUpdateResultScNotify notify,
        GridFightRoleAcquisitionResult acquisition)
    {
        foreach (var effect in acquisition.UnlockEffects)
        {
            var result = new GridFightSyncResultData
            {
                UpdateSource = effect == GridFightRoleUnlockEffect.CyrenePoemShop
                    ? GridFightUpdateSrcType.KgridFightSrcSpecialGoods
                    : GridFightUpdateSrcType.KgridFightSrcTraitEffectSelectEnhance,
            };
            if (effect == GridFightRoleUnlockEffect.CyrenePoemShop)
            {
                result.UpdateDynamicList.Add(new GridFightSyncData { ShopInfo = BuildShopSyncInfo() });
            }
            else
            {
                result.UpdateDynamicList.Add(new GridFightSyncData
                {
                    LockInfo = new GridFightLockInfo
                    {
                        LockType = GridFightLockType.Gameplay,
                        LockReason = GridFightLockReason.PendingAction,
                    },
                });
                if (BuildPendingAction() is { } pending)
                    result.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = pending });
            }
            notify.ResultList.Add(result);
        }
    }

    public GridFightSyncUpdateResultScNotify BuildPortalSelectionSync(
        uint finishedQueuePosition,
        GridFightSessionChange change)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var session = Session;
        if (session == null) return notify;

        var portalResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcSelectPortalBuff,
        };
        portalResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            PortalUpdate = new GridFightPortalBuffUpdateInfo { PortalBuffId = session.SelectedPortalId },
        });
        notify.ResultList.Add(portalResult);

        var supplyResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcInitialSupplySelect,
        };
        supplyResult.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = session.Gold });
        foreach (var role in change.AddedRoles)
            supplyResult.UpdateDynamicList.Add(new GridFightSyncData { AddRoleInfo = BuildRole(role) });
        notify.ResultList.Add(supplyResult);

        var finishResult = new GridFightSyncResultData();
        finishResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            FinishPendingActionPos = finishedQueuePosition,
        });
        finishResult.UpdateDynamicList.Add(new GridFightSyncData { LockInfo = new GridFightLockInfo() });
        notify.ResultList.Add(finishResult);

        var levelResult = new GridFightSyncResultData();
        levelResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            LevelInfo = new GridFightLevelSyncInfo
            {
                ChapterId = session.ChapterId,
                SectionId = session.SectionId,
                LayerInfo = BuildLayerInfo(),
            },
        });
        notify.ResultList.Add(levelResult);

        var pendingResult = new GridFightSyncResultData();
        pendingResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            LockInfo = new GridFightLockInfo
            {
                LockType = GridFightLockType.Gameplay,
                LockReason = GridFightLockReason.PendingAction,
            },
        });
        if (BuildPendingAction() is { } pending)
            pendingResult.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = pending });
        notify.ResultList.Add(pendingResult);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildPendingAdvanceSync(uint finishedQueuePosition)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;

        var finishResult = new GridFightSyncResultData();
        finishResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            FinishPendingActionPos = finishedQueuePosition,
        });
        finishResult.UpdateDynamicList.Add(new GridFightSyncData { LockInfo = new GridFightLockInfo() });
        notify.ResultList.Add(finishResult);

        if (BuildPendingAction() is { } pending)
        {
            var pendingResult = new GridFightSyncResultData();
            pendingResult.UpdateDynamicList.Add(new GridFightSyncData
            {
                LockInfo = new GridFightLockInfo
                {
                    LockType = GridFightLockType.Gameplay,
                    LockReason = GridFightLockReason.PendingAction,
                },
            });
            pendingResult.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = pending });
            notify.ResultList.Add(pendingResult);
        }
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildInitialRoundSync(uint finishedQueuePosition)
    {
        var notify = BuildSync(GridFightUpdateSrcType.KgridFightSrcNone, includeShop: true);
        var pending = BuildPendingAdvanceSync(finishedQueuePosition);
        notify.ResultList.Add(pending.ResultList);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildBattleRoundSync(uint finishedQueuePosition)
    {
        var notify = BuildSync(GridFightUpdateSrcType.KgridFightSrcNone, includeShop: true);
        var pending = BuildPendingAdvanceSync(finishedQueuePosition);
        notify.ResultList.Add(pending.ResultList);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildEncounterRerollSync()
    {
        return BuildSync(
            GridFightUpdateSrcType.KgridFightSrcNone,
            includePending: true,
            includeRoute: true);
    }

    public GridFightSyncUpdateResultScNotify BuildEncounterSelectionSync(
        uint finishedQueuePosition,
        uint eliteBranchId)
    {
        var notify = BuildEncounterSelectionUpdateSync(eliteBranchId);
        notify.ResultList.Add(BuildPendingAdvanceSync(finishedQueuePosition).ResultList);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildEncounterSelectionUpdateSync(uint eliteBranchId)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;
        var selection = new GridFightSyncResultData();
        selection.UpdateDynamicList.Add(new GridFightSyncData
        {
            EliteBranchSyncInfo = new GridFightEliteBranchSyncInfo { EliteBranchId = eliteBranchId },
        });
        notify.ResultList.Add(selection);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildBattleEndRouteSync()
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;
        var routeResult = new GridFightSyncResultData();
        routeResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            LevelInfo = new GridFightLevelSyncInfo
            {
                ChapterId = Session.ChapterId,
                SectionId = Session.SectionId,
                LayerInfo = BuildLayerInfo(),
            },
        });
        notify.ResultList.Add(routeResult);
        if (BuildPendingAction() is { } pending)
        {
            var pendingResult = new GridFightSyncResultData();
            pendingResult.UpdateDynamicList.Add(new GridFightSyncData
            {
                LockInfo = new GridFightLockInfo
                {
                    LockType = GridFightLockType.Gameplay,
                    LockReason = GridFightLockReason.PendingAction,
                },
            });
            pendingResult.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = pending });
            notify.ResultList.Add(pendingResult);
        }
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildInvestmentSelectionSync(
        uint finishedQueuePosition,
        uint augmentId)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;

        var selectionResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcSelectAugment,
        };
        selectionResult.EffectParamList.Add(augmentId);
        selectionResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            AddAugmentInfo = new GridFightGameAugmentAdd
            {
                UpdateAugmentInfo = BuildInvestment(augmentId),
            },
        });
        notify.ResultList.Add(selectionResult);
        notify.ResultList.Add(BuildPendingAdvanceSync(finishedQueuePosition).ResultList);
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildSupplySelectionSync(
        uint finishedQueuePosition,
        uint selectedIndex,
        GridFightSupplySelectionResult selection)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null || !selection.Success || selection.Equipment == null) return notify;

        var selectionResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcSelectSupply,
        };
        selectionResult.EffectParamList.Add(selectedIndex);
        var items = new GridFightGameItemSyncInfo();
        items.GridFightEquipmentList.Add(BuildEquipment(selection.Equipment));
        selectionResult.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = items });
        notify.ResultList.Add(selectionResult);

        var roleResult = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcSelectSupply,
        };
        roleResult.EffectParamList.Add(selectedIndex);
        AppendAcquiredRoles(roleResult, selection.Acquisition);
        notify.ResultList.Add(roleResult);
        AppendRoleMergeResults(
            notify,
            GridFightUpdateSrcType.KgridFightSrcSelectSupply,
            [selectedIndex],
            selection.Acquisition);
        AppendUnlockEffects(notify, selection.Acquisition);

        var finishResult = new GridFightSyncResultData();
        finishResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            FinishPendingActionPos = finishedQueuePosition,
        });
        finishResult.UpdateDynamicList.Add(new GridFightSyncData { LockInfo = new GridFightLockInfo() });
        notify.ResultList.Add(finishResult);

        var routeResult = new GridFightSyncResultData();
        routeResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            LevelInfo = new GridFightLevelSyncInfo
            {
                ChapterId = Session.ChapterId,
                SectionId = Session.SectionId,
                LayerInfo = BuildLayerInfo(),
            },
        });
        notify.ResultList.Add(routeResult);

        if (BuildPendingAction() is { } pending)
        {
            var pendingResult = new GridFightSyncResultData();
            pendingResult.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = pending });
            notify.ResultList.Add(pendingResult);
        }
        return notify;
    }

    public GridFightSyncUpdateResultScNotify BuildPositionSync(IEnumerable<uint> uniqueIds)
    {
        var session = Session;
        var notify = new GridFightSyncUpdateResultScNotify();
        if (session == null) return notify;
        var result = new GridFightSyncResultData();
        foreach (var role in session.Roles.Where(role => uniqueIds.Contains(role.UniqueId)))
            result.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRole(role) });
        notify.ResultList.Add(result);
        return notify;
    }

    public async ValueTask EndBattle(BattleInstance battle, PVEBattleResultCsReq request)
    {
        var session = Session;
        if (session == null) return;
        var win = request.EndStatus == BattleEndStatus.BattleEndWin;
        var chapterId = session.ChapterId;
        var sectionId = session.SectionId;
        var goldReward = win ? session.CurrentNode.BasicGoldReward : 0;
        var previousLevel = session.PlayerLevel;
        var previousExp = session.PlayerExp;
        var previousMaxExp = Catalog.GetPlayerLevel(previousLevel).LevelUpExp;
        if (!session.ResolveBattle(win)) return;

        await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(BuildSync(
            GridFightUpdateSrcType.KgridFightSrcBattleEnd,
            includeGold: true,
            includeLevel: true,
            levelChanged: session.PlayerLevel != previousLevel)));
        await Player.SendPacket(new PacketGridFightEndBattleStageNotify(new GridFightEndBattleStageNotify
        {
            Win = win,
            SectionId = sectionId,
            GoldReward = goldReward,
            LineupHpBefore = session.LineupHp,
            LineupHpAfter = session.LineupHp,
            MaxLineupHp = session.MaxLineupHp,
            ChapterId = chapterId,
            RouteId = Catalog.StandardRouteId,
            ComboCount = win ? 1u : 0,
            FinishProgress = win ? 100u : 0,
            DamageStatistics = new GridFightDamageSttInfo(),
            LevelUpdateInfo = new GridFightLevelUpdateInfo
            {
                AddedExp = win ? Catalog.BattleWinExp : 0,
                PreviousLevelInfo = new GridFightLevelDisplayInfo
                {
                    Level = previousLevel,
                    Exp = previousExp,
                    MaxExp = previousMaxExp,
                },
                CurrentLevelInfo = new GridFightLevelDisplayInfo
                {
                    Level = session.PlayerLevel,
                    Exp = session.PlayerExp,
                    MaxExp = Catalog.GetPlayerLevel(session.PlayerLevel).LevelUpExp,
                },
            },
        }));
        if (win)
            await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(BuildBattleEndRouteSync()));
    }

    private GridFightGameInfo BuildBasicInfo()
    {
        var session = Session!;
        var basic = new GridFightGameBasicInfo
        {
            LineupHp = session.LineupHp,
            MaxLineupHp = session.MaxLineupHp,
            Gold = session.Gold,
            PlayerLevel = session.PlayerLevel,
            LevelExp = session.PlayerExp,
            MaxAvatarCount = Catalog.BenchSize,
            BuyExpCost = Catalog.BuyExpCost,
            MaxInterestGold = Catalog.MaxInterestGold,
            OffFieldMaxCount = Catalog.OffFieldInitialCount,
            MaxActiveRoleCount = session.MaxActiveRoles,
            DepositPerInterest = Catalog.DepositPerInterest,
            BenchOverflowSize = Catalog.BenchOverflowSize,
            CurrentTaskInfo = new GridFightSyncCurrentTaskInfo(),
            LockInfo = new GridFightLockInfo
            {
                LockType = GridFightLockType.Gameplay,
                LockReason = GridFightLockReason.PendingAction,
            },
        };
        basic.ModuleIdList.Add(Catalog.ContentModuleId);
        return new GridFightGameInfo { BasicInfo = basic };
    }

    private GridFightGameShopInfo BuildShopInfo()
    {
        var session = Session!;
        var info = new GridFightGameShopInfo
        {
            RefreshCost = Catalog.RefreshCost,
            RarityWeightInfo = BuildRarityWeights(),
        };
        info.RolePoolList.Add(Catalog.RolePool.Select(role => role.ID));
        info.GoodsList.Add(session.Shop.Select(BuildShopGoods));
        return info;
    }

    private GridFightGameItemsInfo BuildItemsInfo()
    {
        var session = Session!;
        var info = new GridFightGameItemsInfo();
        info.GridFightEquipmentList.Add(session.Equipments.Select(BuildEquipment));
        info.GridFightConsumableList.Add(session.Consumables.Select(consumable => new GridFightConsumableInfo
        {
            ItemId = consumable.ItemId,
            Num = consumable.Count,
        }));
        return info;
    }

    private GridFightGameOrbInfo BuildOrbInfo()
    {
        var info = new GridFightGameOrbInfo();
        info.GridGameOrbList.Add(Session!.Orbs.Select(orb => new GridGameOrbInfo
        {
            UniqueId = orb.UniqueId,
            OrbItemId = orb.OrbItemId,
        }));
        return info;
    }

    private static GridFightEquipmentInfo BuildEquipment(GridFightEquipmentState equipment)
    {
        return new GridFightEquipmentInfo
        {
            UniqueId = equipment.UniqueId,
            GridFightEquipmentId = equipment.EquipmentId,
            Source = equipment.Source,
        };
    }

    private static GridGameForgeItemInfo BuildForgeItem(GridFightForgeItemState forgeItem)
    {
        var info = new GridGameForgeItemInfo
        {
            UniqueId = forgeItem.UniqueId,
            ForgeItemId = forgeItem.ForgeItemId,
            Pos = forgeItem.Position,
        };
        info.ForgeGoodsList.Add(forgeItem.EquipmentChoices.Select(equipmentId =>
            new GridFightForgeGoodsInfo
            {
                EquipmentGoodsInfo = new GridFightForgeEquipmentInfo
                {
                    GridFightEquipmentId = equipmentId,
                },
            }));
        return info;
    }

    private static GridFightGameItemInfo BuildHackConsoleItem(GridFightHackConsoleState state)
    {
        var hackState = new GridFightHackStateInfo
        {
            MinimumEnemyHpPercent = GridFightSpecialFeatures.MinimumEnemyHpPercent,
            MaximumEnemyHpPercent = GridFightSpecialFeatures.MaximumEnemyHpPercent,
            EnemyHpPercent = state.EnemyHpPercent,
            EnhanceOptionId = GridFightSpecialFeatures.SilverWolfEnhanceOptionId,
        };
        hackState.RemainingActionCountList.Add(state.RemainingGoldActions);
        hackState.RemainingActionCountList.Add(state.RemainingHealActions);
        hackState.RemainingActionCountList.Add(state.RemainingOrbActions);
        return new GridFightGameItemInfo
        {
            UniqueId = state.GameItemUniqueId,
            Descriptor_ = BuildGameItemDescriptor(
                GridFightGameItemCategory.HackConsole,
                GridFightSpecialFeatures.SilverWolfEnhanceOptionId),
            HackStateInfo = hackState,
        };
    }

    private static GridGameAugmentInfo BuildInvestment(uint augmentId)
    {
        return new GridGameAugmentInfo
        {
            AugmentId = augmentId,
            IsActive = true,
        };
    }

    private GridFightShopSyncInfo BuildShopSyncInfo()
    {
        var session = Session!;
        var info = new GridFightShopSyncInfo
        {
            RefreshCost = Catalog.RefreshCost,
            RarityWeightInfo = BuildRarityWeights(),
        };
        info.GoodsList.Add(session.Shop.Select(BuildShopGoods));
        return info;
    }

    private GridFightRarityWeightInfo BuildRarityWeights()
    {
        var result = new GridFightRarityWeightInfo();
        var weights = Catalog.GetPlayerLevel(Session!.PlayerLevel).RarityWeights;
        for (var index = 0; index < weights.Count; index++)
            result.EntryList.Add(new GridFightRarityWeightEntry
            {
                Rarity = (uint)index + 1,
                Weight = weights[index],
            });
        return result;
    }

    private static GridFightShopGoodsInfo BuildShopGoods(GridFightShopSlot slot)
    {
        var goods = new GridFightShopGoodsInfo
        {
            SoldOut = slot.SoldOut,
            Price = slot.Price,
        };
        if (slot.SpecialGoodsId != 0)
            goods.SpecialGoodsInfo = new GridFightSpecialGoodsInfo
            {
                SpecialGoodsId = slot.SpecialGoodsId,
            };
        else
            goods.RoleGoodsInfo = new GridFightRoleGoodsInfo
            {
                RoleId = slot.RoleId,
                RoleStar = slot.Star,
            };
        return goods;
    }

    private GridFightLevelInfo BuildLevelInfo()
    {
        var session = Session!;
        var info = new GridFightLevelInfo
        {
            RouteId = Catalog.StandardRouteId,
            ChapterId = session.ChapterId,
            SectionId = session.SectionId,
            SectionProgressInfo = new GridFightSectionProgressInfo(),
            BossInfo = new GridFightBossInfo(),
            BattleStatisticsInfo = new GridFightBattleStatisticsInfo
            {
                DamageStatistics = new GridFightDamageSttInfo(),
            },
        };
        info.CampInfoList.Add(session.CampIds.Select(id => new GridFightCampInfo { CampId = id }));
        info.AffixInfoList.Add(session.AffixIds.Select(id => new GridFightAffixInfo { AffixId = id }));
        info.BossInfo.MonsterList.Add(session.Bosses.Select(monster => new GridFightMonsterInfo
        {
            MonsterId = monster.MonsterID,
            RoleStar = 1,
        }));
        return info;
    }

    private GridFightLayerInfo BuildLayerInfo()
    {
        var session = Session!;
        var data = new GridFightLayerData
        {
            FightCampId = session.BattlePlan?.CampId ??
                          session.EncounterOptions.FirstOrDefault()?.BattlePlan.CampId ?? 0,
            SelectedEliteBranchId = session.CurrentNode.NodeType == Enums.GridFight.GridFightNodeTypeEnum.EliteBranch
                ? session.EliteBranchId
                : 1,
        };
        var plans = session.CurrentNode.NodeType == Enums.GridFight.GridFightNodeTypeEnum.EliteBranch &&
                    session.BattlePlan == null
            ? session.EncounterOptions.Select(option => (option.BattlePlan, Option: option)).ToList()
            : session.BattlePlan == null
                ? []
                : [(session.BattlePlan, (GridFightEncounterOptionState?)null)];
        foreach (var (plan, option) in plans)
        {
            var encounter = new GridFightEncounterInfo
            {
                EncounterId = option?.EncounterId ?? 0,
                EnemyDifficultyAdd = plan.DifficultyAdd,
                PenaltyRuleId = plan.PenaltyRuleId,
            };
            if (option != null)
                encounter.DropInfo = BuildEncounterDropInfo(option.Quality, session.ChapterId);
            foreach (var wavePlan in plan.Waves)
            {
                var wave = new GridFightMonsterWaveInfo { WaveId = wavePlan.WaveId };
                wave.MonsterList.Add(wavePlan.Monsters.Select(monster => new GridFightMonsterInfo
                {
                    MonsterId = monster.Monster.MonsterID,
                    RoleStar = monster.RoleStar,
                }));
                encounter.MonsterWaveList.Add(wave);
            }
            data.EncounterList.Add(encounter);
        }
        return new GridFightLayerInfo { Data = data };
    }

    private static GridFightDropInfo BuildEncounterDropInfo(uint quality, uint chapterId)
    {
        var entry = quality switch
        {
            1 => new GridFightDropEntry
            {
                DropType = GridFightDropType.Coin,
                Num = chapterId + 1,
            },
            2 => new GridFightDropEntry
            {
                DropType = GridFightDropType.Refresh,
                Num = 4,
            },
            4 => new GridFightDropEntry
            {
                DropType = GridFightDropType.Item,
                ItemId = 350701,
                Num = 1,
            },
            _ => null,
        };
        var result = new GridFightDropInfo();
        if (entry != null) result.DropList.Add(entry);
        return result;
    }

    private GridFightGameTeamInfo BuildTeamInfo()
    {
        var team = new GridFightGameTeamInfo();
        team.GridGameRoleList.Add(Session!.Roles.Select(BuildRole));
        team.ForgeItemList.Add(Session.ForgeItems.Select(BuildForgeItem));
        return team;
    }

    private GridGameRoleInfo BuildRole(GridFightRoleState role)
    {
        var info = new GridGameRoleInfo
        {
            Id = role.RoleId,
            UniqueId = role.UniqueId,
            RoleStar = role.Star,
            Pos = role.Position,
        };
        PopulateRolePropertyScales(info.ConvertPropertyToFixpoint, Session?.HasCyreneCombatEnhancement == true);
        foreach (var key in GameData.GridFightRoleBasicInfoData[role.RoleId].RoleSavedValueList)
            info.GridFightValueInitComponent[key] = 0;
        return info;
    }

    internal static void PopulateRolePropertyScales(
        IDictionary<uint, uint> properties,
        bool hasCyreneCombatEnhancement = false)
    {
        properties[32] = 180;
        properties[33] = 480;
        properties[34] = 440;
        properties[44] = 304;
        properties[49] = 180;
        properties[52] = 240;
        properties[53] = 480;
        properties[57] = 240;
        properties[59] = 450;
        properties[1013] = 100;
        properties[1014] = 100;
        properties[1019] = 100;
        properties[1025] = 100;
        if (!hasCyreneCombatEnhancement) return;
        properties[(uint)AvatarPropertyTypeEnum.ExtraAllDamageTypeAddedRatio1] = 1090;
        properties[(uint)AvatarPropertyTypeEnum.ExtraAllDamageReduce] = 990;
    }
}
