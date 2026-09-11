using Google.Protobuf;
using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database.Player;
using March7thHoney.Enums.GridFight;
using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Internationalization;
using March7thHoney.Proto;
using March7thHoney.Util;

// Read the checked-out cache; all gameplay and fixture changes remain in process memory.
var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
ConfigManager.Config.Path.ConfigPath = Path.Combine(repository, "Config");
ConfigManager.Config.Path.LanguagePath = Path.Combine(repository, "Config/Languages");
ConfigManager.Config.Path.LogPath = Path.Combine(repository, "tmp/GridFightRegression/Logs");
I18NManager.LoadLanguage();
if (!ResourceCache.LoadCache()) throw new InvalidDataException("Current resource cache failed validation.");

var failures = new List<string>();
var checks = 0;
void Check(string name, bool success)
{
    checks++;
    Console.WriteLine($"{(success ? "PASS" : "FAIL")} {name}");
    if (!success) failures.Add(name);
}

var catalog = new GridFightResourceCatalog(new Random(731));
var errors = catalog.Validate();
Check("GridFight catalog validates against current cache", errors.Count == 0);
foreach (var error in errors) Console.WriteLine(error);
if (errors.Count > 0) return 1;

GridFightManager NewRun(uint? portalId = null)
{
    // Seeded offers permit selecting every portal without changing production visibility.
    for (var seed = 0; seed < 500; seed++)
    {
        var manager = new GridFightManager(new PlayerInstance(new PlayerData { Uid = 1 }), new Random(seed));
        if (!manager.Start(catalog.CurrentSeason, catalog.HighestDivision.ID, false))
            throw new InvalidOperationException("Cannot start GridFight.");
        var session = manager.Session!;
        var portal = portalId ?? session.PortalOffer.First(id => id != GridFightSession.BlueSeaPortalId);
        if (!session.PortalOffer.Contains(portal)) continue;
        if (!session.HandlePending(session.PendingQueuePosition, GridFightPendingRequestKind.PortalSelect, portal))
            throw new InvalidOperationException("Cannot select portal.");
        if (session.Phase == GridFightSessionPhase.SelectingNestedPortal)
        {
            Check("Blue Sea exposes nested portal", session.PortalOffer.Count == 1);
            Check("Blue Sea keeps its active effect", session.ActivePortalIds.Contains(GridFightSession.BlueSeaPortalId));
            session.HandlePending(session.PendingQueuePosition, GridFightPendingRequestKind.PortalSelect, session.PortalOffer.Single());
        }
        session.HandlePending(session.PendingQueuePosition, GridFightPendingRequestKind.RoundBegin);
        session.GrantOfficialInitialRoundRewards(out _);
        session.HandlePending(session.PendingQueuePosition, GridFightPendingRequestKind.ReturnPreparation);
        return manager;
    }
    throw new InvalidOperationException($"Portal {portalId} was never offered.");
}

var opening = NewRun(101);
var openingSession = opening.Session!;
Check("opening reaches preparation", openingSession.Phase == GridFightSessionPhase.Preparing);
Check("ordinary opening has four roles costing six in total", openingSession.Roles.Count == 4 &&
    openingSession.Roles.Sum(role => GameData.GridFightRoleBasicInfoData[role.RoleId].Rarity) == 6);
Check("opening IDs do not collide", openingSession.Roles.Select(role => role.UniqueId)
    .Concat(openingSession.ForgeItems.Select(item => item.UniqueId))
    .Concat(openingSession.Equipments.Select(item => item.UniqueId))
    .Concat(openingSession.Orbs.Select(item => item.UniqueId)).Distinct().Count() ==
    openingSession.Roles.Count + openingSession.ForgeItems.Count + openingSession.Equipments.Count + openingSession.Orbs.Count);
var orbCount = openingSession.Orbs.Count;
Check("initial reward cannot be granted twice", openingSession.GrantOfficialInitialRoundRewards(out _).Count == 0 &&
    openingSession.Orbs.Count == orbCount);
Check("stale pending request does not advance state", !openingSession.HandlePending(1, GridFightPendingRequestKind.RoundBegin));

foreach (var portalId in catalog.PortalPool)
{
    var manager = NewRun(portalId);
    var session = manager.Session!;
    var snapshot = manager.BuildCurrentInfo()!;
    var bytes = snapshot.ToByteArray();
    Check($"portal {portalId}: opening and protobuf round trip", session.Phase == GridFightSessionPhase.Preparing &&
        GridFightCurrentInfo.Parser.ParseFrom(bytes).Equals(snapshot));
}

var shopManager = NewRun(101);
var shop = shopManager.Session!;
shop.AdjustGold(1000);
var previousShop = shop.Shop.Select(slot => (slot.RoleId, slot.Star, slot.SoldOut)).ToArray();
Check("shop locks", shop.SetShopLocked(true));
Check("locked free refresh retains slots", shop.RefreshShop(false) && previousShop.SequenceEqual(
    shop.Shop.Select(slot => (slot.RoleId, slot.Star, slot.SoldOut))));
var previousGold = shop.Gold;
Check("paid refresh unlocks and charges once", shop.RefreshShop(true) && !shop.ShopLocked &&
    shop.Gold == previousGold - catalog.RefreshCost);
var maxStarRole = catalog.RolePool.First(role => role.ID == 1001);
shop.TryAddRole(maxStarRole.ID, 3, out _);
var switchGroup = catalog.RoleSwitchGroups.FirstOrDefault(group => group.RoleIdList.Contains(maxStarRole.ID));
var excluded = switchGroup?.RoleIdList.ToHashSet() ?? [maxStarRole.ID];
var maxStarFiltered = true;
for (var i = 0; i < 200; i++)
{
    shop.RefreshShop(false);
    maxStarFiltered &= shop.Shop.All(slot => !excluded.Contains(slot.RoleId));
}
Check("shop excludes owned max-star roles and switch variants", maxStarFiltered);
var allExcluded = GameData.GridFightRoleBasicInfoData.Keys.ToHashSet();
Check("fully excluded shop returns empty without throwing", catalog.RollShop(catalog.InitialPlayerLevel,
    excludedRoleIds: allExcluded).Count == 0);

// Synthetic items isolate dress restrictions from resource balancing changes.
const uint FreeItem = 990001, UniqueItem = 990002, LeaderItem = 990003, RoleItem = 990004;
foreach (var (id, rule) in new[] { (FreeItem, GridFightEquipDressTypeEnum.DressRuleNotUnique),
             (UniqueItem, GridFightEquipDressTypeEnum.DressRuleUnique),
             (LeaderItem, GridFightEquipDressTypeEnum.DressRuleLeader),
             (RoleItem, GridFightEquipDressTypeEnum.DressRuleRoleOnly) })
    GameData.GridFightEquipmentData[id] = new GridFightEquipmentExcel
    {
        ID = id, DressRule = rule, DressRuleParamList = [1001],
    };
GridFightRoleState AddRole(GridFightSession session, uint roleId)
{
    if (!session.TryAddRole(roleId, 1, out var acquisition)) throw new InvalidOperationException("Cannot add role.");
    return acquisition.AcquiredRoles.Single();
}
GridFightEquipmentState AddEquipment(GridFightSession session, uint equipmentId)
{
    if (!session.TryAddEquipment(equipmentId, out var equipment)) throw new InvalidOperationException("Cannot add equipment.");
    return equipment;
}
HLHHLIGPNOM Track(uint roleId, uint equipmentId, uint priority = 1)
{
    var update = new HLHHLIGPNOM { BGFFICLCKHE = roleId, BJDOINAHDLN = true, Priority = priority };
    update.GridFightItemList.Add(equipmentId);
    return update;
}
var tracksManager = NewRun(101);
var tracks = tracksManager.Session!;
tracks.Roles.Clear();
var firstRole = AddRole(tracks, 1001);
var secondRole = AddRole(tracks, 1004);
var request = Track(1001, FreeItem);
request.MCJMHBIECMN.Add(FreeItem);
Check("track accepts valid request", tracks.UpdateEquipmentTracks([request], 0) == 0);
Check("unowned client completion is ignored", tracks.EquipmentTracks.Single().CompletedEquipmentIds.Count == 0);
Check("invalid batch is atomic", tracks.UpdateEquipmentTracks([Track(1004, FreeItem), Track(0, FreeItem)], 0) != 0 &&
    tracks.EquipmentTracks.Count == 1);
Check("track cap rejects batch without partial writes", tracks.UpdateEquipmentTracks(
    [Track(1004, FreeItem), Track(1003, FreeItem), Track(1005, FreeItem)], 0) == GridFightSession.EquipmentTrackMaxRetcode &&
    tracks.EquipmentTracks.Count == 1);

// Recommendation/craft APIs call the automatic dress path, exercised through tracked craft.
GameData.GridFightCraftConfigData[990001] = new GridFightCraftConfigExcel
    { CraftID = 990001, CostEquipList = [FreeItem, FreeItem], CraftEquipID = LeaderItem };
GridFightEquipCraftResult CraftTracked(GridFightSession session, uint itemId, uint target = 0)
{
    var left = AddEquipment(session, FreeItem);
    var right = AddEquipment(session, FreeItem);
    return session.CraftEquipment(itemId, [left.UniqueId, right.UniqueId],
        GridFightEquipCraftSource.KgridFightEquipCraftSourceTrack, target);
}
tracks.UpdateEquipmentTracks([Track(1001, LeaderItem), Track(1004, LeaderItem, 2)], 0);
secondRole.Position = 1;
var leaderCraft = CraftTracked(tracks, LeaderItem);
Check("automatic dress skips ineligible higher-priority role", leaderCraft.Success &&
    !firstRole.EquippedEquipmentUniqueIds.Contains(leaderCraft.AddedEquipment!.UniqueId) &&
    secondRole.EquippedEquipmentUniqueIds.Contains(leaderCraft.AddedEquipment.UniqueId));
var explicitCraft = CraftTracked(tracks, LeaderItem, firstRole.UniqueId);
Check("explicit ineligible target leaves item in inventory", explicitCraft.Success &&
    tracks.Roles.All(role => !role.EquippedEquipmentUniqueIds.Contains(explicitCraft.AddedEquipment!.UniqueId)));
var unknownCraft = CraftTracked(tracks, LeaderItem, uint.MaxValue);
Check("unknown explicit target does not fall back to another role", unknownCraft.Success &&
    tracks.Roles.All(role => !role.EquippedEquipmentUniqueIds.Contains(unknownCraft.AddedEquipment!.UniqueId)));
tracks.TryAddEquipment(LeaderItem, out var lockedEquipment, isLocked: true);
Check("NPC equipment is locked before automatic tracking can claim it", lockedEquipment.IsLocked &&
    tracks.Roles.All(role => !role.EquippedEquipmentUniqueIds.Contains(lockedEquipment.UniqueId)));

foreach (var pair in new[] { (350601u, 350601u), (350601u, 350602u), (350602u, 350602u) })
{
    var manager = NewRun(101);
    var session = manager.Session!;
    session.Roles.Clear();
    var role = AddRole(session, 1001);
    var a = AddEquipment(session, pair.Item1);
    var b = AddEquipment(session, pair.Item2);
    var before = session.MaxActiveRoles;
    var crafted = session.CraftEquipment(GridFightSession.WealthDiamondEquipmentId, [a.UniqueId, b.UniqueId], 0, 0);
    Check($"diamond {pair}: manual recipe and capacity", crafted.Success && session.MaxActiveRoles == before + 1);
    a = AddEquipment(session, pair.Item1);
    b = AddEquipment(session, pair.Item2);
    session.DressEquipment(role.UniqueId, a.UniqueId);
    var dressed = session.DressEquipment(role.UniqueId, b.UniqueId);
    Check($"diamond {pair}: on-role recipe agrees", dressed.Success && dressed.AutoCrafted &&
        dressed.AddedEquipments.Single().EquipmentId == GridFightSession.WealthDiamondEquipmentId);
}

var diceManager = NewRun(101);
var diceSession = diceManager.Session!;
diceSession.Roles.Clear();
var diceRole = AddRole(diceSession, 1001);
var otherRole = AddRole(diceSession, 1004);
var dice = AddEquipment(diceSession, 35030404);
var dress = diceSession.DressEquipment(diceRole.UniqueId, dice.UniqueId);
var diceSync = diceManager.BuildEquipDressSync(dress);
var filled = diceSession.GetEquippedEquipment(diceRole).Where(GridFightSession.IsDiceFilledEquipment).ToArray();
Check("dice creates two locked temporary items", dress.Success && filled.Length == 2 && filled.All(item => item.IsLocked));
Check("dice fill is synchronized", diceSync.ResultList.SelectMany(result => result.UpdateDynamicList)
    .Where(data => data.AddGameItemInfo != null).SelectMany(data => data.AddGameItemInfo.GridFightEquipmentList)
    .Count(item => filled.Any(fill => fill.UniqueId == item.UniqueId)) == 2);
Check("temporary item cannot be moved independently", !diceSession.DressEquipment(otherRole.UniqueId, filled[0].UniqueId).Success);
var binding = filled.Select(item => item.EquipmentId).ToArray();
var transfer = diceSession.DressEquipment(otherRole.UniqueId, dice.UniqueId);
diceManager.BuildEquipDressSync(transfer);
Check("moving dice keeps its roll and deletes old instances", transfer.Success && diceRole.EquippedEquipmentUniqueIds.Count == 0 &&
    filled.All(item => !diceSession.Equipments.Contains(item)) && binding.SequenceEqual(
        diceSession.GetEquippedEquipment(otherRole).Where(GridFightSession.IsDiceFilledEquipment).Select(item => item.EquipmentId)));
diceSession.ApplyDiceEquipmentRefills();
var changes = diceSession.TakeDiceEquipmentChanges();
Check("dice reroll reports removals and additions", changes.RemovedEquipments.Count == 2 && changes.AddedEquipments.Count == 2);
Check("dice sync is consumed once", diceSession.TakeDiceEquipmentChanges().AddedEquipments.Count == 0);
diceSession.RecycleRole(otherRole.UniqueId);
Check("selling dice wearer does not leak temporary inventory", diceSession.Equipments.All(item => !GridFightSession.IsDiceFilledEquipment(item)) &&
    diceSession.Equipments.Contains(dice));

var pendingManager = NewRun(101);
var pendingSession = pendingManager.Session!;
pendingSession.Roles.Clear();
pendingSession.TryAddRole(GridFightSpecialFeatures.SilverWolfThreeCostRoleId, 2, out var silverWolfAcquisition);
var silverWolf = silverWolfAcquisition.AcquiredRoles.Single();
var blackSwan = AddRole(pendingSession, GridFightSession.BlackSwanRoleId);
var deployment = pendingSession.UpdatePositions([(silverWolf.UniqueId, 1), (blackSwan.UniqueId, 2)]);
pendingManager.BuildPositionSync(deployment);
Check("simultaneous trait activations share one pending queue", deployment.Success &&
    pendingSession.HeadPlayerPending != null && pendingSession.FateDivinerPending == null);
Check("pending role cannot be moved, sold or sent into battle", !pendingSession.UpdatePositions([(silverWolf.UniqueId, 14)]).Success &&
    !pendingSession.RecycleRole(silverWolf.UniqueId).Success && !pendingSession.EnterBattle());
var head = pendingSession.HeadPlayerPending!;
Check("invalid enhancement preserves pending state", !pendingSession.ResolveHeadPlayerEnhance(head.QueuePosition, uint.MaxValue).Success &&
    ReferenceEquals(head, pendingSession.HeadPlayerPending));
var headResult = pendingSession.ResolveHeadPlayerEnhance(head.QueuePosition, GridFightSpecialFeatures.SilverWolfUpgradeToFourCostOptionId);
pendingManager.BuildHeadPlayerEnhanceSync(headResult);
Check("cost upgrade retains entity and advances pending queue", headResult.Success &&
    silverWolf.RoleId == GridFightSpecialFeatures.SilverWolfFourCostRoleId && silverWolf.Star == 1 && silverWolf.Position == 1);
Check("queued Fate enhancement follows Head Player", pendingSession.HeadPlayerPending == null &&
    pendingSession.FateDivinerPending?.QueuePosition > head.QueuePosition);
var fate = pendingSession.FateDivinerPending!;
var fateResult = pendingSession.ResolveFateDivinerEnhance(fate.QueuePosition, 6);
pendingManager.BuildFateDivinerEnhanceSync(fateResult);
Check("Fate enhancement completes and releases selection", fateResult.Success && !pendingSession.HasPendingSelection);
Check("duplicate enhancement cannot grant twice", !pendingSession.ResolveFateDivinerEnhance(fate.QueuePosition, 6).Success);

// A full route exercises battles, supply nodes, encounters and investment selections.
var routeManager = NewRun(GridFightSession.DotContractPortalId);
var route = routeManager.Session!;
route.Roles.Clear();
route.ActiveInvestmentIds.Add(GridFightSession.PeriodicBenefitsAugmentId);
route.ActiveInvestmentIds.Add(GridFightSession.GoldFuturesPlusAugmentId);
var fighter = AddRole(route, 1001);
fighter.Position = 1;
const uint PeriodicGoldItem = 990005;
GameData.GridFightEquipmentData[PeriodicGoldItem] = new GridFightEquipmentExcel
{
    ID = PeriodicGoldItem, DressRule = GridFightEquipDressTypeEnum.DressRuleNotUnique,
    EquipFunc = GridFightEquipFuncTypeEnum.AvatarMaxNumberAdd,
    ParamList = [new() { Value = 1 }, new() { Value = 2 }],
};
var periodicItem = AddEquipment(route, PeriodicGoldItem);
route.DressEquipment(fighter.UniqueId, periodicItem.UniqueId);
var battleCount = 0;
var futuresSelections = 0;
var lastFuturesNode = 0;
var periodicRewardsObserved = 0;
var finished = false;
for (var transition = 0; transition < 300 && !finished; transition++)
{
    var queue = route.PendingQueuePosition;
    switch (route.Phase)
    {
        case GridFightSessionPhase.Preparing:
            if (!route.EnterBattle()) throw new InvalidOperationException($"Cannot battle at {route.ChapterId}/{route.SectionId}.");
            var statistics = new GridFightBattleStatistics(100, [], [], [], [], 1,
                new Dictionary<string, uint> { [GridFightSession.DotContractSavedValueKey] = 180 });
            var settlement = route.ResolveBattle(true, statistics);
            if (!settlement.Success) throw new InvalidOperationException("Battle settlement failed.");
            battleCount++;
            Check($"battle {battleCount}: duplicate settlement rejected", !route.ResolveBattle(true, statistics).Success);
            finished = !settlement.RouteAdvanced;
            break;
        case GridFightSessionPhase.AwaitingBattleRound:
        case GridFightSessionPhase.AwaitingSupplyRound:
            if (!route.HandlePending(queue, GridFightPendingRequestKind.RoundBegin)) throw new InvalidOperationException("Round failed.");
            var roundSync = routeManager.BuildBattleRoundSync(queue, []);
            periodicRewardsObserved += roundSync.ResultList.Count(result =>
                result.EffectParamList.Contains(GridFightSession.PeriodicBenefitsAugmentId));
            break;
        case GridFightSessionPhase.SelectingInvestment:
            if (route.IsGoldFuturesInvestmentPending)
            {
                var nodeIndex = catalog.StandardRoute.ToList().FindIndex(node =>
                    node.ChapterId == route.ChapterId && node.SectionId == route.SectionId);
                Check($"gold futures selection {++futuresSelections}: waits at least four nodes", nodeIndex - lastFuturesNode >= 4);
                lastFuturesNode = nodeIndex;
            }
            var investment = route.InvestmentOffer.First();
            if (!route.SelectInvestment(queue, investment)) throw new InvalidOperationException("Investment failed.");
            routeManager.BuildInvestmentSelectionSync(queue, investment);
            break;
        case GridFightSessionPhase.SelectingSupply:
            var supply = route.SelectSupply(queue, [1]);
            if (!supply.Success) throw new InvalidOperationException("Supply failed.");
            routeManager.BuildSupplySelectionSync(queue, 1, supply);
            break;
        case GridFightSessionPhase.SelectingEncounter:
            if (!route.SelectEncounter(queue, route.EncounterOptions.First().EliteBranchId)) throw new InvalidOperationException("Encounter failed.");
            break;
        case GridFightSessionPhase.AwaitingBattleReturn:
            if (!route.HandlePending(queue, GridFightPendingRequestKind.ReturnPreparation)) throw new InvalidOperationException("Return failed.");
            break;
        case GridFightSessionPhase.AwaitingBackToPrepare:
            var beforePreparationGold = route.Gold;
            if (!route.BackToPrepare()) throw new InvalidOperationException("Preparation failed.");
            routeManager.BuildPreparationSync();
            Check($"node {route.ChapterId}/{route.SectionId}: periodic equipment pays once", route.Gold == beforePreparationGold + 2 &&
                !route.BackToPrepare() && route.Gold == beforePreparationGold + 2);
            break;
        default:
            throw new InvalidOperationException($"Unexpected route state {route.Phase}.");
    }
}
Check("full route reaches final boss settlement", finished && battleCount > 10);
Check("periodic benefits are published on node entry", periodicRewardsObserved > 10);
Check("gold futures offers recur through full route", futuresSelections >= 2);
Check("DoT threshold grants Black Swan once across repeated settlements", route.Orbs.Count(orb =>
    orb.OrbItemId == GridFightSession.DotContractBlackSwanOrbId) == 1);

var abort = NewRun(101).Session!;
abort.Roles.Clear();
AddRole(abort, 1001).Position = 1;
var abortGold = abort.Gold;
var abortSection = abort.SectionId;
Check("aborted battle returns to preparation without rewards or route advancement", abort.EnterBattle() &&
    !abort.ResolveBattle(false, GridFightBattleStatistics.Empty).Success && abort.Phase == GridFightSessionPhase.Preparing &&
    abort.Gold == abortGold && abort.SectionId == abortSection);

Console.WriteLine($"GridFight regression: {checks - failures.Count}/{checks} passed.");
return failures.Count == 0 ? 0 : 1;
