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

// Usage: GridFightRegression [--cache <Resource.bin>]
// Reads the checked-out cache (or a copy of --cache); all gameplay and fixture changes remain in process memory.
// Checks shared by every season run first; the capture-backed season-1 mechanics (Silver Wolf, Fate Diviner,
// DoT contract) run only when the cache's GridFight_Season_ID is 1.
var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
string? Arg(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
var cacheArgument = Arg("--cache");
var configPath = Path.Combine(repository, "Config");
if (cacheArgument != null)
{
    configPath = Path.Combine(repository, "tmp/GridFightRegression/Cache");
    Directory.CreateDirectory(configPath);
    File.Copy(Path.GetFullPath(cacheArgument, repository), Path.Combine(configPath, "Resource.bin"), true);
}
ConfigManager.Config.Path.ConfigPath = configPath;
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
var season = catalog.CurrentSeason;
Console.WriteLine($"GridFight season {season}.{catalog.CurrentSubSeason}, role pool {catalog.RolePool.Count}, " +
                  $"portals {catalog.PortalPool.Count}, augments {catalog.AugmentPool.Count}");

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

// Test roles: unrestricted current-season shop roles with every star row (season 1 keeps its historical 1001/1004).
var testRoles = catalog.RolePool
    .Where(role => openingSession.IsRestrictedRoleUnlockedForRun(role.ID))
    .Where(role => Enumerable.Range(1, 3).All(star => GameData.GridFightRoleStarData.ContainsKey(role.ID << 4 | (uint)star)))
    .OrderBy(role => role.ID is 1001 or 1004 ? 0 : 1)
    .ThenBy(role => role.ID)
    .Select(role => role.ID)
    .Take(4)
    .ToArray();
if (testRoles.Length < 4) throw new InvalidDataException("GridFight role pool has fewer than four test roles.");
var (roleA, roleB, roleC, roleD) = (testRoles[0], testRoles[1], testRoles[2], testRoles[3]);

// Battle-entry maze skills are sent as MazeBuffs; a skill id without a MazeBuff row (e.g. 121002 Guinaifen) makes the
// client NRE while the battle loads, so every role star must resolve to nothing or to an existing MazeBuff.
var invalidEntryBuffs = GameData.GridFightRoleStarData.Values
    .Select(star => new GridFightRoleState { RoleId = star.ID, Star = star.Star })
    .Where(role => March7thHoney.GameServer.Game.GridFight.Battle.GridFightBattleProtoBuilder
                       .TryResolveEntryMazeSkill(role, out var buffId) &&
                   !GameData.MazeBuffData.ContainsKey(buffId * 10 + 1))
    .Select(role => $"{role.RoleId}/{role.Star}")
    .ToList();
Check($"battle-entry maze buffs exist for every role star (invalid: {string.Join(", ", invalidEntryBuffs)})",
    invalidEntryBuffs.Count == 0);
if (GameData.GridFightRoleStarData.ContainsKey(121002u << 4 | 1))
    Check("battle-entry maze buff: Guinaifen's overridden skill 121002 is not sent",
        !March7thHoney.GameServer.Game.GridFight.Battle.GridFightBattleProtoBuilder.TryResolveEntryMazeSkill(
            new GridFightRoleState { RoleId = 121002, Star = 1 }, out _));

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
var maxStarRole = catalog.RolePool.First(role => role.ID == roleA);
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

// --- Season separation ----------------------------------------------------------------------------------------
// From 4.6.51 the role/trait/equipment/forge/craft tables hold several seasons; nothing from another season may
// reach a run. On a single-season cache these checks still hold trivially.
bool RoleInSeason(uint roleId) => GridFightSeasonScope.IsRoleInSeason(roleId, season);
bool ItemInSeason(uint itemId) => GridFightSeasonScope.IsItemInSeason(itemId, season);
bool RewardInSeason(GridFightRewardEntry reward) => reward.Type switch
{
    GridFightBonusTypeEnum.SpecificAvatar => RoleInSeason(reward.ItemId),
    GridFightBonusTypeEnum.Item => ItemInSeason(reward.ItemId),
    _ => true,
};
var hasOtherSeasonRows = GameData.GridFightRoleBasicInfoData.Values.Any(role => role.SeasonID != season);
Console.WriteLine($"cache holds other-season GridFight rows: {hasOtherSeasonRows}");
Check("role pool holds only current-season roles", catalog.RolePool.All(role => role.SeasonID == season));
Check("role pool excludes upgrade-only cost tiers", catalog.RolePool.All(role => !catalog.UpgradeOnlyRoleIds.Contains(role.ID)));
Check("portal and augment pools belong to the current season",
    catalog.PortalPool.All(id => GridFightSeasonScope.IsPortalInSeason(id, season)) &&
    catalog.AugmentPool.All(id => GridFightSeasonScope.IsAugmentInSeason(id, season)));
Check("basic equipment pool belongs to the current season", catalog.BasicEquipmentPool.Count > 0 &&
    catalog.BasicEquipmentPool.All(ItemInSeason));
var seasonShop = NewRun(101).Session!;
seasonShop.AdjustGold(100000);
var shopRolesInSeason = true;
for (var i = 0; i < 300; i++)
{
    seasonShop.RefreshShop(false);
    shopRolesInSeason &= seasonShop.Shop.All(slot => slot.SpecialGoodsId != 0 || RoleInSeason(slot.RoleId));
}
Check("shop rolls only current-season roles", shopRolesInSeason);
var supplyInSeason = Enumerable.Range(0, 100).All(_ => catalog.RollSupplyOffer(5)
    .All(offer => RoleInSeason(offer.RoleId) && ItemInSeason(offer.EquipmentId)));
Check("supply offers stay in the current season", supplyInSeason);
var orbsChecked = 0;
var orbsInSeason = true;
foreach (var orbId in GameData.GridFightOrbData.Keys.Order())
for (var sample = 0; sample < 8; sample++)
{
    if (!GridFightSpecialFeatures.TryResolveOrb(orbId, out var rewards)) break;
    orbsChecked++;
    if (rewards.All(RewardInSeason)) continue;
    orbsInSeason = false;
    Console.WriteLine($"orb {orbId} opened other-season rewards: " +
                      string.Join(", ", rewards.Select(reward => $"{reward.Type}:{reward.ItemId}")));
    break;
}
Check($"every resolvable orb opens current-season roles and items ({orbsChecked} samples)", orbsChecked > 0 && orbsInSeason);
var forgesInSeason = GameData.GridFightForgeData.Keys
    .Where(id => ItemInSeason(id))
    .All(id => Enumerable.Range(0, 20).All(_ => catalog.RollForgeEquipment(id).All(ItemInSeason)));
Check("forge offers stay in the current season", forgesInSeason);
var categoriesInSeason = Enum.GetValues<GridFightEquipCategoryEnum>()
    .All(category => Enumerable.Range(0, 20).All(_ => catalog.RollEquipment(category, 0) is var id && (id == 0 || ItemInSeason(id))));
Check("random equipment by category stays in the current season", categoriesInSeason &&
    Enumerable.Range(0, 20).All(_ => catalog.RollPepeEmblems(3).All(ItemInSeason)));
var craftSession = NewRun(101).Session!;
var basicRecipe = GridFightSeasonScope.Crafts(season)
    .Where(recipe => recipe.CostEquipList.Count == 2 && recipe.CostEquipList.All(id =>
        GameData.GridFightEquipmentData.TryGetValue(id, out var item) && item.EquipCategory == GridFightEquipCategoryEnum.Basic))
    .Where(recipe => GameData.GridFightEquipmentData.ContainsKey(recipe.CraftEquipID))
    .OrderBy(recipe => recipe.CraftID)
    .First();
craftSession.TryAddEquipment(basicRecipe.CostEquipList[0], out var craftLeft);
craftSession.TryAddEquipment(basicRecipe.CostEquipList[1], out var craftRight);
var seasonalCraft = craftSession.CraftEquipment(0, [craftLeft.UniqueId, craftRight.UniqueId], 0, 0);
Check($"basic pair {basicRecipe.CostEquipList[0]}+{basicRecipe.CostEquipList[1]} crafts the current-season item",
    seasonalCraft.Success && seasonalCraft.AddedEquipment!.EquipmentId == basicRecipe.CraftEquipID &&
    ItemInSeason(seasonalCraft.AddedEquipment.EquipmentId));
var systemInfo = opening.BuildSystemInfo();
var tutorialStages = GameData.GridFightConstValueData.GetValueOrDefault("GridFight_TutorialStageList")?.AsUInt32List() ?? [];
Check("permanent info reports every tutorial stage as finished", tutorialStages.Count > 0 &&
    systemInfo.PermanentInfo.ENECKKGPKHI == tutorialStages.Max() &&
    tutorialStages.All(stage => systemInfo.PermanentInfo.FBJMBJKMAJA.TryGetValue(stage, out var progress) &&
                                progress.EABKMELHIML == stage && progress.GDAKCOICIDA > 0 &&
                                (progress.MPJPLKKJNGE > 0 || GameData.GridFightTutorialStageNodeData.All(node => node.UnlockTutorialTask == 0))));
var handbook = systemInfo.StaticInfo.Handbook;
Check("handbook lists only current-season roles, items and portals",
    handbook.RoleInfo.RoleIdList.All(RoleInSeason) &&
    handbook.EquipInfo.GridFightItemList.All(ItemInSeason) &&
    handbook.PortalInfo.SeenPortalIdList.All(id => GridFightSeasonScope.IsPortalInSeason(id, season)));
var avatarOrb = GameData.GridFightOrbData.Values
    .Select(orb => (Orb: orb, Bonus: GameData.GridFightBasicBonusPoolV2Data.GetValueOrDefault(orb.BonusID)))
    .Where(entry => entry.Bonus?.BonusType == GridFightBonusTypeEnum.SpecificAvatar)
    .Select(entry => (entry.Orb.OrbID, AvatarId: entry.Bonus!.BonusTypeParamList.FirstOrDefault()))
    .FirstOrDefault(entry => GridFightSeasonScope.ResolveRoleId(entry.AvatarId, season) is var id && id != 0 &&
                             GameData.GridFightRoleBasicInfoData[id].AvatarID == entry.AvatarId);
Check("avatar orbs resolve to the current season's role for that avatar", avatarOrb.OrbID != 0 &&
    GridFightSpecialFeatures.TryResolveOrb(avatarOrb.OrbID, out var avatarRewards) &&
    avatarRewards.All(reward => reward.Type != GridFightBonusTypeEnum.SpecificAvatar ||
                                RoleInSeason(reward.ItemId) &&
                                GameData.GridFightRoleBasicInfoData[reward.ItemId].AvatarID == avatarOrb.AvatarId));
foreach (var tieredRoleId in catalog.UpgradeOnlyRoleIds.Where(id =>
             GameData.GridFightConstValueData.GetValueOrDefault("GridFight_SevenPriceRoleIdList")?.AsUInt32List().Contains(id) == true))
    Check($"seven-price role {tieredRoleId} uses ShopPrice row 7",
        catalog.GetBuyPrice(tieredRoleId) == GameData.GridFightShopPriceData[7].BuyGoldList[0]);
Check("shop weights come from the current season", catalog.IsSeasonOne ||
    catalog.GetShopRarityWeights(3).SequenceEqual(GameData.GridFightPlayerLevelData[3].RarityWeights));
// Season 2 runs carry one partner; the client flow reads its first effect when the run opens. Season 1 has none.
var partnerRun = NewRun();
var partnerSession = partnerRun.Session!;
var partnerSync = partnerRun.BuildCurrentInfo()!.GameInfoList
    .Where(info => info.OGKMAKNOPOD != null)
    .SelectMany(info => info.OGKMAKNOPOD.BBEEGMDAMIB)
    .ToList();
Check("season 2 run syncs one partner effect; season 1 syncs none", catalog.IsSeasonOne
    ? partnerSession.PetPalId == 0 && partnerSync.Count == 0
    : GameData.GridFightPetPalData.TryGetValue(partnerSession.PetPalId, out var partnerRow) &&
      partnerRow.EffIDList.Contains(partnerSession.PetPalEffectId) && partnerSync.Count == 1 &&
      partnerSync[0].NDAHDHKGNBG == partnerSession.PetPalId && partnerSync[0].OKGGFNJJLNG == partnerSession.PetPalEffectId);

// The 4.7 client builds its stage nodes only from the synced route nodes; without them the current node is null.
var levelInfo = partnerRun.BuildCurrentInfo()!.GameInfoList.Single(info => info.LevelInfo != null).LevelInfo;
Check("current info syncs every node of a current-season route", GridFightSeasonScope.IsRouteInSeason(catalog.StandardRouteId, season) &&
    levelInfo.RouteId == catalog.StandardRouteId && levelInfo.OOMMIIKPHBC.Count == catalog.StandardRoute.Count &&
    levelInfo.OOMMIIKPHBC.Zip(catalog.StandardRoute).All(pair =>
        pair.First.DCAHJELNJNP == pair.Second.ChapterId && pair.First.SectionId == pair.Second.SectionId &&
        pair.First.Param.StageId == pair.Second.StageId && pair.First.Param.NodeType == (uint)pair.Second.NodeType &&
        pair.First.Param.PFEMDGNFHNH == pair.Second.Template.IsAugment &&
        pair.First.Param.ParamList.SequenceEqual(pair.Second.Parameters)) &&
    levelInfo.OOMMIIKPHBC.Any(node => node.DCAHJELNJNP == levelInfo.ChapterId && node.SectionId == levelInfo.SectionId));

// The 4.7 client reads consumable updates and removals only through the nested consumable info (field 13).
var consumableItem = GameData.GridFightConsumablesData.Keys.Order()
    .First(id => partnerSession.Consumables.All(held => held.ItemId != id));
var consumableUpdates = new[] { true, false }.Select(_ =>
        partnerSession.TryAddConsumable(consumableItem, out var consumableChange)
            ? partnerRun.BuildConsumableSync(consumableChange).ResultList.Single().UpdateDynamicList.Single()
            : null)
    .ToList();
Check("consumable add and stack updates carry the nested consumable info", consumableUpdates.All(update =>
    (update?.AddGameItemInfo ?? update?.UpdateGameItemInfo)?.UpdateGridFightConsumableList.Single() is { } entry &&
    entry.MCKHGIEJLHM is { } nested && nested.GroupId == entry.GroupId && nested.ItemId == consumableItem &&
    nested.Num == entry.Num) && consumableUpdates[0]?.AddGameItemInfo != null && consumableUpdates[1]?.UpdateGameItemInfo != null);

// Synthetic items isolate dress restrictions from resource balancing changes.
const uint FreeItem = 990001, UniqueItem = 990002, LeaderItem = 990003, RoleItem = 990004;
foreach (var (id, rule) in new[] { (FreeItem, GridFightEquipDressTypeEnum.DressRuleNotUnique),
             (UniqueItem, GridFightEquipDressTypeEnum.DressRuleUnique),
             (LeaderItem, GridFightEquipDressTypeEnum.DressRuleLeader),
             (RoleItem, GridFightEquipDressTypeEnum.DressRuleRoleOnly) })
    GameData.GridFightEquipmentData[id] = new GridFightEquipmentExcel
    {
        ID = id, DressRule = rule, DressRuleParamList = [roleA],
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
var firstRole = AddRole(tracks, roleA);
var secondRole = AddRole(tracks, roleB);
var request = Track(roleA, FreeItem);
request.MCJMHBIECMN.Add(FreeItem);
Check("track accepts valid request", tracks.UpdateEquipmentTracks([request], 0) == 0);
Check("unowned client completion is ignored", tracks.EquipmentTracks.Single().CompletedEquipmentIds.Count == 0);
Check("invalid batch is atomic", tracks.UpdateEquipmentTracks([Track(roleB, FreeItem), Track(0, FreeItem)], 0) != 0 &&
    tracks.EquipmentTracks.Count == 1);
Check("track cap rejects batch without partial writes", tracks.UpdateEquipmentTracks(
    [Track(roleB, FreeItem), Track(roleC, FreeItem), Track(roleD, FreeItem)], 0) == GridFightSession.EquipmentTrackMaxRetcode &&
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
tracks.UpdateEquipmentTracks([Track(roleA, LeaderItem), Track(roleB, LeaderItem, 2)], 0);
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
    var role = AddRole(session, roleA);
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
var diceRole = AddRole(diceSession, roleA);
var otherRole = AddRole(diceSession, roleB);
var dice = AddEquipment(diceSession, 35030404);
var dress = diceSession.DressEquipment(diceRole.UniqueId, dice.UniqueId);
var diceSync = diceManager.BuildEquipDressSync(dress);
var filled = diceSession.GetEquippedEquipment(diceRole).Where(GridFightSession.IsDiceFilledEquipment).ToArray();
Check("dice creates two locked temporary items", dress.Success && filled.Length == 2 && filled.All(item => item.IsLocked));
Check("dice fill stays in the current season", filled.All(item => ItemInSeason(item.EquipmentId)));
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

if (catalog.IsSeasonOne)
{
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
}

// A full route exercises battles, supply nodes, encounters and investment selections.
var routePortal = catalog.PortalPool.Contains(GridFightSession.DotContractPortalId)
    ? GridFightSession.DotContractPortalId
    : 101u;
var routeManager = NewRun(routePortal);
var route = routeManager.Session!;
route.Roles.Clear();
route.ActiveInvestmentIds.Add(GridFightSession.PeriodicBenefitsAugmentId);
route.ActiveInvestmentIds.Add(GridFightSession.GoldFuturesPlusAugmentId);
var fighter = AddRole(route, roleA);
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
var routeStaysInSeason = true;
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
            routeStaysInSeason &= route.InvestmentOffer.All(id => GridFightSeasonScope.IsAugmentInSeason(id, season));
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
            routeStaysInSeason &= route.SupplyOffer.All(offer => RoleInSeason(offer.RoleId));
            // The 4.6.51 client builds every draft card from the top drop (field 13) and NREs
            // without it, leaving the run stuck on the post-battle settlement.
            var supplyCards = routeManager.BuildPendingAction()?.SupplyAction?.SupplyRoleInfoList;
            Check($"supply at {route.ChapterId}/{route.SectionId}: every card carries a role top drop and its equipment drop",
                supplyCards is { Count: > 0 } && supplyCards.All(card =>
                    card.OPPFPECOBEF is { DropType: GridFightDropType.Role } top &&
                    top.ItemId == card.RoleId && top.Param?.RoleStar >= 1 && card.GridFightItemList.Count >= 1 &&
                    card.DHLBEFLOOPM.Select(drop => drop.ItemId).SequenceEqual(card.GridFightItemList)));
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
    routeStaysInSeason &= route.Roles.All(role => RoleInSeason(role.RoleId)) &&
                          route.Shop.All(slot => slot.SpecialGoodsId != 0 || RoleInSeason(slot.RoleId)) &&
                          route.Equipments.All(item => item.EquipmentId >= 990000 || ItemInSeason(item.EquipmentId));
}
Check("full route reaches final boss settlement", finished && battleCount > 10);
Check("full route keeps roles, shop, items and offers in the current season", routeStaysInSeason);

// Season 2 adds an effect-only level 11 after the level-10 cap; normal exp must stop at the cap.
var levelRun = NewRun(routePortal).Session!;
levelRun.AdjustGold(10000);
while (levelRun.BuyExp()) { }
var boughtLevel = levelRun.PlayerLevel;
levelRun.Roles.Clear();
AddRole(levelRun, roleA).Position = 1;
var cappedBattleWon = levelRun.EnterBattle() &&
    levelRun.ResolveBattle(true, new GridFightBattleStatistics(100, [], [], [], [], 1, new Dictionary<string, uint>())).Success;
Check("player level stops at the level cap, also when a battle grants exp",
    boughtLevel == catalog.MaxPlayerLevel && cappedBattleWon && levelRun.PlayerLevel == catalog.MaxPlayerLevel);
Check("periodic benefits are published on node entry", periodicRewardsObserved > 10);
Check("gold futures offers recur through full route", futuresSelections >= 2);
if (routePortal == GridFightSession.DotContractPortalId)
    Check("DoT threshold grants Black Swan once across repeated settlements", route.Orbs.Count(orb =>
        orb.OrbItemId == GridFightSession.DotContractBlackSwanOrbId) == 1);

var abort = NewRun(101).Session!;
abort.Roles.Clear();
AddRole(abort, roleA).Position = 1;
var abortGold = abort.Gold;
var abortSection = abort.SectionId;
Check("aborted battle returns to preparation without rewards or route advancement", abort.EnterBattle() &&
    !abort.ResolveBattle(false, GridFightBattleStatistics.Empty).Success && abort.Phase == GridFightSessionPhase.Preparing &&
    abort.Gold == abortGold && abort.SectionId == abortSection);

// --- Season-2 unique / origin trait effects ---------------------------------------------------------------------
// Wire layouts follow the 4.6.51 client (artifacts/gf-season2/re-g*.md); these checks pin the server-side rules.
if (!catalog.IsSeasonOne)
{
    (GridFightManager Manager, GridFightSession Session) Board(params (uint RoleId, uint Position)[] roles)
    {
        var manager = NewRun(101);
        var session = manager.Session!;
        session.Roles.Clear();
        session.AdjustGold(500);
        foreach (var (roleId, position) in roles) AddRole(session, roleId).Position = position;
        return (manager, session);
    }
    GridFightTraitEffectInfo? EffectInfo(GridFightManager manager, uint traitId, uint effectId) =>
        manager.BuildCurrentInfo()!.GameInfoList
            .Where(info => info.TraitInfo != null)
            .SelectMany(info => info.TraitInfo.GridFightTraitInfo)
            .Where(trait => trait.TraitId == traitId)
            .SelectMany(trait => trait.TraitEffectList)
            .FirstOrDefault(effect => effect.EffectId == effectId);

    // 双龙吟: Dan Heng IL summons a same-star Tenglong in the back area and asks for a trait.
    var (danHengRun, danHeng) = Board((GridFightSession.DanHengMainRoleId, 1), (roleA, 2));
    var danHengSync = danHengRun.BuildSeasonTwoFollowUpSync();
    var sub = danHeng.Roles.FirstOrDefault(role => role.RoleId == GridFightSession.DanHengSubRoleId);
    Check("S2 双龙吟: Tenglong is summoned in the back area with the main's star",
        sub != null && sub.Position is >= 5 and <= 13 && sub.Star == 1 && danHeng.DanHengSubUniqueId == sub.UniqueId &&
        danHengSync!.ResultList.SelectMany(result => result.UpdateDynamicList).Any(data => data.AddRoleInfo?.UniqueId == sub.UniqueId));
    var danHengPending = danHengRun.BuildPendingAction();
    Check("S2 双龙吟: trait choice pending action carries the four candidate traits",
        danHengPending?.GEPGMIEPBMB is { } choose && choose.RoleId == GridFightSession.DanHengMainRoleId &&
        choose.DAHOEGKELCI.OrderBy(id => id).SequenceEqual(new uint[] { 200402, 200602, 200802, 201002 }));
    Check("S2 双龙吟: Tenglong cannot be sold or moved to the front",
        !danHeng.RecycleRole(sub!.UniqueId).Success && !danHeng.UpdatePositions([(sub.UniqueId, 3)]).Success);
    var finishedChoice = danHeng.SelectDanHengTrait(danHengPending!.QueuePosition, 200402, out var choseTrait);
    var main = danHeng.Roles.First(role => role.RoleId == GridFightSession.DanHengMainRoleId);
    Check("S2 双龙吟: the chosen trait goes to Dan Heng IL and the prompt finishes",
        choseTrait && finishedChoice == danHengPending.QueuePosition && danHeng.SeasonTwoPending == null &&
        danHeng.ResolveRoleTraitIds(main).Contains(200402u) &&
        EffectInfo(danHengRun, GridFightSession.DanHengTraitId, GridFightSession.DanHengChooseTraitEffectId)
            ?.GEPGMIEPBMB.CKHHAPGAAOH.SequenceEqual(new uint[] { 200402 }) == true &&
        EffectInfo(danHengRun, GridFightSession.DanHengTraitId, GridFightSession.DanHengTwoInOneEffectId)
            ?.AMOIMOHMBEM is { } twoInOne && twoInOne.CAAKCKEPMEK == main.UniqueId && twoInOne.OKFEMIMJLME == sub.UniqueId);
    Check("S2 双龙吟: the summon does not use population", danHeng.EnterBattle());
    danHeng.AbortBattleStart();
    main.Position = 15;
    danHengRun.BuildSeasonTwoFollowUpSync();
    Check("S2 双龙吟: Tenglong leaves with Dan Heng IL", danHeng.Roles.All(role => role.RoleId != GridFightSession.DanHengSubRoleId));

    // 盗火行者: dragging a role onto Phainon turns it into 薪火 (role saved value).
    var (phainonRun, phainon) = Board((GridFightSession.PhainonRoleId, 1), (roleA, 2));
    phainonRun.BuildSeasonTwoFollowUpSync();
    var food = phainon.Roles.First(role => role.RoleId == roleA);
    var expectedKindling = GridFightSession.GetPhainonKindlingGain(roleA, 1);
    var consumed = phainon.ConsumeRoleForPhainon(food.UniqueId, out var kindling);
    Check("S2 盗火行者: consuming a role removes it and stores the kindling in Phainon's saved value",
        consumed.Success && kindling == expectedKindling && kindling > 0 && phainon.Roles.All(role => role.UniqueId != food.UniqueId) &&
        phainon.RefreshRoleSavedValues().GetValueOrDefault(GridFightSession.PhainonSavedValueKey) == kindling);
    Check("S2 盗火行者: Phainon cannot eat himself",
        !phainon.ConsumeRoleForPhainon(phainon.Roles.First(role => role.RoleId == GridFightSession.PhainonRoleId).UniqueId, out _).Success);

    // 巡海游侠: Mortenax asks to appoint a front role, which then carries trait 300302.
    var (rangerRun, ranger) = Board((150402, 5), (roleA, 1));
    var rangerSync = rangerRun.BuildSeasonTwoFollowUpSync();
    var rangerPending = rangerRun.BuildPendingAction();
    Check("S2 巡海游侠: appointment prompt lists the front roles",
        rangerPending?.DPKFJEEJABC?.PNHBEHLICFL.SequenceEqual(new[] { roleA }) == true && rangerSync != null);
    ranger.AppointRanger(rangerPending!.QueuePosition, roleA, out var appointedRanger);
    Check("S2 巡海游侠: the appointed role gains the ranger trait",
        appointedRanger && ranger.ResolveRoleTraitIds(ranger.Roles.First(role => role.RoleId == roleA))
            .Contains(GridFightSession.RangerTraitId));

    // 黑市主理人: the signature good sits in slot 1 and never sells out.
    var (jadeRun, jade) = Board((GridFightSession.JadeRoleId, 1));
    jadeRun.BuildSeasonTwoFollowUpSync();
    var jadeInfo = EffectInfo(jadeRun, GridFightSession.JadeTraitId, GridFightSession.JadeEffectId)?.GDAMHENIIDJ;
    Check("S2 黑市主理人: shop has a signature good in slot 1 and rotating goods from slot 2, each with drops",
        jadeInfo != null && jadeInfo.OGDKJPDFFAI.Any(good => good.SlotId == 1 && good.DNJCAMKKLPB != 0) &&
        jadeInfo.OGDKJPDFFAI.Where(good => good.DNJCAMKKLPB != 0).All(good => good.NMGIABHIJJB?.DropList.Count > 0));
    var hpBeforeJade = jade.LineupHp;
    var goldBeforeJade = jade.Gold;
    var signature = jade.BuyJadeGood(1);
    Check("S2 黑市主理人: buying the signature good pays HP, grants gold and keeps the slot",
        signature.Success && jade.LineupHp == hpBeforeJade - 10 && jade.Gold > goldBeforeJade &&
        jade.JadeGoods.Any(good => good.SlotId == 1 && good.GoodId != 0));

    // 开拓银轨, 奇兽花园, 梦境交织, 反击, 星核猎手, 机心巧匠: stateful effects round-trip through GetData.
    var trailRoles = GameData.GridFightRoleBasicInfoData.Values
        .Where(role => role.SeasonID == season && role.TraitList.Contains(GridFightSession.TrailblazeTraitId) && role.IsInPool)
        .Select(role => role.ID).Take(2).ToArray();
    if (trailRoles.Length == 2)
    {
        var (trailRun, trail) = Board((trailRoles[0], 1), (trailRoles[1], 2));
        trailRun.BuildSeasonTwoFollowUpSync();
        var trailInfo = EffectInfo(trailRun, GridFightSession.TrailblazeTraitId, GridFightSession.TrailblazeEffectId)?.KDBKIPPHMFI;
        Check("S2 开拓银轨: goods list ends with the refresh good and never contains id 0",
            trailInfo != null && trailInfo.OGDKJPDFFAI.Count > 1 && trailInfo.OGDKJPDFFAI.All(good => good.DNJCAMKKLPB != 0) &&
            trailInfo.OGDKJPDFFAI[^1].DNJCAMKKLPB == GridFightSession.TrailblazeRefreshGoodId);
        trail.EnterBattle();
        trail.ResolveBattle(true, new GridFightBattleStatistics(100, [], [], [], [], 1, new Dictionary<string, uint>()));
        Check("S2 开拓银轨: a battle stage grants 开拓力", trail.TrailblazeCoin > 0);

        // 帕姆王 "累计消耗解锁": spending 开拓力 fills CostedCoin of the oneof 1466 modifier; 500 grants 604202 once.
        var (pamRun, pam) = Board((trailRoles[0], 1), (trailRoles[1], 2));
        pamRun.BuildSeasonTwoFollowUpSync();
        var pamStart = pamRun.BuildCurrentInfo()!.GameData.GameItemList
            .FirstOrDefault(item => item.ONKMJAGNECC != null)?.ONKMJAGNECC;
        Check("S2 帕姆王: GetData carries the progress modifier at 0/500",
            pamStart != null && pamStart.IANFMIJBKAF == 0 && pamStart.PLAIPMBIHGJ == GridFightSession.PamKingCostedThreshold &&
            pam.PamKingModifierUniqueId != 0);
        typeof(GridFightSession).GetField("_trailblazeCoin", System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance)!.SetValue(pam, 2000u);
        var pamFirstCost = pam.TrailblazeGoods[^1].Cost;
        var pamFirstBuy = pam.BuyTrailblazeGood((uint)pam.TrailblazeGoods.Count - 1).Success;
        var pamDiff = pamRun.BuildSeasonTwoFollowUpSync()?.ResultList.SelectMany(result => result.UpdateDynamicList)
            .Select(data => data.GameDataItemInfo?.ONKMJAGNECC).LastOrDefault(info => info != null);
        Check("S2 帕姆王: a Trailblaze purchase counts its cost and syncs the modifier",
            pamFirstBuy && pam.TrailblazeCostedCoin == pamFirstCost && pamDiff?.IANFMIJBKAF == pamFirstCost);
        GridFightSyncUpdateResultScNotify? pamUnlock = null;
        for (var buy = 0; buy < 1000 && pamUnlock == null; buy++)
        {
            if (!pam.BuyTrailblazeGood((uint)pam.TrailblazeGoods.Count - 1).Success) break;
            var notify = new GridFightSyncUpdateResultScNotify();
            pamRun.AppendPamKingUnlock(notify);
            if (notify.ResultList.Count > 0) pamUnlock = notify;
        }
        var pamResult = pamUnlock?.ResultList[0];
        Check("S2 帕姆王: reaching 500 grants 帕姆王 with update source 42 and the modifier UID as params[1]",
            pam.TrailblazeCostedCoin >= GridFightSession.PamKingCostedThreshold && pam.PamKingUnlocked &&
            pam.Roles.Any(role => role.RoleId == GridFightSession.PamKingRoleId) &&
            pamResult?.UpdateSource == GridFightUpdateSrcType.Hlhfimghhno &&
            pamResult.EffectParamList.SequenceEqual(new[] { GridFightSession.TrailblazeTraitId, pam.PamKingModifierUniqueId }) &&
            pamResult.UpdateDynamicList.Any(data => data.AddRoleInfo?.Id == GridFightSession.PamKingRoleId) &&
            pamResult.UpdateDynamicList.Any(data => data.GameDataItemInfo?.ONKMJAGNECC?.IANFMIJBKAF >= GridFightSession.PamKingCostedThreshold));
        var pamCount = pam.Roles.Count(role => role.RoleId == GridFightSession.PamKingRoleId);
        pam.BuyTrailblazeGood((uint)pam.TrailblazeGoods.Count - 1);
        var pamAgain = new GridFightSyncUpdateResultScNotify();
        pamRun.AppendPamKingUnlock(pamAgain);
        Check("S2 帕姆王: the unlock is granted only once",
            pamAgain.ResultList.Count == 0 && pam.Roles.Count(role => role.RoleId == GridFightSession.PamKingRoleId) == pamCount);
    }
    var (claraRun, clara) = Board((GridFightSession.ClaraRoleId, 1));
    claraRun.BuildSeasonTwoFollowUpSync();
    var claraPending = claraRun.BuildPendingAction();
    Check("S2 机心巧匠: Clara's first upgrade offers the three modules",
        claraPending?.PFEBKFJOGKO?.EGJDILDEAKP.OrderBy(id => id).SequenceEqual(new uint[] { 1001, 2001, 3001 }) == true);
    clara.SelectClaraEnhance(claraPending!.QueuePosition, 2001, out var enhanced);
    Check("S2 机心巧匠: the chosen module is recorded and synced",
        enhanced && EffectInfo(claraRun, GridFightSession.ClaraTraitId, GridFightSession.ClaraEffectId)
            ?.PFEBKFJOGKO.EMAGFIBLNCJ.Any(entry => entry.IGOHOJEJIGP == 2001 && entry.Num == 1) == true);

    var chimeraRoles = GameData.GridFightRoleBasicInfoData.Values
        .Where(role => role.SeasonID == season && role.TraitList.Contains(GridFightSession.ChimeraTraitId) && role.IsInPool)
        .Select(role => role.ID).Distinct().Take(3).ToArray();
    if (chimeraRoles.Length == 3)
    {
        var (chimeraRun, chimera) = Board((chimeraRoles[0], 1), (chimeraRoles[1], 2), (chimeraRoles[2], 3));
        chimeraRun.BuildSeasonTwoFollowUpSync();
        var chimeraPending = chimeraRun.BuildPendingAction()?.JMJOAOCJAAG;
        Check("S2 奇兽花园: first activation grants a free recruit with goods", chimeraPending is { OGDKJPDFFAI.Count: > 0 });
        var pickIndex = chimera.ChimeraGoods.ToList().FindIndex(good => good.Type == GridFightSession.ChimeraGoodChimera);
        if (pickIndex >= 0)
        {
            chimera.SelectChimeraGood(chimera.SeasonTwoPending!.QueuePosition, (uint)pickIndex, out var finishedChimera);
            Check("S2 奇兽花园: a recruited chimera appears in the garden",
                finishedChimera != 0 && chimera.Chimeras.Count == 1 &&
                EffectInfo(chimeraRun, GridFightSession.ChimeraTraitId, GridFightSession.ChimeraEffectId)
                    ?.GIKNCJMJLFE.BCGOAIPKGIK.Count == 1);
        }
    }

    // Jade's auto-open flag must not arrive together with a server prompt (the client then never closes the prompt).
    var (mixedRun, mixed) = Board((GridFightSession.JadeRoleId, 1), (GridFightSession.ClaraRoleId, 2));
    bool? AutoOpen(GridFightSyncUpdateResultScNotify? sync) => sync?.ResultList
        .SelectMany(result => result.UpdateDynamicList)
        .Where(data => data.TraitInfo?.TraitEffectInfo?.GDAMHENIIDJ != null)
        .Select(data => (bool?)data.TraitInfo.TraitEffectInfo.GDAMHENIIDJ.KPMKFGCFMHC)
        .LastOrDefault();
    var firstFlush = mixedRun.BuildSeasonTwoFollowUpSync();
    var claraQueue = mixed.SeasonTwoPending?.QueuePosition ?? 0;
    Check("S2 黑市主理人: shop auto-open waits while Clara's prompt is open",
        claraQueue != 0 && AutoOpen(firstFlush) == false);
    mixed.SelectClaraEnhance(claraQueue, 1001, out _);
    Check("S2 黑市主理人: shop auto-open follows once the prompt is answered",
        AutoOpen(mixedRun.BuildSeasonTwoFollowUpSync()) == true);
    Check("S2 pending: a resent answer for a finished prompt is recognised",
        mixed.IsFinishedSeasonTwoPending(claraQueue) && !mixed.IsFinishedSeasonTwoPending(claraQueue + 100));

    // 幻乐之主: an Aha on the board may put an Aha good into a shop roll (3-cost 20%); copies turn into growth.
    var (ahaRun, aha) = Board((151102, 1));
    ahaRun.BuildSeasonTwoFollowUpSync();
    var ahaRolls = 0;
    for (var roll = 0; roll < 400; roll++)
    {
        aha.RefreshShop(false);
        if (aha.Shop.Any(slot => GridFightSession.IsAhaGoods(slot.SpecialGoodsId))) ahaRolls++;
    }
    Check("S2 幻乐之主: a 3-cost Aha glances at about one shop roll in five, not every roll",
        aha.AhaShopGlanceChance == 0.2 && ahaRolls is >= 40 and <= 120);
    for (var roll = 0; roll < 100 && !aha.Shop.Any(slot => GridFightSession.IsAhaGoods(slot.SpecialGoodsId)); roll++)
        aha.RefreshShop(false);
    var ahaSlot = aha.Shop.FirstOrDefault(slot => GridFightSession.IsAhaGoods(slot.SpecialGoodsId));
    var ahaGoldBefore = aha.Gold;
    Check("S2 幻乐之主: the shop offers an Aha good and it can be bought for its price",
        ahaSlot != null && aha.BuySpecialGoods([ahaSlot.Index]).Count == 1 && ahaSlot.SoldOut &&
        aha.RefreshShop(true));
    // The rolled Aha good is random and some hand out Aha copies (346 星神降临, 349 球球了！), so the copy rule runs on
    // its own board.
    var (ahaCopyRun, ahaCopy) = Board((151102, 1));
    ahaCopyRun.BuildSeasonTwoFollowUpSync();
    AddRole(ahaCopy, 151102);
    ahaCopyRun.BuildSeasonTwoFollowUpSync();
    Check("S2 幻乐之主: a second Aha becomes 幻月之力 instead of a merge",
        ahaCopy.Roles.Count(role => role.RoleId is 151102 or 151112 or 151122 or 151132) == 1 && ahaCopy.AhaGrowth == 1 &&
        ahaCopy.RefreshRoleSavedValues().ContainsKey(GridFightSession.AhaGrowthSavedValueKey) == false);
    Check("S2 幻乐之主: GetData carries the 7-cost progress modifier",
        ahaCopyRun.BuildCurrentInfo()!.GameData.GameItemList.Any(item => item.GEKCCCPGBGJ?.GridFightTraitInfo.Count == 2));

    // 星神★阿哈 3星7费: every fielded role joins the leading trait.
    var (masterRun, master) = Board((roleA, 1));
    master.TryAddRole(151132, 3, out var masterAcquisition);
    masterAcquisition.AcquiredRoles.Single().Position = 2;
    masterRun.BuildSeasonTwoFollowUpSync();
    var ultra = masterRun.BuildPendingAction();
    var ultraQueue = master.SeasonTwoPending?.QueuePosition ?? 0;
    var confirmed = master.ConfirmAhaUltra(ultraQueue) == ultraQueue && ultraQueue != 0;
    Check("S2 幻乐之主: a 3-star 7-cost Aha opens the ultra block and confirming makes everyone join 幻月游戏 and 欢愉",
        ultra?.OKPMJOCEKBC?.Type == HOIDGPICBOJ.Jphakjkapei && confirmed &&
        master.Roles.Where(role => role.Position is >= 1 and <= 13).All(role =>
            master.ResolveRoleTraitIds(role).Contains(GridFightSession.MoonGameTraitId) &&
            master.ResolveRoleTraitIds(role).Contains(GridFightSession.ElationTraitId)) &&
        master.GetAhaUltraTraitGrants().Count == 2);

    // 主角光环: route nodes 1-2 / 2-1 carry the spotlight flag; entering one opens a two-card selection.
    var (spotRun, spot) = Board((roleA, 1), (roleB, 2));
    var spotNodes = spotRun.BuildCurrentInfo()!.GameInfoList
        .Where(info => info.LevelInfo != null)
        .SelectMany(info => info.LevelInfo.OOMMIIKPHBC)
        .Count(node => node.Param.HPNNLMBHOPF);
    spot.TryMoveToSection(1, 2, out _);
    spot.HandlePending(spot.PendingQueuePosition, GridFightPendingRequestKind.RoundBegin);
    for (var step = 0; step < 6 && spot.Phase != GridFightSessionPhase.Preparing; step++)
    {
        if (spot.Phase == GridFightSessionPhase.SelectingInvestment)
            spot.SelectInvestment(spot.PendingQueuePosition, spot.InvestmentOffer.First());
        else if (spot.Phase == GridFightSessionPhase.AwaitingBackToPrepare)
            spot.BackToPrepare();
        else
            spot.HandlePending(spot.PendingQueuePosition, GridFightPendingRequestKind.ReturnPreparation);
    }
    // The client asks for the selection itself (8508 from the "选择主角" battle button after its spotlight hint), so
    // node entry must not push it, and the battle stays closed until it is made.
    var spotEntrySync = spotRun.BuildSeasonTwoFollowUpSync();
    var spotNodeFlagged = spotRun.BuildCurrentInfo()!.GameInfoList.Where(info => info.LevelInfo != null)
        .SelectMany(info => info.LevelInfo.OOMMIIKPHBC)
        .Single(node => node.DCAHJELNJNP == 1 && node.SectionId == 2).Param.HPNNLMBHOPF;
    Check("S2 主角光环: entering the node pushes no selection, keeps the node flag and keeps the battle closed",
        spot.SeasonTwoPending == null && spotNodeFlagged && spot.IsSpotLightSelectionDue && !spot.EnterBattle() &&
        spotEntrySync?.ResultList.SelectMany(result => result.UpdateDynamicList).All(data => data.PendingAction == null) != false);
    var spotEntered = spot.StartSpotLightFromEnter();
    var spotOpenSync = spotRun.BuildSpotLightSync(null, 0);
    var spotPending = spotRun.BuildPendingAction()?.EEKMIAMGHFL;
    Check("S2 主角光环: 8508 opens the node selection",
        spotEntered && spot.IsSpotLightSelecting && spotPending != null &&
        spotOpenSync.ResultList.SelectMany(result => result.UpdateDynamicList).Any(data => data.PendingAction?.EEKMIAMGHFL != null));
    var spotQueue = spot.SeasonTwoPending?.QueuePosition ?? 0;
    // The role is chosen by dragging it onto the spotlight stage; the client refuses board drags under the gameplay
    // lock, so the selection must stay unlocked and the server must accept team edits while it is open.
    var spotLocks = spotOpenSync?.ResultList.SelectMany(result => result.UpdateDynamicList)
        .Where(data => data.LockInfo != null).Select(data => data.LockInfo).ToList() ?? [];
    var spotBasicLock = spotRun.BuildCurrentInfo()!.GameInfoList.First(info => info.BasicInfo != null).BasicInfo.LockInfo;
    var spotRoleB = spot.Roles.First(role => role.RoleId == roleB);
    var spotMoved = spot.UpdatePositions([(spotRoleB.UniqueId, 5)]).Success;
    var spotBattleBlocked = !spot.EnterBattle();
    Check("S2 主角光环: the open selection sends no gameplay lock and still allows moving roles, but not entering battle",
        spotPending != null && spotLocks.All(info => info.LockType == GridFightLockType.None) &&
        spotBasicLock?.LockType != GridFightLockType.Gameplay && spotMoved && spotRoleB.Position == 5 &&
        spotBattleBlocked);
    var spotChanges = spotPending == null ? null
        : spot.SelectSpotLight(spotQueue, spotPending.HPKFKBKOADP[0].IHHNLLAJAGB, roleA, out _);
    var spotRole = spot.Roles.First(role => role.RoleId == roleA);
    Check("S2 主角光环: two spotlight nodes, a two-card selection on entry and the crown goes to the chosen role",
        spotNodes == 2 && spotPending?.HPKFKBKOADP.Count == 2 && spotChanges?.Added.Count == 1 &&
        spot.GetSpotLightIdForRole(spotRole.UniqueId) == spotPending.HPKFKBKOADP[0].IHHNLLAJAGB &&
        spotRun.BuildCurrentInfo()!.GameInfoList.Any(info => info.LEGMAJLHBLA?.FNNEPMAPOOF.Count == 1));
    // While the current node has HasSpotLight the S2 battle button sends 8508 instead of starting the battle, so the
    // finished selection must clear the flag on 1-2 (route node update 1779 and GetData) and keep it on 2-1.
    var spotRouteUpdates = spotChanges == null ? []
        : spotRun.BuildSpotLightSync(spotChanges, 0).ResultList.SelectMany(result => result.UpdateDynamicList)
            .Where(data => data.KBHOBGHKIBA != null).Select(data => data.KBHOBGHKIBA.DBIIPEEDBHI).ToList();
    var spotRouteAfter = spotRun.BuildCurrentInfo()!.GameInfoList
        .Where(info => info.LevelInfo != null)
        .SelectMany(info => info.LevelInfo.OOMMIIKPHBC)
        .ToList();
    Check("S2 主角光环: a finished selection clears the node's spotlight flag so the battle button starts the battle",
        spotRouteUpdates.Count == 1 && spotRouteUpdates[0].DCAHJELNJNP == 1 && spotRouteUpdates[0].SectionId == 2 &&
        !spotRouteUpdates[0].Param.HPNNLMBHOPF &&
        spotRouteAfter.Single(node => node.DCAHJELNJNP == 1 && node.SectionId == 2).Param.HPNNLMBHOPF == false &&
        spotRouteAfter.Single(node => node.DCAHJELNJNP == 2 && node.SectionId == 1).Param.HPNNLMBHOPF &&
        spot.ResolveStaleSpotLightNode() == null && spot.EnterBattle());

    // 本局奇遇: partner effects that change server rules.
    GridFightSession? RunWithPetPal(uint effectId)
    {
        for (var seed = 0; seed < 3000; seed++)
        {
            var manager = new GridFightManager(new PlayerInstance(new PlayerData { Uid = 1 }), new Random(seed));
            if (!manager.Start(catalog.CurrentSeason, catalog.HighestDivision.ID, false)) continue;
            var session = manager.Session!;
            if (session.PetPalEffectId != effectId) continue;
            session.HandlePending(session.PendingQueuePosition, GridFightPendingRequestKind.PortalSelect,
                session.PortalOffer.First(id => id != GridFightSession.BlueSeaPortalId));
            session.HandlePending(session.PendingQueuePosition, GridFightPendingRequestKind.RoundBegin);
            session.GrantOfficialInitialRoundRewards(out _);
            session.HandlePending(session.PendingQueuePosition, GridFightPendingRequestKind.ReturnPreparation);
            return session.Phase == GridFightSessionPhase.Preparing ? session : null;
        }
        return null;
    }
    if (RunWithPetPal(109004) is { } fanGifts)
    {
        fanGifts.Roles.Clear();
        AddRole(fanGifts, roleA).Position = 1;
        var supplied = false;
        for (var step = 0; step < 40 && !supplied; step++)
        {
            switch (fanGifts.Phase)
            {
                case GridFightSessionPhase.Preparing:
                    fanGifts.EnterBattle();
                    fanGifts.ResolveBattle(true, new GridFightBattleStatistics(100, [], [], [], [], 1, new Dictionary<string, uint>()));
                    break;
                case GridFightSessionPhase.SelectingSupply:
                    var picked = fanGifts.SupplyOffer[0];
                    supplied = fanGifts.SupplyOffer.All(offer => offer.Star == 2) &&
                               fanGifts.SelectSupply(fanGifts.PendingQueuePosition, [1]).Success &&
                               fanGifts.Roles.Any(role => role.RoleId == picked.RoleId && role.Star >= 2);
                    step = 40;
                    break;
                case GridFightSessionPhase.SelectingInvestment:
                    fanGifts.SelectInvestment(fanGifts.PendingQueuePosition, fanGifts.InvestmentOffer.First());
                    break;
                case GridFightSessionPhase.SelectingEncounter:
                    fanGifts.SelectEncounter(fanGifts.PendingQueuePosition, fanGifts.EncounterOptions.First().EliteBranchId);
                    break;
                case GridFightSessionPhase.AwaitingBackToPrepare:
                    fanGifts.BackToPrepare();
                    break;
                default:
                    fanGifts.HandlePending(fanGifts.PendingQueuePosition, GridFightPendingRequestKind.RoundBegin);
                    fanGifts.HandlePending(fanGifts.PendingQueuePosition, GridFightPendingRequestKind.ReturnPreparation);
                    break;
            }
        }
        Check("S2 奇遇 绯绯狐 109004: every supply offers 2-star roles and grants them", supplied);
    }
    if (RunWithPetPal(102001) is { } guarded)
    {
        guarded.Roles.Clear();
        AddRole(guarded, roleA).Position = 1;
        var hpBefore = guarded.LineupHp;
        var lost = guarded.EnterBattle() &&
                   guarded.ResolveBattle(true, new GridFightBattleStatistics(10, [], [], [], [], 1, new Dictionary<string, uint>()))
                       is { Success: true, IsPerfectFinish: true };
        Check("S2 奇遇 汪汪丹 102001: the first failed battle counts as a perfect finish", lost && guarded.LineupHp >= hpBefore);
    }
    if (RunWithPetPal(104003) is { } coffee && coffee.BonfireNodes.FirstOrDefault() is { } bonfireNode)
    {
        coffee.TryMoveToSection(bonfireNode.ChapterId, bonfireNode.SectionId, out _);
        coffee.HandlePending(coffee.PendingQueuePosition, GridFightPendingRequestKind.RoundBegin);
        for (var step = 0; step < 6 && coffee.Phase != GridFightSessionPhase.Preparing; step++)
        {
            if (coffee.Phase == GridFightSessionPhase.SelectingInvestment)
                coffee.SelectInvestment(coffee.PendingQueuePosition, coffee.InvestmentOffer.First());
            else if (coffee.Phase == GridFightSessionPhase.AwaitingBackToPrepare)
                coffee.BackToPrepare();
            else
                coffee.HandlePending(coffee.PendingQueuePosition, GridFightPendingRequestKind.ReturnPreparation);
        }
        coffee.RefreshSeasonTwoTraits();
        var bonfirePending = coffee.SeasonTwoPending;
        var bootsBefore = coffee.Equipments.Count(item => item.EquipmentId == 47030101);
        var boots = bonfirePending == null ? null : coffee.BonfireOptions.FirstOrDefault(option => option.OptionId == 2);
        var gained = boots != null && coffee.SelectBonfireOption(bonfirePending!.QueuePosition, boots.EventId, boots.OptionId).Success;
        var section = coffee.SectionId;
        var left = gained && coffee.LeaveBonfire(bonfirePending!.QueuePosition, out _) == bonfirePending.QueuePosition;
        Check("S2 幕后茶歇: the partner's node opens a bonfire, its option pays out and leaving moves on",
            bonfirePending?.Kind == GridFightSeasonTwoPendingKind.Bonfire && bonfireNode.EventId == 10400301 &&
            coffee.Equipments.Count(item => item.EquipmentId == 47030101) == bootsBefore + 3 && left &&
            coffee.SectionId != section && coffee.Phase == GridFightSessionPhase.AwaitingBattleRound);
    }
    if (RunWithPetPal(105001) is { } blackHole)
    {
        blackHole.RefreshSeasonTwoTraits();
        Check("S2 奇遇 杨叔 105001: the run starts with 坍缩奇点",
            blackHole.Equipments.Count(item => item.EquipmentId == 353005) == 1 &&
            blackHole.RefreshSeasonTwoTraits().AddedEquipments.Count == 0);
    }

    // Every season-2 run snapshot (GetData) and battle survives a protobuf round trip with all effects present.
    var everyone = Board((GridFightSession.DanHengMainRoleId, 1), (GridFightSession.PhainonRoleId, 2),
        (GridFightSession.JadeRoleId, 3), (150402, 5), (GridFightSession.ClaraRoleId, 6));
    everyone.Manager.BuildSeasonTwoFollowUpSync();
    var everyoneInfo = everyone.Manager.BuildCurrentInfo()!;
    Check("S2 GetData with all 5-cost uniques round-trips and carries the context values",
        GridFightCurrentInfo.Parser.ParseFrom(everyoneInfo.ToByteArray()).Equals(everyoneInfo) &&
        everyoneInfo.GameInfoList.Any(info => info.ContextValueInfo is { FMNPBOJGLFI: > 0 } context &&
                                              context.DKMNOPCJDDC.Count == 2 && context.ContextValueVersion != 0));
}

Console.WriteLine($"GridFight regression: {checks - failures.Count}/{checks} passed.");
return failures.Count == 0 ? 0 : 1;
