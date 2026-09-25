using System.Net;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using March7thHoney.Command;
using March7thHoney.Command.Command;
using March7thHoney.Command.Command.Cmd;
using March7thHoney.Data;
using March7thHoney.Data.Config.Scene;
using March7thHoney.Data.Custom;
using March7thHoney.Data.Excel;
using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Database.Quests;
using March7thHoney.Enums;
using March7thHoney.Enums.Scene;
using March7thHoney.Enums.Task;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.GameServer.Game.RogueMagic;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server;
using March7thHoney.GameServer.Server.Packet;
using March7thHoney.GameServer.Server.Packet.Recv.Activity;
using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Internationalization;
using March7thHoney.Proto;
using March7thHoney.ProtoOrganizer;
using March7thHoney.Kcp;
using March7thHoney.Kcp.KcpSharp;
using Google.Protobuf;
using MemoryPack;
using March7thHoney.Util;
using March7thHoney.WebServer;
using March7thHoney.WebServer.Handler;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RSA = System.Security.Cryptography.RSA;

// Isolated configuration, random test keys, temporary SQLite database, loopback-only HTTP.
// The checked-out Resource.bin is read, never regenerated or changed.
var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var scratch = Path.Combine(Path.GetTempPath(), "m7h-login-scene-regression-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(scratch);
Environment.CurrentDirectory = scratch;
var config = ConfigManager.Config;
config.Path.ConfigPath = scratch;
config.Path.LanguagePath = Path.Combine(repository, "Config/Languages");
config.Path.ResourcePath = Path.Combine(repository, "Resources");
config.Path.DatabasePath = scratch;
config.Path.LogPath = Path.Combine(scratch, "Logs");
config.Database.DatabaseType = "sqlite";
config.Database.DatabaseName = "regression.db";
config.HttpServer.UseSSL = false;
config.MuipServer.AdminKey = "local-login-scene-regression-only";
config.GameServer.UsePacketEncryption = false;
config.ServerOption.Language = "CHS";
config.ServerOption.WelcomeMail.Enabled = false;
config.ServerOption.EnableMission = false;
config.ServerOption.AutoCreateUser = true;
config.ServerOption.Auth.PasswordHashIterations = 10000;
config.ServerOption.Auth.SendVerificationEmailOnRegister = false;
config.ServerOption.Auth.AntiAbuse.RateLimitEnabled = false;
config.ServerOption.Auth.AntiAbuse.LoginRateLimitEnabled = false;
config.ServerOption.Auth.AntiAbuse.LimitRegistrationPerIdentity = false;
config.ServerOption.Auth.AntiAbuse.RequireRegistrationIdentity = false;
config.WebSecurity.EnableRsaLoginDecrypt = true;
I18NManager.LoadLanguage();
new DatabaseHelper().Initialize(startSaveThread: false);
AuthRateLimiter.Initialize();

var failures = new List<string>();
var checks = 0;
void Check(string name, bool pass)
{
    checks++;
    Console.WriteLine($"{(pass ? "PASS" : "FAIL")} {name}");
    if (!pass) failures.Add(name);
}
int AccountCount() => DatabaseHelper.GetAllInstance<AccountData>()!.Count;
string Encrypt(RSA key, string value, RSAEncryptionPadding? padding = null) =>
    Convert.ToBase64String(key.Encrypt(Encoding.UTF8.GetBytes(value), padding ?? RSAEncryptionPadding.Pkcs1));
void CheckRejected(string name, (string account, string password, bool decryptFailed) result) =>
    Check(name, result.decryptFailed && result.account == "" && result.password == "");

using var key = RSA.Create(2048);
using var otherKey = RSA.Create(2048);
using var legacyKey = RSA.Create(1024);
config.WebSecurity.RsaPrivateKeyPem = key.ExportRSAPrivateKeyPem();
const string Username = "rsa-regression";
const string Password = "Regression-Password-42";
foreach (var padding in new[] { RSAEncryptionPadding.Pkcs1, RSAEncryptionPadding.OaepSHA1, RSAEncryptionPadding.OaepSHA256 })
{
    var result = LoginCryptoHelper.TryDecryptCredentialsStrict(Encrypt(key, Username, padding), Encrypt(key, Password, padding), true);
    Check($"RSA {padding} decrypts both credentials", !result.decryptFailed && result.account == Username && result.password == Password);
}
CheckRejected("wrong 2048-bit key never returns ciphertext", LoginCryptoHelper.TryDecryptCredentialsStrict(
    Encrypt(otherKey, Username), Encrypt(otherKey, Password), true));
CheckRejected("legacy 1024-bit key mismatch never returns ciphertext", LoginCryptoHelper.TryDecryptCredentialsStrict(
    Encrypt(legacyKey, Username), Encrypt(legacyKey, Password), true));
CheckRejected("one failed credential rejects the whole login", LoginCryptoHelper.TryDecryptCredentialsStrict(
    Encrypt(key, Username), Encrypt(otherKey, Password), true));
CheckRejected("explicit encrypted password cannot fall back to plaintext", LoginCryptoHelper.TryDecryptCredentialsStrict(
    Username, Password, true, true));
CheckRejected("explicit malformed Base64 password is rejected", LoginCryptoHelper.TryDecryptCredentialsStrict(
    Username, "not-base64!", true, true));
CheckRejected("encrypted non-UTF8 plaintext is rejected", LoginCryptoHelper.TryDecryptCredentialsStrict(
    Username, Convert.ToBase64String(key.Encrypt(new byte[] { 0xff, 0xfe }, RSAEncryptionPadding.Pkcs1)), true, true));
config.WebSecurity.RsaPrivateKeyPem = "";
CheckRejected("missing private key rejects encrypted credentials", LoginCryptoHelper.TryDecryptCredentialsStrict(
    Encrypt(key, Username), Encrypt(key, Password), true));
var plain = LoginCryptoHelper.TryDecryptCredentialsStrict("YWJjZA==", "cGFzc3dvcmQ=", true);
Check("short Base64-looking plaintext works without a private key", !plain.decryptFailed && plain.account == "YWJjZA==" && plain.password == "cGFzc3dvcmQ=");
config.WebSecurity.RsaPrivateKeyPem = "not-a-private-key";
CheckRejected("invalid PEM rejects encrypted login without throwing", LoginCryptoHelper.TryDecryptCredentialsStrict(
    Encrypt(key, Username), Encrypt(key, Password), true));
config.WebSecurity.RsaPrivateKeyPem = legacyKey.ExportRSAPrivateKeyPem();
var legacy = LoginCryptoHelper.TryDecryptCredentialsStrict(Encrypt(legacyKey, Username), Encrypt(legacyKey, Password), true);
Check("matching legacy 1024-bit key is still supported", !legacy.decryptFailed && legacy.account == Username && legacy.password == Password);
config.WebSecurity.RsaPrivateKeyPem = key.ExportRSAPrivateKeyPem();

await using (var app = WebProgram.BuildWebApp([], 0, "http://127.0.0.1:0"))
{
    await app.StartAsync();
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var client = new HttpClient { BaseAddress = new Uri(address) };
    async Task<JsonElement> Login(string path, string account, string password, bool crypto = false)
    {
        using var response = await client.PostAsJsonAsync(path, new { account, password, is_crypto = crypto, device_id = "local-regression" });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }
    string[] routes = ["/hkrpg_global/mdk/shield/api/login",
        "/hkrpg_global/account/ma-passport/api/appLoginByPassword",
        "/hkrpg_cn/account/ma-passport/api/appLoginByPassword",
        "/account/ma-cn-passport/app/loginByPassword"];
    foreach (var path in routes)
    {
        var baseline = AccountCount();
        foreach (var badKey in new[] { otherKey, legacyKey })
        for (var retry = 0; retry < 2; retry++)
        {
            var response = await Login(path, Encrypt(badKey, Username), Encrypt(badKey, Password), true);
            Check($"{path}: wrong {badKey.KeySize}-bit key retry {retry + 1} rejected before registration",
                response.GetProperty("retcode").GetInt32() == -202 && AccountCount() == baseline);
        }
        var mixedFailure = await Login(path, "must-not-register", Encrypt(otherKey, Password), true);
        Check($"{path}: failed encrypted password cannot create a plaintext account",
            mixedFailure.GetProperty("retcode").GetInt32() == -202 && AccountCount() == baseline);
    }
    foreach (var path in routes)
    {
        for (var retry = 0; retry < 2; retry++)
        {
            var response = await Login(path, Encrypt(key, Username), Encrypt(key, Password), true);
            var uid = AccountData.GetAccountByUserName(Username)?.Uid;
            Check($"{path}: randomized valid ciphertext retry {retry + 1} resolves to one account",
                response.GetProperty("retcode").GetInt32() == 0 && uid != null && AccountCount() == 1);
        }
    }
    var stableUid = AccountData.GetAccountByUserName(Username)!.Uid;
    var mixed = await Login(routes[0], Username, Encrypt(key, Password), true);
    Check("Shield plaintext account plus encrypted password preserves UID",
        mixed.GetProperty("retcode").GetInt32() == 0 && AccountData.GetAccountByUserName(Username)!.Uid == stableUid);
    config.WebSecurity.RsaPrivateKeyPem = "";
    foreach (var path in routes)
    {
        var response = await Login(path, Encrypt(key, "missing-key"), Encrypt(key, Password), true);
        Check($"{path}: missing private key creates no account", response.GetProperty("retcode").GetInt32() == -202 && AccountCount() == 1);
        var plaintext = await Login(path, Username, Password);
        Check($"{path}: ordinary plaintext login remains compatible", plaintext.GetProperty("retcode").GetInt32() == 0);
    }
    config.WebSecurity.EnableRsaLoginDecrypt = false;
    var disabled = await Login(routes[0], Encrypt(key, Username), Encrypt(key, Password), true);
    Check("Shield is_crypto cannot bypass disabled decryption", disabled.GetProperty("retcode").GetInt32() == -202 && AccountCount() == 1);
    await app.StopAsync();
}

SavedValueLoadCondition Condition(OperationEnum operation, params SavedValueCondition[] clauses) => new()
{
    Operation = operation, Conditions = clauses.ToList()
};
var a = new SavedValueCondition { SavedValueName = "a", Value = 1, Operation = CompareTypeEnum.Equal };
var b = new SavedValueCondition { SavedValueName = "b", Value = 1, Operation = CompareTypeEnum.Equal };
foreach (var av in new[] { 0, 1 })
foreach (var bv in new[] { 0, 1 })
{
    var values = new Dictionary<string, int> { ["a"] = av, ["b"] = bv };
    Check($"FSV AND truth table {av}/{bv}", Condition(OperationEnum.And, a, b).IsTrue(values) == (av == 1 && bv == 1));
    Check($"FSV OR truth table {av}/{bv}", Condition(OperationEnum.Or, a, b).IsTrue(values) == (av == 1 || bv == 1));
    Check($"FSV NOT truth table {av}/{bv}", Condition(OperationEnum.Not, a, b).IsTrue(values) == !(av == 1 || bv == 1));
}
Check("empty FSV condition preserves caller default", Condition(OperationEnum.And).IsTrue([])
    && !Condition(OperationEnum.And).IsTrue([], false));
Check("unsupported nonempty FSV operator cannot pass", !Condition(OperationEnum.Unknow, a).IsTrue(new() { ["a"] = 1 }));
var omitted = Condition(OperationEnum.And, new SavedValueCondition { SavedValueName = "a", Value = 1 });
Check("omitted comparison in existing cache means equality", !omitted.IsTrue([]) && omitted.IsTrue(new() { ["a"] = 1 }));
foreach (var (comparison, expected) in new[] { (CompareTypeEnum.Equal, false), (CompareTypeEnum.NotEqual, true),
    (CompareTypeEnum.Greater, true), (CompareTypeEnum.GreaterEqual, true), (CompareTypeEnum.Less, false), (CompareTypeEnum.LessEqual, false) })
{
    Check($"FSV comparison {comparison}", Condition(OperationEnum.And,
        new SavedValueCondition { SavedValueName = "x", Value = 1, Operation = comparison }).IsTrue(new() { ["x"] = 2 }) == expected);
}

string? OptionValue(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1], repository) : null;
}
if (OptionValue("--legacy-cache") is { } legacyCachePath)
{
    File.Copy(legacyCachePath, Path.Combine(scratch, "Resource.bin"));
    Check("legacy cache deserializes but is rejected when server atmosphere metadata is missing", !ResourceCache.LoadCache()
        && GameData.FloorInfoData["P10202_F10202001"].Groups[169].IsHoyoGroup == null);
}
// --resource-cache tests a freshly generated cache before it replaces the tracked Config/Resource.bin.
var cacheSource = OptionValue("--resource-cache") ?? Path.Combine(repository, "Config/Resource.bin");
File.Copy(cacheSource, Path.Combine(scratch, "Resource.bin"), overwrite: true);
var rawResourcePath = config.Path.ResourcePath;
config.Path.ResourcePath = Path.Combine(scratch, "no-raw-resource-json");
var loadedCache = ResourceCache.LoadCache();
Check("upgraded cache loads without the raw Resources tree", loadedCache);
if (!loadedCache) throw new InvalidOperationException("The checked-out 4.5 resource cache could not be loaded.");
config.Path.ResourcePath = rawResourcePath;
Check("cache preserves Hoyo atmosphere metadata", GameData.FloorInfoData["P10202_F10202001"].Groups[169].IsHoyoGroup == true);
var mapEntranceFiles = typeof(MapEntranceExcel).GetCustomAttribute<ResourceEntity>()!.FileName;
Check("map entrance loader includes the 4.5 LD table", mapEntranceFiles.Contains("MapEntranceLD.json"));

GameData.RogueMapGenData = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(
    File.ReadAllText(Path.Combine(repository, "Config/Custom/RogueMapGen.json")))!;
var rogueMap = new RogueInstance { MapId = 904 };
rogueMap.InitRooms();
Check("new Rogue maps resolve every site through the room pools",
    rogueMap.StartSiteId != 0 && rogueMap.Rooms.Count == GameData.RogueMapData[904].Count &&
    rogueMap.Rooms.Values.All(room => GameData.RogueRoomData.ContainsKey(room.RoomId)));

var defaultChessBranch = ChessRogueResources.ResolveBranchId(0);
Check("ChessRogue omitted dice branch resolves to a complete default",
    defaultChessBranch != 0 &&
    ChessRogueResources.DefaultSurfaces(defaultChessBranch).Count == ChessRogueResources.DiceSlotCount);
var rogueMagicQuery = RogueMagicProtoBuilder.BuildQueryInfo();
Check("RogueMagic map query includes every area and required nested metadata",
    rogueMagicQuery.DANNIDIPFAL.Count == GameData.RogueMagicAreaData.Count &&
    rogueMagicQuery.DANNIDIPFAL.Count > 0 && rogueMagicQuery.StoryInfo != null &&
    rogueMagicQuery.IHJKLHGMKBI?.JCOBJAMPOAL != null);
var absentFlag = new GroupInfo();
Check("old-cache metadata remains distinguishable from false", MemoryPackSerializer.Deserialize<GroupInfo>(
    MemoryPackSerializer.Serialize(absentFlag))!.IsHoyoGroup == null);
absentFlag.Load();
Check("omitted raw resource Hoyo flag normalizes to false", absentFlag.IsHoyoGroup == false);
var commonFlagFloor = new FloorInfo { GroupInstanceCommonMap = new()
    { ["1"] = new GroupInstanceCommonInfo { IsHoyoGroup = false } } };
var commonFlagGroup = new GroupInfo { Id = 1, IsHoyoGroup = true };
commonFlagFloor.StampCommonMapFields(commonFlagGroup);
Check("explicit floor metadata false overrides group-file true", commonFlagGroup.IsHoyoGroup == false);

// Inspect numeric wire tags independently of the generated property names/descriptors.
Dictionary<int, ulong> UIntFields(byte[] bytes)
{
    var fields = new Dictionary<int, ulong>();
    using var input = new CodedInputStream(bytes);
    uint tag;
    while ((tag = input.ReadTag()) != 0)
        if (WireFormat.GetTagWireType(tag) == WireFormat.WireType.Varint)
            fields.Add(WireFormat.GetTagFieldNumber(tag), input.ReadUInt64());
        else
            input.SkipLastField();
    return fields;
}
var successFields = UIntFields(new SceneMapInfo { FloorId = 10202001 }.ToByteArray());
Check("4.5 map success writes floor to wire field 14, not retcode field 6",
    successFields.GetValueOrDefault(14) == 10202001 && !successFields.ContainsKey(6));
var errorFields = UIntFields(new SceneMapInfo { FloorId = 10202001, Retcode = 2 }.ToByteArray());
Check("4.5 map error writes independent retcode to wire field 6",
    errorFields.GetValueOrDefault(14) == 10202001 && errorFields.GetValueOrDefault(6) == 2);

// Explicit field corrections must also survive a full same-seed regeneration, not just protoc.
TypeDef Definition(string name, string fields) => new()
{
    Name = name, Kind = DefKind.Message, Refs = [], Body = $"message {name} {{\n{fields}\n}}"
};
var rawMap = Definition("HEACNLOGGPH", "    uint32 JFFKLDOPFOJ = 14;\n    uint32 BPKNKCKFEIF = 6;");
var oldMap = Definition("SceneMapInfo", "    uint32 retcode = 14;\n    uint32 floor_id = 6;");
var translations = Translations.Load(Path.Combine(repository, "Proto/4.5v3/translations.txt"));
var migratedMap = SameSeedMigrator.Apply([rawMap], [rawMap], [oldMap], translations).Definitions.Single().Body;
Check("forced 4.5 field mappings override inferred older field meanings",
    migratedMap.Contains("floor_id = 14;") && migratedMap.Contains("retcode = 6;"));
var boundaryPacket = new PacketSyncServerSceneChangeNotify();
Check("4.5 scene boundary has cmdid 1491 and empty protobuf payload",
    boundaryPacket.CmdId == 1491 && boundaryPacket.Data.Length == 0 && CmdIds.SyncServerSceneChangeNotify == 1491);

var player = new PlayerInstance(60001);
await player.InitializeAsync();

var roguePlayer = new PlayerInstance(60002);
await roguePlayer.InitializeAsync();
var rogueAvatarIds = roguePlayer.AvatarManager!.Data.FormalAvatars
    .Select(avatar => avatar.BaseAvatarId).Distinct().Take(2).ToList();
var rogueStart = await roguePlayer.RogueManager!.StartRogue(193, 4, rogueAvatarIds, false);
Check("new Rogue area starts and enters its generated first room",
    rogueStart == Retcode.RetSucc && roguePlayer.RogueManager.RogueInstance?.MapId == 904 &&
    roguePlayer.SceneInstance?.EntryId != 0);

var chessPlayer = new PlayerInstance(60003);
await chessPlayer.InitializeAsync();
var chessAvatarIds = chessPlayer.AvatarManager!.Data.FormalAvatars
    .Select(avatar => avatar.BaseAvatarId).Distinct().Take(2).ToList();
var chessStart = await chessPlayer.ChessRogueManager!.StartRun(205, 6, 0, chessAvatarIds, []);
Check("ChessRogue start accepts the client's omitted dice branch",
    chessStart == Retcode.RetSucc && chessPlayer.ChessRogueManager.RunInstance?.BranchId == defaultChessBranch);

var magicPlayer = new PlayerInstance(60004);
await magicPlayer.InitializeAsync();
var magicAvatarIds = magicPlayer.AvatarManager!.Data.FormalAvatars
    .Select(avatar => avatar.BaseAvatarId).Distinct().Take(2).ToList();
var magicAreaId = GameData.RogueMagicAreaData.Keys.Min();
var magicStart = await magicPlayer.RogueMagicManager!.StartRun(magicAreaId, 1, [], magicAvatarIds);
Check("RogueMagic area data builds a playable run",
    magicStart.Ret == Retcode.RetSucc && magicStart.Run?.Layers.Count > 0);

var rogueTournLobby = new SceneInstance(player, GameData.MazePlaneData[10341], 10341001, 10341901);
var rogueTournLobbyGroup = rogueTournLobby.FloorInfo!.Groups[13];
var rogueTournEntrance = await rogueTournLobby.EntityLoader!.LoadProp(
    rogueTournLobbyGroup.PropList.Single(prop => prop.ID == 300001), rogueTournLobbyGroup);
var rogueTournEmblem = await rogueTournLobby.EntityLoader.LoadProp(
    rogueTournLobbyGroup.PropList.Single(prop => prop.ID == 300002), rogueTournLobbyGroup);
Check("RogueTourn lobby entrance and emblem default open",
    rogueTournEntrance?.State == PropStateEnum.Open && rogueTournEmblem?.State == PropStateEnum.Open);
var scene = new SceneInstance(player, GameData.MazePlaneData[10202], 10202001, 102020107);
var snapshot = scene.BuildClientFloorSavedData();
Check("Changle Tian restores floor-wide activity defaults without writing player saves",
    snapshot.GetValueOrDefault("LoadBianZhong") == 1 && snapshot.GetValueOrDefault("ChallengeStoryEntrance") == 1
    && snapshot.GetValueOrDefault("FSV_UmbrellaGroup_IsHidden") == 0
    && !player.SceneData!.FloorSavedData.ContainsKey(10202001));
var group170 = scene.FloorInfo!.Groups[170];
Check("production-cache Pure Fiction entrance is enabled with empty save", group170.SavedValueCondition.IsTrue(snapshot));
scene.EntityLoader!.LoadGroups.Add(170);
Check("production-cache Pure Fiction entrance props are actually created", (await scene.EntityLoader.LoadGroup(group170))
    ?.OfType<EntityProp>().Any(prop => prop.InstId == 300002 && prop.Excel.ID == 102226) == true);
player.SceneData!.FloorSavedData[10202001] = new() { ["ChallengeStoryEntrance"] = 0, ["LoadBianZhong"] = 0 };
Check("explicit saved zero still overrides floor-wide activity defaults",
    scene.BuildClientFloorSavedData().GetValueOrDefault("ChallengeStoryEntrance") == 0
    && scene.BuildClientFloorSavedData().GetValueOrDefault("LoadBianZhong") == 0
    && !group170.SavedValueCondition.IsTrue(scene.BuildClientFloorSavedData()));
player.SceneData.FloorSavedData.Remove(10202001);

var synthetic = new FloorInfo { FloorID = 99000001, StartGroupID = 901, DimensionList =
[
    new() { ID = 0, GroupIDList = [901], SavedValues = [new() { Name = "defaultOn", DefaultValue = 1 }, new() { Name = "zero", DefaultValue = 0 }] },
    new() { ID = 2, GroupIDList = [902], SavedValues = [new() { Name = "otherDimension", DefaultValue = 9 }] }
] };
var syntheticGroup = new GroupInfo
{
    Id = 901,
    LoadSide = GroupLoadSideEnum.Server,
    SavedValueCondition = Condition(OperationEnum.And,
        new SavedValueCondition { SavedValueName = "defaultOn", Value = 1, Operation = CompareTypeEnum.Equal }),
    PropList = [new PropInfo { ID = 300001, PropID = 100, LoadOnInitial = false }],
    NPCList = [new NpcInfo { ID = 400001, NPCID = 100, LoadOnInitial = false }]
};
synthetic.Groups[901] = syntheticGroup;
var syntheticScene = new SceneInstance(player, GameData.MazePlaneData[10202], synthetic.FloorID, 0) { FloorInfo = synthetic };
syntheticScene.EntityLoader = new SceneEntityLoader(syntheticScene) { LoadGroups = [901] };
typeof(PlayerInstance).GetProperty(nameof(PlayerInstance.SceneInstance))!.SetValue(player, syntheticScene);
var defaults = syntheticScene.BuildClientFloorSavedData();
Check("scene defaults come only from the active dimension", defaults.GetValueOrDefault("defaultOn") == 1
    && defaults.GetValueOrDefault("zero") == 0 && !defaults.ContainsKey("otherDimension"));
var syntheticEntities = await syntheticScene.EntityLoader.LoadGroup(syntheticGroup);
Check("entity loader evaluates nonzero resource defaults", syntheticEntities != null);
Check("ordinary LoadOnInitial=false server prop remains available", syntheticEntities?.OfType<EntityProp>()
    .Any(prop => prop.InstId == 300001) == true);
Check("ordinary LoadOnInitial=false server NPC remains available", syntheticEntities?.OfType<EntityNpc>()
    .Any(npc => npc.InstId == 400001) == true);
const int SyntheticProbeEntityId = 900001;
syntheticScene.Entities[SyntheticProbeEntityId] = new ProbeEntity { EntityId = SyntheticProbeEntityId, GroupId = 901 };
await syntheticScene.EntityLoader.SyncEntity();
Check("entity sync retains groups enabled by resource defaults", syntheticScene.Entities.ContainsKey(SyntheticProbeEntityId));
player.SceneData.FloorSavedData[synthetic.FloorID] = new() { ["defaultOn"] = 0 };
Check("persisted zero overrides a nonzero default", syntheticScene.BuildClientFloorSavedData()["defaultOn"] == 0);
await syntheticScene.EntityLoader.SyncEntity();
Check("entity sync unloads group when persisted switch becomes zero",
    !syntheticScene.Entities.ContainsKey(SyntheticProbeEntityId) && !syntheticScene.Groups.Contains(901)
    && syntheticScene.Entities.Values.All(entity => entity.GroupId != 901));
Check("entity loader respects persisted zero", await syntheticScene.EntityLoader.LoadGroup(syntheticGroup) == null);
var ordinaryAtmosphere = new GroupInfo { Id = 902, LoadSide = GroupLoadSideEnum.Server,
    Category = GroupCategoryEnum.Atmosphere, IsHoyoGroup = false };
synthetic.Groups[902] = ordinaryAtmosphere;
syntheticScene.EntityLoader.LoadGroups.Add(902);
Check("non-Hoyo atmosphere groups remain loadable", await syntheticScene.EntityLoader.LoadGroup(ordinaryAtmosphere) != null);
ordinaryAtmosphere.IsHoyoGroup = true;
await syntheticScene.EntityLoader.SyncEntity();
Check("sync retains ordinary empty Hoyo atmosphere groups", syntheticScene.Groups.Count(id => id == 902) == 1);
await syntheticScene.EntityLoader.UnloadGroup(902);
Check("Hoyo atmosphere alone does not block group activation", await syntheticScene.EntityLoader.LoadGroup(ordinaryAtmosphere) != null);
typeof(PlayerInstance).GetProperty(nameof(PlayerInstance.SceneInstance))!.SetValue(player, null);

var badEntry = await player.TryEnterScene(10001, 0, false);
Check("plane ID is not silently accepted as entrance ID", !badEntry.Success && !badEntry.IsOverMap);
var initial = await player.TryEnterScene(1000001, 0, false);
Check("valid scene entry reports actual success", initial.Success && player.Data.EntryId == 1000001);
var same = await player.TryEnterScene(1000001, 0, false, mapTp: true);
Check("same-scene teleport is success with IsOverMap=false", same.Success && !same.IsOverMap);
Check("legacy EnterScene API keeps its over-map return contract", !await player.EnterScene(1000001, 0, false));
var realEntry = await player.TryEnterScene(102020107, 0, false);
Check("Changle Tian scene can be built from production cache", realEntry.Success && realEntry.IsOverMap && player.Data.FloorId == 10202001);
Check("Changle Tian scene snapshot includes the enabled activity group", player.SceneInstance!.Groups.Contains(170)
    && player.SceneInstance.BuildClientFloorSavedData().GetValueOrDefault("ChallengeStoryEntrance") == 1);
var changle = player.SceneInstance!;
var excludedChangleProps = new[] { (144, 300003), (208, 300001), (208, 300002) };
foreach (var (groupId, instanceId) in excludedChangleProps)
    Check($"Changle Tian omits Android-incompatible prop {groupId}/{instanceId}", !changle.Entities.Values.OfType<EntityProp>()
        .Any(prop => prop.GroupId == groupId && prop.InstId == instanceId));
Check("ordinary Changle Tian puzzle prop remains available", changle.Entities.Values.OfType<EntityProp>()
    .Any(prop => prop.GroupId == 144 && prop.InstId == 300001));
Check("Changle Tian incompatible atmosphere group is not spawned", !changle.Groups.Contains(169)
    && changle.Entities.Values.All(entity => entity.GroupId != 169));
await changle.EntityLoader!.SyncEntity();
await changle.EntityLoader.SyncEntity();
Check("stage handler stays loaded exactly once across repeated sync", changle.Groups.Count(id => id == 208) == 1
    && await changle.EntityLoader.LoadGroup(changle.FloorInfo!.Groups[208]) == null);
Check("sync does not reintroduce incompatible Changle Tian content", !changle.Groups.Contains(169)
    && changle.Entities.Values.All(entity => entity.GroupId != 208 && entity.GroupId != 169
        && (entity.GroupId != 144 || entity is not EntityProp { InstId: 300003 })));
foreach (var (groupId, instanceId) in excludedChangleProps.Append((169, 300001)))
{
    var group = changle.FloorInfo!.Groups[groupId];
    Check($"direct creation cannot reintroduce incompatible prop {groupId}/{instanceId}",
        await changle.EntityLoader.LoadProp(group.PropList.Single(prop => prop.ID == instanceId), group) == null);
}
Check("forced group activation cannot reintroduce incompatible atmosphere",
    await changle.EntityLoader.LoadGroup(changle.FloorInfo!.Groups[169], forceLoad: true) == null);
// Simulate stale group membership and an entity created before the compatibility rule applied.
changle.Groups.Add(169);
changle.Entities[SyntheticProbeEntityId] = new ProbeEntity { EntityId = SyntheticProbeEntityId, GroupId = 169 };
await changle.EntityLoader.SyncEntity();
Check("sync removes stale incompatible atmosphere membership and entities", !changle.Groups.Contains(169)
    && changle.Entities.Values.All(entity => entity.GroupId != 169));
var stageGroup = changle.FloorInfo!.Groups[208];
Check("explicit creation cannot reintroduce the PCOnly capture", await changle.EntityLoader.LoadProp(
    stageGroup.PropList.Single(prop => prop.ID == 300001), stageGroup) == null);
await changle.EntityLoader.UnloadGroup(208);
var forcedStage = await changle.EntityLoader.LoadGroup(stageGroup, forceLoad: true);
Check("forced stage activation keeps an empty handler without incompatible captures", forcedStage is { Count: 0 }
    && changle.Groups.Count(id => id == 208) == 1);
await changle.EntityLoader.UnloadGroup(144);
var forcedPuzzle = await changle.EntityLoader.LoadGroup(changle.FloorInfo!.Groups[144], forceLoad: true);
Check("forced puzzle activation retains ordinary prop but excludes incompatible prop", forcedPuzzle?.OfType<EntityProp>()
    .Any(prop => prop.InstId == 300001) == true && forcedPuzzle.All(entity => entity is not EntityProp { InstId: 300003 }));
var elevatorScene = new SceneInstance(player, GameData.MazePlaneData[20002], 20002001, 2000201);
elevatorScene.EntityLoader!.LoadGroups.Add(8);
var elevatorGroup = elevatorScene.FloorInfo!.Groups[8];
Check("cache retains ordinary server Hoyo metadata", elevatorGroup.IsHoyoGroup == true
    && elevatorGroup.Category != GroupCategoryEnum.Atmosphere);
Check("ordinary server Hoyo elevator is not excluded", (await elevatorScene.EntityLoader.LoadGroup(elevatorGroup, true))
    ?.OfType<EntityProp>().Any(prop => prop.Excel.PropType == PropTypeEnum.PROP_ELEVATOR) == true);
var loginScenePacket = new PacketGetCurSceneInfoScRsp(player);
var loginScene = GetCurSceneInfoScRsp.Parser.ParseFrom(loginScenePacket.Data).Scene;
Check("login restoration also carries the 4.5 scene initialization value",
    UIntFields(loginScene.FOJJGJPIMBL.ToByteArray()).GetValueOrDefault(11) == 2);
bool ExcludesIncompatibleChangleContent(SceneInfo info) => info.EntityGroupList.All(group =>
    group.GroupId != 169 && (group.GroupId != 208 || group.EntityList.Count == 0)
    && (group.GroupId != 144 || group.EntityList.All(entity => entity.InstId != 300003)));
Check("login restoration excludes incompatible Changle Tian content", ExcludesIncompatibleChangleContent(loginScene));
Check("teleport snapshot excludes incompatible Changle Tian content", ExcludesIncompatibleChangleContent(
    EnterSceneByServerScNotify.Parser.ParseFrom(new PacketEnterSceneByServerScNotify(changle).Data).Scene));
var mapPacket = new PacketGetSceneMapInfoScRsp(new GetSceneMapInfoCsReq
    { SceneIdentifiers = { new SceneIdentifier { FloorId = 10202001 } } }, player);
var mapInfo = GetSceneMapInfoScRsp.Parser.ParseFrom(mapPacket.Data).SceneMapInfo.Single();
Check("real map response carries Changle Tian floor at tag 14 and success at tag 6",
    UIntFields(mapInfo.ToByteArray()).GetValueOrDefault(14) == 10202001 && mapInfo.Retcode == 0);
Console.WriteLine($"Changle Tian fixture: {changle.Entities.Count} total entities, " +
                  $"{changle.Entities.Values.OfType<EntityProp>().Count()} props, groups [{string.Join(',', changle.Groups.Order())}].");

bool HasSceneProp(SceneInfo info, int groupId, int instanceId, int propId) => info.EntityGroupList
    .Any(group => group.GroupId == groupId && group.EntityList.Any(entity =>
        entity.InstId == instanceId && entity.Prop?.PropId == propId));

foreach (var (entryId, floorId, groupId, savedName, propId) in new[]
{
    (102020107, 10202001, 170, "ChallengeStoryEntrance", 102226),
    (1030402, 10304001, 145, "ChallengeEntrance", 103249)
})
{
    var entered = await player.TryEnterScene(entryId, 0, false);
    var entryScene = player.SceneInstance!;
    var instanceId = floorId == 10202001 ? 300002 : 300001;
    Check($"challenge entrance {floorId}/{groupId} loads with no persisted switch", entered.Success
        && !player.SceneData.FloorSavedData.GetValueOrDefault(floorId, []).ContainsKey(savedName)
        && HasSceneProp(entryScene.ToProto(), groupId, instanceId, propId));
    var floorWideDefaults = entryScene.BuildClientFloorSavedData();
    Check($"activity floor {floorId} restores all zero defaults, not only the challenge switch",
        entryScene.FloorInfo!.FloorSavedValue.Where(value => value.DefaultValue == 0 && !value.Name.Contains("_IsHidden"))
            .All(value => floorWideDefaults.GetValueOrDefault(value.Name) == 1));
    var entryPacket = EnterSceneByServerScNotify.Parser.ParseFrom(new PacketEnterSceneByServerScNotify(entryScene).Data);
    Check($"teleport snapshot includes challenge entrance {floorId}/{groupId} and enabled switch",
        entryPacket.Scene.FloorSavedData.GetValueOrDefault(savedName) == 1
        && HasSceneProp(entryPacket.Scene, groupId, instanceId, propId));
    await entryScene.EntityLoader!.SyncEntity();
    await entryScene.EntityLoader.SyncEntity();
    var restoredScene = GetCurSceneInfoScRsp.Parser.ParseFrom(new PacketGetCurSceneInfoScRsp(player).Data).Scene;
    Check($"login snapshot after sync retains challenge entrance {floorId}/{groupId}",
        restoredScene.FloorSavedData.GetValueOrDefault(savedName) == 1
        && HasSceneProp(restoredScene, groupId, instanceId, propId));
    if (floorId == 10202001)
        Check("all activities stay enabled while teleport/login snapshots exclude incompatible content",
            ExcludesIncompatibleChangleContent(entryPacket.Scene) && ExcludesIncompatibleChangleContent(restoredScene)
            && restoredScene.FloorSavedData.GetValueOrDefault("LoadBianZhong") == 1);
    player.SceneData.FloorSavedData[floorId] = new() { [savedName] = 0 };
    await entryScene.EntityLoader.SyncEntity();
    Check($"explicit saved zero still unloads challenge entrance {floorId}/{groupId}",
        !entryScene.Groups.Contains(groupId) && !HasSceneProp(entryScene.ToProto(), groupId, instanceId, propId));
    player.SceneData.FloorSavedData.Remove(floorId);
    await entryScene.EntityLoader.SyncEntity();
    Check($"removing the saved override restores challenge entrance {floorId}/{groupId}",
        HasSceneProp(entryScene.ToProto(), groupId, instanceId, propId));
}

var starskiff = new SceneInstance(player, GameData.MazePlaneData[10201], 10201001, 1020101);
starskiff.EntityLoader!.LoadGroups.Add(280);
var starskiffGroup = starskiff.FloorInfo!.Groups[280];
var starskiffEntities = await starskiff.EntityLoader.LoadGroup(starskiffGroup);
Check("Starskiff Haven PCOnly capture is excluded while its empty handler loads",
    starskiffEntities is { Count: 0 } && starskiff.Groups.Contains(280));
await starskiff.EntityLoader.SyncEntity();
await starskiff.EntityLoader.SyncEntity();
Check("PCOnly-only handler remains loaded exactly once after sync", starskiff.Groups.Count(id => id == 280) == 1
    && starskiff.Entities.Values.All(entity => entity.GroupId != 280));
Check("explicit creation also excludes Starskiff Haven PCOnly capture", await starskiff.EntityLoader.LoadProp(
    starskiffGroup.PropList.Single(prop => prop.ID == 300001), starskiffGroup) == null);
await starskiff.EntityLoader.UnloadGroup(280);
Check("forced Starskiff Haven activation still excludes PCOnly", (await starskiff.EntityLoader.LoadGroup(
    starskiffGroup, forceLoad: true)) is { Count: 0 });

var previousScene = player.SceneInstance;
const int MissingEntry = 99000002;
GameData.MapEntranceData[MissingEntry] = new MapEntranceExcel { ID = MissingEntry, PlaneID = 10202, FloorID = 99000002 };
var noFloor = await player.TryEnterScene(MissingEntry, 0, false, storyLineId: 123456);
Check("missing floor fails without changing scene/story state", !noFloor.Success && ReferenceEquals(previousScene, player.SceneInstance)
    && player.StoryLineManager!.StoryLineData.CurStoryLineId == 0);
GameData.FloorInfoData["P10202_F99000002"] = new FloorInfo { FloorID = 99000002 };
Check("floor with no usable anchor fails", !(await player.TryEnterScene(MissingEntry, 0, false)).Success);
GameData.FloorInfoData.TryRemove("P10202_F99000002", out _);
GameData.MapEntranceData[MissingEntry].PlaneID = 99000002;
Check("missing plane fails", !(await player.TryEnterScene(MissingEntry, 0, false)).Success);
GameData.MapEntranceData.Remove(MissingEntry);
Check("unknown story line fails instead of false success", !(await player.TryEnterScene(1000001, 0, false, storyLineId: 123456)).Success);
Check("entry zero without story transition fails", !(await player.TryEnterScene(0, 0, false)).Success);
var loadingFloors = (HashSet<long>)typeof(PlayerInstance).GetField("_loadingFloors", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(player)!;
var floorKey = ((long)10202 << 32) | 10202001u;
loadingFloors.Add(floorKey);
Check("recursive scene-load guard is reported as failure", !(await player.TryEnterScene(102020107, 0, false)).Success);
loadingFloors.Remove(floorKey);

var sender = new ProbeSender();
var target = new ProbeConnection { Player = player };
var command = new CommandScene();
await command.ChangeScene(new CommandArg("10001", sender, target));
Check("invalid scene command reports failure, never entered", sender.Messages.Single() == I18NManager.Translate("Game.Command.Scene.SceneEnterFailed", "10001"));
sender.Messages.Clear();
await command.ChangeScene(new CommandArg("102020107", sender, target));
Check("same-scene command still reports successful entry", sender.Messages.Single() == I18NManager.Translate("Game.Command.Scene.SceneChanged", "102020107"));
sender.Messages.Clear();
await command.ReloadScene(new CommandArg("", sender, target));
Check("reload command still reports successful reload", sender.Messages.Single() == I18NManager.Translate("Game.Command.Scene.SceneReloaded"));
sender.Messages.Clear();
loadingFloors.Add(floorKey);
await command.ReloadScene(new CommandArg("", sender, target));
loadingFloors.Remove(floorKey);
Check("aborted reload reports failure", sender.Messages.Single() == I18NManager.Translate("Game.Command.Scene.SceneEnterFailed", "102020107"));
var failurePacket = new PacketEnterSceneScRsp(Retcode.RetFail);
Check("scene failure response encodes nonzero retcode", EnterSceneScRsp.Parser.ParseFrom(failurePacket.Data).Retcode != 0);

// Entry zero is a real protocol path for entering/leaving a story's saved scene.
var storyId = GameData.StoryLineFloorDataData.Keys.First(GameData.StoryLineData.ContainsKey);
player.StoryLineManager!.StoryLineData.RunningStoryLines[storyId] = new StoryLineInfo
{
    StoryLineId = storyId, SavedPlaneId = 10000, SavedFloorId = 10000000, SavedEntryId = 1000001,
    Lineup = player.LineupManager!.GetCurLineup()!.BaseAvatars!.ToList()
};
Check("invalid entrance cannot activate an otherwise valid story", !(await player.TryEnterScene(10001, 0, false, storyId)).Success
    && player.StoryLineManager.StoryLineData.CurStoryLineId == 0);
var storyEnter = await player.TryEnterScene(0, 0, false, storyId);
Check("entry-zero story transition preserves successful saved-scene teleport", storyEnter.Success && storyEnter.IsOverMap
    && player.StoryLineManager.StoryLineData.CurStoryLineId == storyId && player.Data.EntryId == 1000001);
var storyLeave = await player.TryEnterScene(0, 0, false);
Check("entry-zero story exit returns to original scene", storyLeave.Success && storyLeave.IsOverMap
    && player.StoryLineManager.StoryLineData.CurStoryLineId == 0 && player.Data.EntryId == 102020107);

// Exercise the real send path with two in-memory KCP peers: no sockets or production server.
var outbound = new LoopbackKcpTransport();
var inbound = new LoopbackKcpTransport();
var endpoint = new IPEndPoint(IPAddress.Loopback, 60002);
var options = new KcpConversationOptions { NoDelay = true, UpdateInterval = 10, DisableCongestionControl = true,
    SendWindow = 128, ReceiveWindow = 128, SendQueueSize = 256, ReceiveQueueSize = 256 };
using (var serverPeer = new KcpConversation(endpoint, outbound, options))
using (var clientPeer = new KcpConversation(endpoint, inbound, options))
{
    outbound.Peer = clientPeer;
    inbound.Peer = serverPeer;
    outbound.Start();
    inbound.Start();
    player.Connection = new ProbeConnection(serverPeer) { Player = player };
    async Task<List<ushort>> SentPackets(Func<Task> action)
    {
        await action();
        await player.SendPacket(new BasePacket(ushort.MaxValue)); // deterministic end-of-batch marker
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var buffer = new byte[March7thHoneyConnection.MaxPacketBytes];
        var ids = new List<ushort>();
        while (true)
        {
            var received = await clientPeer.ReceiveAsync(buffer, timeout.Token);
            if (received.TransportClosed || received.BytesReceived < 16)
                throw new InvalidDataException("KCP scene test did not receive a complete game packet.");
            var id = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(4));
            if (id == ushort.MaxValue) return ids;
            ids.Add(id);
        }
    }
    var teleportPackets = await SentPackets(async () =>
    {
        await player.TryEnterScene(1000001, 0, true);
        await player.SendPacket(new PacketEnterSceneScRsp());
    });
    Check("teleport sends 1491 immediately before scene notify and before response",
        teleportPackets.TakeLast(3).SequenceEqual(new ushort[]
            { CmdIds.SyncServerSceneChangeNotify, CmdIds.EnterSceneByServerScNotify, CmdIds.EnterSceneScRsp }));
    var movePackets = await SentPackets(async () => { await player.TryEnterScene(1000001, 0, true, mapTp: true); });
    Check("same-scene map move does not send a scene-change boundary", movePackets.Contains(CmdIds.SceneEntityMoveScNotify)
        && !movePackets.Contains(1491) && !movePackets.Contains(CmdIds.EnterSceneByServerScNotify));
    var invalidPackets = await SentPackets(async () => { await player.TryEnterScene(10001, 0, true); });
    Check("failed scene entry sends no boundary or scene snapshot", !invalidPackets.Contains(1491)
        && !invalidPackets.Contains(CmdIds.EnterSceneByServerScNotify));
    var quietPackets = await SentPackets(async () => { await player.TryEnterScene(102020107, 0, false); });
    Check("quiet scene build sends no boundary or scene snapshot", !quietPackets.Contains(1491)
        && !quietPackets.Contains(CmdIds.EnterSceneByServerScNotify));
    var restorePackets = await SentPackets(async () => { await player.SendPacket(new PacketGetCurSceneInfoScRsp(player)); });
    Check("login snapshot is not mistaken for a scene transition", restorePackets.SequenceEqual(new ushort[] { CmdIds.GetCurSceneInfoScRsp }));
    player.Connection = null;
    await outbound.StopAsync();
    await inbound.StopAsync();
}

// Role Trial (TrialActivity): start, battle stage choice, clear, reward claim, leave and relogin rescue, driven
// through the real request handlers and captured over the in-memory KCP pair.
Directory.CreateDirectory(Path.Combine(scratch, "Custom"));
File.Copy(Path.Combine(repository, "Config/Custom/ActivityConfig.json"), Path.Combine(scratch, "Custom/ActivityConfig.json"));
var schedule = ResourceManager.LoadCustomFile<ActivityConfig>("Activity", "ActivityConfig");
int[] trialModules = [2000502, 2000603, 2000903, 2001101, 2001102, 2001103, 2001104, 2001105, 2001106, 2001107,
    2001108, 2001109, 2001303, 2001703, 2002803, 2006001, 2006301, 2006302, 2008001, 2008601, 2008701, 2008901, 2009001];
Check("activity schedule lists every Role Trial module", schedule != null &&
    trialModules.All(id => schedule.ScheduleData.Any(row => row.ActivityId == id && row.PanelId is 20011 or 20064 or 20087 or 20089 or 20090)));

var validateCache = typeof(ResourceCache).GetMethod("ValidateLoadedCache", BindingFlags.Static | BindingFlags.NonPublic)!;
var fakeRaw = Path.Combine(scratch, "fake-raw");
Directory.CreateDirectory(Path.Combine(fakeRaw, "ExcelOutputGameCore"));
File.WriteAllText(Path.Combine(fakeRaw, "ExcelOutputGameCore/AvatarDemoConfig.json"), "[]");
var savedResourcePath = config.Path.ResourcePath;
config.Path.ResourcePath = fakeRaw;
Check("cache with AvatarDemoConfig StageType passes validation", (bool)validateCache.Invoke(null, null)!);
var stageTypes = GameData.AvatarDemoConfigData.ToDictionary(pair => pair.Key, pair => pair.Value.StageType);
foreach (var row in GameData.AvatarDemoConfigData.Values) row.StageType = "";
Check("stale cache without AvatarDemoConfig StageType is rejected when the raw table exists",
    !(bool)validateCache.Invoke(null, null)!);
config.Path.ResourcePath = Path.Combine(scratch, "no-raw-resource-json");
Check("cache-only deployments keep a cache without StageType", (bool)validateCache.Invoke(null, null)!);
foreach (var (id, type) in stageTypes) GameData.AvatarDemoConfigData[id].StageType = type;
config.Path.ResourcePath = savedResourcePath;
Check("trial stage 315130 carries StageType and SPList", GameData.AvatarDemoConfigData[315130].IsTrialActivity
    && GameData.AvatarDemoConfigData[315130].SPList.SequenceEqual([6500, 6500, 7500, 7000])
    && !GameData.AvatarDemoConfigData[313079].IsTrialActivity);
if (!GameData.AvatarDemoConfigData[315130].IsTrialActivity)
    throw new InvalidOperationException("The resource cache predates AvatarDemoConfig StageType/SPList. Regenerate " +
                                        "Config/Resource.bin or pass --resource-cache <regenerated cache>.");

var trialPlayer = new PlayerInstance(60005);
await trialPlayer.InitializeAsync();
const int TrialReturnEntry = 1000001;
await trialPlayer.TryEnterScene(TrialReturnEntry, 0, false);
var trialReturnPos = trialPlayer.Data.Pos!;
var trialData = trialPlayer.ActivityManager!.Data.TrialActivityData;
var trialOutbound = new LoopbackKcpTransport();
var trialInbound = new LoopbackKcpTransport();
using (var trialServerPeer = new KcpConversation(endpoint, trialOutbound, options))
using (var trialClientPeer = new KcpConversation(endpoint, trialInbound, options))
{
    trialOutbound.Peer = trialClientPeer;
    trialInbound.Peer = trialServerPeer;
    trialOutbound.Start();
    trialInbound.Start();
    var trialConnection = new ProbeConnection(trialServerPeer) { Player = trialPlayer };
    trialPlayer.Connection = trialConnection;
    async Task<List<(ushort Id, byte[] Body)>> Capture(Func<Task> action)
    {
        await action();
        await trialPlayer.SendPacket(new BasePacket(ushort.MaxValue));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var buffer = new byte[March7thHoneyConnection.MaxPacketBytes];
        var packets = new List<(ushort, byte[])>();
        while (true)
        {
            var received = await trialClientPeer.ReceiveAsync(buffer, timeout.Token);
            if (received.TransportClosed || received.BytesReceived < 16)
                throw new InvalidDataException("KCP trial test did not receive a complete game packet.");
            var id = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(4));
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(8));
            if (id == ushort.MaxValue) return packets;
            packets.Add((id, buffer.AsSpan(12, length).ToArray()));
        }
    }
    Task Send<TReq>(Handler handler, TReq req) where TReq : IMessage =>
        handler.OnHandle(trialConnection, [], req.ToByteArray());
    Task Start(int stageId) => Send(new HandlerStartTrialActivityCsReq(), new StartTrialActivityCsReq { StageId = (uint)stageId });
    StartTrialActivityScRsp StartRsp(List<(ushort Id, byte[] Body)> packets) =>
        StartTrialActivityScRsp.Parser.ParseFrom(packets.Last(p => p.Id == CmdIds.StartTrialActivityScRsp).Body);
    async Task<List<(ushort Id, byte[] Body)>> WinTrialBattle()
    {
        var monsters = trialPlayer.SceneInstance!.Entities.Values.OfType<EntityMonster>().Where(m => m.IsAlive).ToList();
        trialPlayer.BattleInstance = new BattleInstance(trialPlayer, trialPlayer.LineupManager!.GetCurLineup()!, monsters);
        return await Capture(async () => await trialPlayer.BattleManager!.EndBattle(
            new PVEBattleResultCsReq { EndStatus = BattleEndStatus.BattleEndWin, Stt = new() }));
    }

    foreach (var invalidStage in new[] { 313079, 1 })
    {
        var rejected = await Capture(() => Start(invalidStage));
        var rsp = StartRsp(rejected);
        Check($"trial start rejects non-trial stage {invalidStage} without side effects",
            rsp.Retcode == (uint)Retcode.RetReqParaInvalid && rsp.StageId == invalidStage && trialData.CurTrialStageId == 0
            && trialPlayer.ActivityManager.TrialActivityInstance == null
            && !rejected.Any(p => p.Id == CmdIds.ExtraLineupDestroyNotify || p.Id == CmdIds.EnterSceneByServerScNotify));
    }

    var started = await Capture(() => Start(315130));
    var startRsp = StartRsp(started);
    var trialLineup = trialPlayer.LineupManager!.GetCurLineup()!;
    Check("trial 315130 starts and records the world return point", startRsp.Retcode == 0 && startRsp.StageId == 315130
        && trialData.CurTrialStageId == 315130 && trialData.ReturnEntryId == TrialReturnEntry
        && trialPlayer.SceneInstance?.Excel.PlaneType == PlaneTypeEnum.TrialActivity);
    Check("trial lineup uses the stage's special trial avatars", trialLineup.IsExtraLineup()
        && trialLineup.BaseAvatars!.Select(a => a.SpecialAvatarId).SequenceEqual([2001381, 2001382, 2001383, 2001384]));
    var enterTrial = EnterSceneByServerScNotify.Parser.ParseFrom(started.Last(p => p.Id == CmdIds.EnterSceneByServerScNotify).Body);
    Check("trial team starts with SPList 6500/6500/7500/7000",
        enterTrial.Lineup.AvatarList.OrderBy(a => a.Slot).Select(a => a.SpBar.CurSp).SequenceEqual([6500u, 6500u, 7500u, 7000u]));
    Check("trial scene stages the stage's own monster event", trialPlayer.SceneInstance!.Entities.Values.OfType<EntityMonster>()
        .Any(m => m.GroupId == GameData.AvatarDemoConfigData[315130].MazeGroupID1 && m.EventId == 315130));
    Check("trial scene resolves world 999 to the return point's world", enterTrial.Scene.WorldId != 999
        && enterTrial.Scene.WorldId == (uint)trialPlayer.SceneInstance.ResolveWorldId());
    Check("start sends lineup destroy, scene change and response in order", started.Select(p => p.Id)
        .Where(id => id is CmdIds.ExtraLineupDestroyNotify or CmdIds.SyncServerSceneChangeNotify
            or CmdIds.EnterSceneByServerScNotify or CmdIds.StartTrialActivityScRsp)
        .SequenceEqual(new ushort[] { CmdIds.ExtraLineupDestroyNotify, CmdIds.SyncServerSceneChangeNotify,
            CmdIds.EnterSceneByServerScNotify, CmdIds.StartTrialActivityScRsp }));

    var runningInstance = trialPlayer.ActivityManager.TrialActivityInstance;
    var rejectedWhileRunning = StartRsp(await Capture(() => Start(313079)));
    Check("rejected start keeps the running trial", rejectedWhileRunning.Retcode == (uint)Retcode.RetReqParaInvalid
        && trialData.CurTrialStageId == 315130 && ReferenceEquals(runningInstance, trialPlayer.ActivityManager.TrialActivityInstance)
        && trialPlayer.LineupManager.GetCurLineup()!.IsExtraLineup());
    var dataRsp = GetTrialActivityDataScRsp.Parser.ParseFrom(new PacketGetTrialActivityDataScRsp(trialPlayer).Data);
    Check("trial data reports the running stage", dataRsp.ActivityStageId == 315130);

    var brokenStage = GameData.AvatarDemoConfigData[315130];
    var brokenEntrance = brokenStage.MapEntranceID;
    brokenStage.MapEntranceID = 99000003;
    var failedStart = StartRsp(await Capture(() => Start(315130)));
    brokenStage.MapEntranceID = brokenEntrance;
    Check("failed scene entry rolls back to the running trial", failedStart.Retcode == (uint)Retcode.RetFail
        && trialData.CurTrialStageId == 315130 && trialData.ReturnEntryId == TrialReturnEntry
        && trialPlayer.LineupManager.GetCurLineup()!.BaseAvatars!.Select(a => a.SpecialAvatarId).First() == 2001381);

    Check("switching trials keeps the original return point", StartRsp(await Capture(() => Start(315120))).Retcode == 0
        && trialData.CurTrialStageId == 315120 && trialData.ReturnEntryId == TrialReturnEntry);
    var trialMonsters = trialPlayer.SceneInstance!.Entities.Values.OfType<EntityMonster>().Where(m => m.IsAlive).ToList();
    BattleInstance? trialBattle = null;
    try { trialBattle = new BattleInstance(trialPlayer, trialPlayer.LineupManager.GetCurLineup()!, trialMonsters); }
    catch (Exception ex) { Console.WriteLine(ex); }
    Check("trial 315120 battle resolves stage 315121 from the monster event", trialBattle is { StageId: 315121, IsTrialActivityBattle: true }
        && trialBattle.EntityMonsters.Count > 0);

    var lost = trialPlayer.BattleInstance = new BattleInstance(trialPlayer, trialPlayer.LineupManager.GetCurLineup()!, trialMonsters);
    var lostPackets = await Capture(async () => await trialPlayer.BattleManager!.EndBattle(
        new PVEBattleResultCsReq { EndStatus = BattleEndStatus.BattleEndLose, Stt = new() }));
    Check("lost trial battle does not clear the trial", trialData.CurTrialStageId == 315120 && trialData.Activities.Count == 0
        && !lostPackets.Any(p => p.Id == CmdIds.CurTrialActivityScNotify));

    var hcoinBeforeClear = trialPlayer.Data.Hcoin;
    var finished = await WinTrialBattle();
    var finishOrder = finished.Select(p => p.Id).Where(id => id is CmdIds.TrialActivityDataChangeScNotify
        or CmdIds.SyncServerSceneChangeNotify or CmdIds.EnterSceneByServerScNotify or CmdIds.CurTrialActivityScNotify
        or CmdIds.PVEBattleResultScRsp).ToList();
    Check("trial clear sends data change, return scene, finish notify, then battle result", finishOrder.SequenceEqual(new ushort[] {
        CmdIds.TrialActivityDataChangeScNotify, CmdIds.SyncServerSceneChangeNotify, CmdIds.EnterSceneByServerScNotify,
        CmdIds.CurTrialActivityScNotify, CmdIds.PVEBattleResultScRsp }));
    var change = TrialActivityDataChangeScNotify.Parser.ParseFrom(finished.First(p => p.Id == CmdIds.TrialActivityDataChangeScNotify).Body);
    Check("data change notify carries the cleared stage and featured avatar", change.TrialActivityInfo?.StageId == 315120
        && !change.TrialActivityInfo.TakenReward && change.OBOPMFBGBAE.Any(a => a.AvatarId == 1512));
    var finishNotify = CurTrialActivityScNotify.Parser.ParseFrom(finished.First(p => p.Id == CmdIds.CurTrialActivityScNotify).Body);
    Check("finish notify reports the stage as finished", finishNotify.ActivityStageId == 315120
        && finishNotify.Status == TrialActivityStatus.Finish);
    Check("trial clear returns the player to the saved world point without the trial lineup",
        trialData.CurTrialStageId == 0 && trialPlayer.ActivityManager.TrialActivityInstance == null
        && trialPlayer.Data.EntryId == TrialReturnEntry && trialPlayer.Data.Pos!.X == trialReturnPos.X
        && trialPlayer.Data.Pos.Z == trialReturnPos.Z && !trialPlayer.LineupManager.GetCurLineup()!.IsExtraLineup()
        && trialPlayer.LineupManager.GetExtraLineup(ExtraLineupType.LineupStageTrial) == null
        && trialPlayer.Data.Hcoin == hcoinBeforeClear);
    await Capture(() => Start(315120));
    await WinTrialBattle();
    Check("clearing a stage again keeps one record", trialData.Activities.Count(r => r.StageId == 315120) == 1);
    var afterClear = GetTrialActivityDataScRsp.Parser.ParseFrom(new PacketGetTrialActivityDataScRsp(trialPlayer).Data);
    Check("trial data lists the record and the featured avatar", afterClear.ActivityStageId == 0
        && afterClear.TrialActivityInfoList.Any(i => i.StageId == 315120)
        && afterClear.OBOPMFBGBAE.Count(a => a.AvatarId == 1512) == 1);

    Check("stage without a StageConfig row resolves its battle from the monster event",
        StartRsp(await Capture(() => Start(380060))).Retcode == 0 && new BattleInstance(trialPlayer,
            trialPlayer.LineupManager.GetCurLineup()!, trialPlayer.SceneInstance!.Entities.Values.OfType<EntityMonster>()
                .Where(m => m.IsAlive).ToList()) is { StageId: 380050, IsTrialActivityBattle: true });
    var left = await Capture(() => Send(new HandlerLeaveTrialActivityCsReq(), new LeaveTrialActivityCsReq { StageId = 380060 }));
    Check("leaving echoes the stage and returns without recording a clear",
        LeaveTrialActivityScRsp.Parser.ParseFrom(left.Last(p => p.Id == CmdIds.LeaveTrialActivityScRsp).Body).StageId == 380060
        && trialData.CurTrialStageId == 0 && trialPlayer.Data.EntryId == TrialReturnEntry
        && trialData.Activities.All(r => r.StageId != 380060));

    TakeTrialActivityRewardScRsp Claim(List<(ushort Id, byte[] Body)> packets) =>
        TakeTrialActivityRewardScRsp.Parser.ParseFrom(packets.Last(p => p.Id == CmdIds.TakeTrialActivityRewardScRsp).Body);
    Task<List<(ushort Id, byte[] Body)>> TakeReward(int stageId) => Capture(() =>
        Send(new HandlerTakeTrialActivityRewardCsReq(), new TakeTrialActivityRewardCsReq { StageId = (uint)stageId }));
    var rewardHcoin = GameData.RewardDataData[GameData.AvatarDemoConfigData[315120].RewardID].Hcoin;
    var hcoinBeforeClaim = trialPlayer.Data.Hcoin;
    var firstClaimPackets = await TakeReward(315120);
    var firstClaim = Claim(firstClaimPackets);
    Check("first claim grants the reward with Hcoin as item 1", firstClaim.Retcode == 0 && firstClaim.StageId == 315120
        && rewardHcoin > 0 && firstClaim.Reward.ItemList_.First().ItemId == 1
        && firstClaim.Reward.ItemList_.First().Num == (uint)rewardHcoin && firstClaim.Reward.ItemList_.Count > 1
        && trialPlayer.Data.Hcoin == hcoinBeforeClaim + rewardHcoin
        && trialData.Activities.Single(r => r.StageId == 315120).TakenReward
        && !firstClaimPackets.Any(p => p.Id == CmdIds.ScenePlaneEventScNotify));
    var secondClaim = Claim(await TakeReward(315120));
    Check("second claim is refused without granting", secondClaim.Retcode == (uint)Retcode.RetFail
        && secondClaim.StageId == 315120 && trialPlayer.Data.Hcoin == hcoinBeforeClaim + rewardHcoin);
    var unclearedClaim = Claim(await TakeReward(315130));
    Check("uncleared trial reward is refused", unclearedClaim.Retcode == (uint)Retcode.RetFail && unclearedClaim.StageId == 315130);
    var displayClaim = Claim(await TakeReward(313079));
    Check("non-trial reward claim is invalid", displayClaim.Retcode == (uint)Retcode.RetReqParaInvalid && displayClaim.StageId == 313079);

    await Capture(() => Start(315130));
    trialPlayer.Connection = null;
    await trialOutbound.StopAsync();
    await trialInbound.StopAsync();
}
Check("trial is running on the trial plane before relogin", trialData.CurTrialStageId == 315130
    && GameData.MazePlaneData[trialPlayer.Data.PlaneId].PlaneType == PlaneTypeEnum.TrialActivity);
var relogin = new PlayerInstance(trialPlayer.Data);
await relogin.InitializeAsync();
await relogin.OnLogin();
Check("relogin mid-trial abandons the run and returns to the return point", trialData.CurTrialStageId == 0
    && relogin.ActivityManager!.TrialActivityInstance == null && relogin.Data.EntryId == TrialReturnEntry
    && relogin.SceneInstance?.Excel.PlaneType != PlaneTypeEnum.TrialActivity
    && relogin.LineupManager!.GetExtraLineup(ExtraLineupType.LineupStageTrial) == null
    && !relogin.LineupManager.GetCurLineup()!.IsExtraLineup()
    && GetTrialActivityDataScRsp.Parser.ParseFrom(new PacketGetTrialActivityDataScRsp(relogin).Data).ActivityStageId == 0);

Console.WriteLine($"Login/scene regression: {checks} checks, {failures.Count} failures. Temporary artifacts: {scratch}");
Environment.ExitCode = failures.Count == 0 ? 0 : 1;

sealed class ProbeEntity : BaseGameEntity
{
    public override int EntityId { get; set; }
    public override int GroupId { get; set; }
    public override SceneEntityInfo ToProto() => new() { EntityId = (uint)EntityId, GroupId = (uint)GroupId };
}

sealed class ProbeSender : ICommandSender
{
    public List<string> Messages { get; } = [];
    public ValueTask SendMsg(string msg) { Messages.Add(msg); return ValueTask.CompletedTask; }
    public bool HasPermission(string permission) => true;
    public int GetSender() => 0;
}

sealed class ProbeConnection(KcpConversation? conversation = null) : Connection(conversation!, new IPEndPoint(IPAddress.Loopback, 60001))
{
    public override void Start() { }
}

sealed class LoopbackKcpTransport : IKcpTransport
{
    private readonly Channel<UdpReceiveResult> packets = Channel.CreateUnbounded<UdpReceiveResult>();
    private Task? pump;
    public KcpConversation? Peer { get; set; }
    public void Start() => pump = Task.Run(async () =>
    {
        await foreach (var packet in packets.Reader.ReadAllAsync())
            await Peer!.InputPakcetAsync(packet);
    });
    public async Task StopAsync()
    {
        packets.Writer.TryComplete();
        if (pump != null) await pump.WaitAsync(TimeSpan.FromSeconds(5));
    }
    public ValueTask SendPacketAsync(Memory<byte> packet, IPEndPoint remoteEndpoint, CancellationToken cancellationToken)
    {
        // Like UDP, a send completes when queued, not when the remote peer has processed it.
        // Awaiting Input inside the sender's flush can deadlock two peers exchanging acknowledgments.
        packets.Writer.TryWrite(new UdpReceiveResult(packet.ToArray(), remoteEndpoint));
        return ValueTask.CompletedTask;
    }
}
