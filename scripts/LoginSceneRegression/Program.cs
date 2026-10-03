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
using March7thHoney.Database.Friend;
using March7thHoney.Database.Generated;
using March7thHoney.Database.Player;
using March7thHoney.Database.Quests;
using March7thHoney.Enums;
using March7thHoney.Enums.Avatar;
using March7thHoney.Enums.Scene;
using March7thHoney.Enums.Task;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Command;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.GameServer.Game.Friend;
using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.GameServer.Game.RogueMagic;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Game.Tutorial;
using March7thHoney.GameServer.Server;
using March7thHoney.GameServer.Server.Packet;
using March7thHoney.GameServer.Server.Packet.Recv.Activity;
using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.GameServer.Server.Packet.Send.Avatar;
using March7thHoney.GameServer.Server.Packet.Send.Chat;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.GameServer.Server.Packet.Recv.Mission;
using March7thHoney.GameServer.Server.Packet.Recv.Rogue;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Internationalization;
using March7thHoney.Proto;
using March7thHoney.ProtoOrganizer;
using March7thHoney.Kcp;
using March7thHoney.Kcp.KcpSharp;
using Google.Protobuf;
using MemoryPack;
using Microsoft.Data.Sqlite;
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
var applies45ProdCompatibility = GameConstants.IsVersion("4.5.0");
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
    var legacyLoaded = ResourceCache.LoadCache();
    Check("legacy cache validation follows the active protocol requirements", applies45ProdCompatibility
        ? !legacyLoaded && GameData.FloorInfoData["P10202_F10202001"].Groups[169].IsHoyoGroup == null
        : legacyLoaded);
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

// With the raw RogueBuffGroup table present, the stale check must accept this cache and reject one whose classic
// groups collapsed onto id 0 (pre-4.5-key-mapping) or whose buff rows lost their classic subtype.
var validateCache = typeof(ResourceCache).GetMethod("ValidateLoadedCache", BindingFlags.Static | BindingFlags.NonPublic)!;
var rogueRawPath = Path.Combine(scratch, "rogue-raw-resource-json");
Directory.CreateDirectory(Path.Combine(rogueRawPath, config.Path.ExcelOutputDirs[0]));
File.WriteAllText(Path.Combine(rogueRawPath, config.Path.ExcelOutputDirs[0], "RogueBuffGroup.json"), "[]");
config.Path.ResourcePath = rogueRawPath;
var freshCacheValid = (bool)validateCache.Invoke(null, null)!;
GameData.RogueBuffGroupData[0] = new RogueBuffGroupExcel();
var collapsedCacheValid = (bool)validateCache.Invoke(null, null)!;
GameData.RogueBuffGroupData.Remove(0);
config.Path.ResourcePath = rawResourcePath;
Check("cache keeps classic Simulated Universe buff and group subtypes",
    GameData.RogueBuffData.Values.OfType<RogueBuffExcel>().Any() &&
    GameData.RogueBuffData.Values.OfType<RogueTournBuffExcel>().Any() &&
    GameData.RogueBuffGroupData.GetValueOrDefault(100005) is RogueBuffGroupExcel { IsLoaded: true } normalDraft &&
    normalDraft.BuffList.Count > 0 && normalDraft.BuffList.All(buff => buff is RogueBuffExcel));
Check("stale check accepts the fresh cache and rejects collapsed classic RogueBuffGroup rows",
    freshCacheValid && !collapsedCacheValid);
Check("cache satisfies the active version's Hoyo metadata requirement", applies45ProdCompatibility
    ? GameData.FloorInfoData["P10202_F10202001"].Groups[169].IsHoyoGroup == true
    : GameData.FloorInfoData["P10202_F10202001"].Groups[169].IsHoyoGroup is null or true);
var mapEntranceFiles = typeof(MapEntranceExcel).GetCustomAttribute<ResourceEntity>()!.FileName;
Check("map entrance loader includes the 4.5 LD table", mapEntranceFiles.Contains("MapEntranceLD.json"));

var cacheConfigPath = config.Path.ConfigPath;
config.Path.ConfigPath = Path.Combine(repository, "Config");
GameData.RogueMapGenData = ResourceManager.LoadCustomFile<Dictionary<int, List<int>>>("Rogue Map", "RogueMapGen")!;
ResourceManager.LoadRogueMagicRoomData();
ResourceManager.LoadChessRogueRoomData();
ResourceManager.LoadChessRogueDiceSurfaceEffectData();
ResourceManager.LoadRogueChestMapInfo();
config.Path.ConfigPath = cacheConfigPath;
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
var sceneMapFloorTag = applies45ProdCompatibility ? 14 : 9;
var sceneMapRetcodeTag = applies45ProdCompatibility ? 6 : 11;
Check("active SceneMapInfo writes floor_id to its verified wire field",
    successFields.GetValueOrDefault(sceneMapFloorTag) == 10202001 && !successFields.ContainsKey(sceneMapRetcodeTag));
var errorFields = UIntFields(new SceneMapInfo { FloorId = 10202001, Retcode = 2 }.ToByteArray());
Check("active SceneMapInfo writes an independent retcode field",
    errorFields.GetValueOrDefault(sceneMapFloorTag) == 10202001 &&
    errorFields.GetValueOrDefault(sceneMapRetcodeTag) == 2);

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
Check("active scene boundary uses the generated cmdid and an empty protobuf payload",
    boundaryPacket.CmdId == CmdIds.SyncServerSceneChangeNotify && boundaryPacket.Data.Length == 0 &&
    (applies45ProdCompatibility || CmdIds.SyncServerSceneChangeNotify == 1428));

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

var rogueRoomScene = roguePlayer.SceneInstance!;
var rogueRoomExcel = roguePlayer.RogueManager.GetActiveRoomExcel(rogueRoomScene.EntryId)!;
var rogueExitTargets = roguePlayer.RogueManager.GetRoomExitTargets();
List<EntityProp> RogueExitDoors(SceneInstance scene) => scene.GetEntitiesInGroup<EntityProp>(rogueRoomExcel.GroupID)
    .Where(prop => prop.Excel.PropType == PropTypeEnum.PROP_ROGUE_DOOR).ToList();
var rogueRoomHasMonsters = rogueRoomScene.Entities.Values.OfType<EntityMonster>().Any();
var rogueDoors = RogueExitDoors(rogueRoomScene);
Check("Rogue room exit portals carry a next-room destination",
    rogueExitTargets.Count > 0 && rogueDoors.Count > 0 &&
    rogueDoors.All(door => door.RogueDoorTarget is { } target && rogueExitTargets.Contains(target)));
Check("Rogue room exit portal serializes rogue_game_info {room_id, site}", rogueDoors.All(door =>
    door.ToProto().Prop.ExtraInfo?.RogueGameInfo is { } info && info.RoomId == door.RogueDoorTarget!.Value.RoomId &&
    info.MAJKFIMPOOF == door.RogueDoorTarget.Value.SiteId));
Check("Rogue combat room exit portal loads Closed until the room is cleared", !rogueRoomHasMonsters ||
    rogueDoors.All(door => door.State == PropStateEnum.Closed));
var seededDoor = rogueDoors[0];
roguePlayer.SetScenePropData(rogueRoomScene.FloorId, seededDoor.GroupId, seededDoor.InstId, PropStateEnum.Open);
var rebuiltRogueRoom = new SceneInstance(roguePlayer, rogueRoomScene.Excel, rogueRoomScene.FloorId, rogueRoomScene.EntryId);
var rebuiltDoor = await rebuiltRogueRoom.EntityLoader!.LoadProp(seededDoor.PropInfo, seededDoor.Group);
Check("Rogue room ignores a saved portal state and ships it Closed with its destination",
    rebuiltRogueRoom.EntityLoader is RogueEntityLoader && rebuiltDoor?.State == PropStateEnum.Closed &&
    rebuiltDoor.RogueDoorTarget == seededDoor.RogueDoorTarget);
roguePlayer.SceneData!.ScenePropData.Remove(rogueRoomScene.FloorId);

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

// Global chat: a second synthetic contact whose messages are relayed to every online player.
var serverOption = config.ServerOption;
var globalChatUid = serverOption.GetGlobalChatUid() ?? 0;
Check("global chat is enabled by default on its own uid", globalChatUid == 98 && globalChatUid != serverOption.ServerProfile.Uid);
var friendList = player.FriendManager!.ToProto();
var globalContact = friendList.FriendList.SingleOrDefault(friend => friend.PlayerInfo.Uid == globalChatUid);
Check("friend list shows the global chat contact next to the command bot", globalContact != null
    && friendList.FriendList.Any(friend => friend.PlayerInfo.Uid == serverOption.ServerProfile.Uid)
    && globalContact.IsMarked && globalContact.PlayerInfo.OnlineStatus == FriendOnlineStatus.Online
    && globalContact.PlayerInfo.Nickname == serverOption.GlobalChat.Name
    && globalContact.PlayerInfo.AssistSimpleInfoList.Count == serverOption.GlobalChat.AssistInfo.Count);
Check("player detail lookup resolves the global contact, not the first system entry",
    player.FriendManager.GetFriendPlayerData([globalChatUid]).FirstOrDefault(x => x.Uid == globalChatUid)?.Name
    == serverOption.GlobalChat.Name);
Check("friend requests to system contacts are rejected", await player.FriendManager.AddFriend(globalChatUid)
    == Retcode.RetFriendAlreadyIsFriend && !player.FriendManager.FriendData.SendApplyList.Contains(globalChatUid));
List<FriendChatData> GlobalHistory(PlayerInstance p) =>
    p.FriendManager!.FriendData.ChatHistory.GetValueOrDefault(globalChatUid)?.MessageList ?? [];
var chatClock = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var relayRecipients = new[] { player, roguePlayer, chessPlayer };
var relayResult = await GlobalChatManager.RelayAsync(magicPlayer, "  hello everyone  ", relayRecipients, chatClock);
var expectedRelay = $"[{magicPlayer.Data.Name} / {magicPlayer.Uid}]: hello everyone";
Check("global chat relays to every recipient and the sender from the contact", relayResult == GlobalChatRelayResultEnum.Relayed
    && relayRecipients.Append(magicPlayer).All(p => GlobalHistory(p).LastOrDefault() is { } last
        && last.Message == expectedRelay && last.SendUid == globalChatUid && last.ReceiveUid == p.Uid));
Check("global chat never runs through the command bot conversation",
    !magicPlayer.FriendManager!.FriendData.ChatHistory.ContainsKey(serverOption.ServerProfile.Uid));
var historyBefore = GlobalHistory(player).Count;
Check("empty global chat text is ignored", await GlobalChatManager.RelayAsync(magicPlayer, "   ", relayRecipients, chatClock)
    == GlobalChatRelayResultEnum.Ignored && GlobalHistory(player).Count == historyBefore);
Check("too-long global chat text is rejected without relaying", await GlobalChatManager.RelayAsync(magicPlayer,
    new string('x', serverOption.GlobalChat.MaxLength + 1), relayRecipients, chatClock) == GlobalChatRelayResultEnum.TooLong
    && GlobalHistory(player).Count == historyBefore);
Check("global chat rate limit allows the configured burst", await GlobalChatManager.RelayAsync(magicPlayer, "two",
    relayRecipients, chatClock.AddSeconds(1)) == GlobalChatRelayResultEnum.Relayed
    && await GlobalChatManager.RelayAsync(magicPlayer, "three", relayRecipients, chatClock.AddSeconds(2))
    == GlobalChatRelayResultEnum.Relayed);
Check("global chat rate limit rejects the next message inside the window", await GlobalChatManager.RelayAsync(magicPlayer,
    "four", relayRecipients, chatClock.AddSeconds(59)) == GlobalChatRelayResultEnum.RateLimited
    && GlobalHistory(player).Last().Message.EndsWith(": three"));
Check("global chat rate limit is per sender", await GlobalChatManager.RelayAsync(chessPlayer, "other sender",
    relayRecipients, chatClock.AddSeconds(59)) == GlobalChatRelayResultEnum.Relayed);
Check("global chat rate limit window slides", await GlobalChatManager.RelayAsync(magicPlayer, "five", relayRecipients,
    chatClock.AddSeconds(60)) == GlobalChatRelayResultEnum.Relayed);
var rateLimitCount = serverOption.GlobalChat.RateLimitCount;
serverOption.GlobalChat.RateLimitCount = 0;
for (var i = 0; i < serverOption.GlobalChat.MaxHistory + 5; i++)
    await GlobalChatManager.RelayAsync(roguePlayer, $"flood {i}", relayRecipients, chatClock.AddMinutes(5));
serverOption.GlobalChat.RateLimitCount = rateLimitCount;
Check("global chat history is trimmed to MaxHistory per player", relayRecipients.All(p =>
    GlobalHistory(p).Count == serverOption.GlobalChat.MaxHistory
    && GlobalHistory(p).Last().Message.EndsWith($": flood {serverOption.GlobalChat.MaxHistory + 4}")));
var globalHistoryPacket = GetPrivateChatHistoryScRsp.Parser.ParseFrom(
    new PacketGetPrivateChatHistoryScRsp((uint)player.Uid, (uint)globalChatUid, player).Data);
Check("global chat history ends with the configured welcome as its oldest entry",
    globalHistoryPacket.ChatMessageList.Count == serverOption.GlobalChat.MaxHistory + 1
    && globalHistoryPacket.ChatMessageList.Last().MessageDatas.Single().ChatData.MessageText
    == serverOption.GlobalChat.WelcomeMessage);
var botHistoryPacket = GetPrivateChatHistoryScRsp.Parser.ParseFrom(new PacketGetPrivateChatHistoryScRsp(
    (uint)player.Uid, (uint)serverOption.ServerProfile.Uid, player).Data);
Check("command bot history gets no global chat welcome", botHistoryPacket.ChatMessageList.All(message =>
    message.MessageDatas.All(data => data.ChatData.MessageText != serverOption.GlobalChat.WelcomeMessage)));
serverOption.GlobalChat.Uid = serverOption.ServerProfile.Uid;
Check("global chat uid colliding with the command bot disables the contact", serverOption.GetGlobalChatUid() == null
    && player.FriendManager.GetFriendPlayerData().Count(x => x.Uid == serverOption.ServerProfile.Uid) == 1);
serverOption.GlobalChat.Uid = globalChatUid;
serverOption.GlobalChat.Enabled = false;
Check("disabled global chat removes the contact and relays nothing", serverOption.GetGlobalChatUid() == null
    && player.FriendManager.GetFriendPlayerData().All(x => x.Uid != globalChatUid)
    && await GlobalChatManager.RelayAsync(magicPlayer, "off", relayRecipients, chatClock.AddHours(1))
    == GlobalChatRelayResultEnum.Disabled);
serverOption.GlobalChat.Enabled = true;

// /giveall avatar: only typed values touch owned characters; new characters start at l1 / r0.
var givePlayer = new PlayerInstance(60005);
await givePlayer.InitializeAsync();
var giveTarget = new ProbeConnection { Player = givePlayer };
var giveSender = new ProbeSender();
var ownedHero = givePlayer.AvatarManager!.GetHero()!;
var ownedMarch = givePlayer.AvatarManager.GetFormalAvatar(1001)!;
ownedHero.Level = 70;
ownedHero.GetCurPathInfo().Rank = 2;
ownedMarch.Level = 50;
ownedMarch.GetCurPathInfo().Rank = 4;
var heroPathBefore = ownedHero.AvatarId;
var marchPathBefore = ownedMarch.AvatarId;
var maleHeroPathsBefore = ownedHero.PathInfos.Keys.Where(pathId => pathId % 2 == 1).ToHashSet();
var giveall = new CommandGiveall();
await giveall.GiveAllAvatar(new CommandArg("avatar", giveSender, giveTarget));
var newAvatar = givePlayer.AvatarManager.Data.FormalAvatars.FirstOrDefault(avatar => avatar.BaseAvatarId == 1002);
Check("bare /giveall avatar keeps owned levels and Eidolons", ownedHero.Level == 70 && ownedHero.GetCurPathInfo().Rank == 2
    && ownedMarch.Level == 50 && ownedMarch.GetCurPathInfo().Rank == 4);
Check("bare /giveall avatar keeps the current path of owned characters",
    ownedHero.AvatarId == heroPathBefore && ownedMarch.AvatarId == marchPathBefore);
Check("bare /giveall avatar creates missing characters at l1 / r0",
    newAvatar is { Level: 1 } && newAvatar.PathInfos.Values.All(path => path.Rank == 0));
Check("giveall unlocks every Path of multi-path characters", ownedMarch.PathInfos.ContainsKey(1224)
    && GameData.MultiplePathAvatarConfigData.Values.Where(x => x.BaseAvatarID == 8001 && x.AvatarID % 2 == 0)
        .All(x => ownedHero.PathInfos.ContainsKey(x.AvatarID)));
Check("giveall unlocks only the Trailblazer paths of the player's gender",
    ownedHero.PathInfos.Keys.Where(pathId => pathId % 2 == 1).ToHashSet().SetEquals(maleHeroPathsBefore));
await giveall.GiveAllAvatar(new CommandArg("avatar r6", giveSender, giveTarget));
Check("explicit giveall Eidolon applies to every owned path without touching levels",
    ownedMarch.PathInfos.Values.All(path => path.Rank == 6) && ownedHero.PathInfos.Values.All(path => path.Rank == 6)
    && ownedMarch.Level == 50 && ownedHero.Level == 70 && newAvatar?.Level == 1);
await giveall.GiveAllAvatar(new CommandArg("avatar l80", giveSender, giveTarget));
Check("explicit giveall level applies without touching Eidolons", ownedMarch.Level == 80 && ownedHero.Level == 80
    && newAvatar?.Level == 80 && ownedMarch.PathInfos.Values.All(path => path.Rank == 6)
    && ownedMarch.Promotion == GameData.GetMinPromotionForLevel(80));

// /language: per-player pin persisted on PlayerData, used by command replies and chat notices.
var languageCommand = new CommandLanguage();
var languageTarget = new ProbeConnection { Player = givePlayer };
var languageSender = new ProbeSender(givePlayer.Uid);
async Task<string> LanguageReply(string raw, ICommandSender who)
{
    var probeWho = who as ProbeSender;
    probeWho?.Messages.Clear();
    await languageCommand.Language(new CommandArg(raw, who, languageTarget));
    return probeWho?.Messages.LastOrDefault() ?? "";
}
givePlayer.Language = "CHS";
Check("console cannot pin a language", await LanguageReply("en", new ProbeSender())
    == I18NManager.Translate("Game.Command.Language.PlayerOnly") && givePlayer.Data.LanguagePin == null);
Check("language list shows only installed language files", (await LanguageReply("list", languageSender))
    == I18NManager.Translate("Game.Command.Language.Available", "CHS, CHT, EN"));
Check("language alias pins EN and answers in English", await LanguageReply("English", languageSender)
    == I18NManager.TranslateAsCertainLang("EN", "Game.Command.Language.Set", "EN")
    && givePlayer.Data.LanguagePin == "EN" && givePlayer.EffectiveLanguage == "EN"
    && new PlayerCommandSender(givePlayer).Language == "EN");
Check("language aliases are case-insensitive", await LanguageReply("ZH-Hant", languageSender)
    == I18NManager.TranslateAsCertainLang("CHT", "Game.Command.Language.Set", "CHT") && givePlayer.Data.LanguagePin == "CHT");
Check("language code without a file is rejected", (await LanguageReply("id", languageSender))
    == I18NManager.Translate("Game.Command.Language.NotFound", "id", "CHS, CHT, EN") && givePlayer.Data.LanguagePin == "CHT");
Check("language auto clears the pin and answers in the client language", await LanguageReply("auto", languageSender)
    == I18NManager.TranslateAsCertainLang("CHS", "Game.Command.Language.Cleared", "CHS")
    && givePlayer.Data.LanguagePin == null && givePlayer.EffectiveLanguage == "CHS");
givePlayer.Data.LanguagePin = "EN";
DatabaseHelper.MarkDirty(givePlayer.Uid);
DatabaseHelper.Instance!.SaveDatabase();
await using (var db = new SqliteConnection($"Data Source={Path.Combine(scratch, "regression.db")}"))
{
    await db.OpenAsync();
    await using var query = db.CreateCommand();
    query.CommandText = "select LanguagePin from Player where Uid = $uid";
    query.Parameters.AddWithValue("$uid", givePlayer.Uid);
    Check("language pin is persisted in the Player table", (await query.ExecuteScalarAsync()) as string == "EN");
}
await using (var oldDb = new SqliteConnection($"Data Source={Path.Combine(scratch, "old-player-table.db")}"))
{
    await oldDb.OpenAsync();
    await using (var create = oldDb.CreateCommand())
    {
        create.CommandText = "create table \"Player\" (\"Uid\" INTEGER PRIMARY KEY, \"Name\" TEXT); " +
                             "insert into \"Player\" (\"Uid\", \"Name\") values (70001, 'legacy')";
        await create.ExecuteNonQueryAsync();
    }
    var playerStore = EntityStores.Map[typeof(PlayerData)];
    playerStore.EnsureTable(oldDb);
    var legacyPlayer = playerStore.LoadAll(oldDb).OfType<PlayerData>().Single();
    Check("old Player tables gain a nullable LanguagePin column that loads as unpinned",
        legacyPlayer.Uid == 70001 && legacyPlayer.Name == "legacy" && legacyPlayer.LanguagePin == null);
}
givePlayer.Data.LanguagePin = null;

// Trailblazer paths: both genders stay stored, only the current gender is announced, and path changes are
// normalized to the player's gender and echo the stored id.
var heroPathConfigs = GameData.MultiplePathAvatarConfigData.Values.Where(x => x.BaseAvatarID == 8001).ToList();
Check("Trailblazer path configs pair odd=male / even=female", heroPathConfigs.Count > 0 && heroPathConfigs.All(x =>
    x.Gender == (x.AvatarID % 2 == 1 ? GenderTypeEnum.GENDER_MAN : GenderTypeEnum.GENDER_WOMAN)));
var hero = givePlayer.AvatarManager.GetHero()!;
List<uint> AnnouncedHeroPaths() => hero.ToAvatarPathDataProto().Select(path => path.AvatarId).ToList();
bool OnlyGender(IEnumerable<uint> ids, int parity) => ids.Where(id => id is > 8000 and < 9000).All(id => id % 2 == parity);
Check("female hero stores both genders but announces only female paths", givePlayer.Data.CurrentGender == Gender.Woman
    && hero.PathInfos.ContainsKey(8001) && hero.PathInfos.ContainsKey(8002) && OnlyGender(AnnouncedHeroPaths(), 0)
    && AnnouncedHeroPaths().Count == heroPathConfigs.Count(x => x.AvatarID % 2 == 0));
var avatarDataRsp = GetAvatarDataScRsp.Parser.ParseFrom(new PacketGetAvatarDataScRsp(givePlayer).Data);
Check("avatar data basic types and path list agree on the hero's gender",
    OnlyGender(avatarDataRsp.BasicTypeIdList, 0) && OnlyGender(avatarDataRsp.AvatarPathDataInfoList.Select(x => x.AvatarId), 0)
    && avatarDataRsp.BasicTypeIdList.Contains(8004u) && !avatarDataRsp.BasicTypeIdList.Contains(8003u));
var heroSync = PlayerSyncScNotify.Parser.ParseFrom(new PacketPlayerSyncScNotify(hero).Data);
Check("avatar sync announces only the hero's gender paths",
    OnlyGender(heroSync.AvatarSync.AvatarPathDataInfoList.Select(x => x.AvatarId), 0));
Check("female choosing a male path id stores the female variant", await givePlayer.ChangeAvatarPathType(8001,
    (MultiPathAvatarTypeEnum)8003) == 8004 && hero.AvatarId == 8004 && givePlayer.Data.CurBasicType == 8004);
Check("female choosing a female path id keeps it", await givePlayer.ChangeAvatarPathType(8001,
    (MultiPathAvatarTypeEnum)8006) == 8006 && hero.AvatarId == 8006);
Check("path change accepts any variant id as the base", await givePlayer.ChangeAvatarPathType(8004,
    (MultiPathAvatarTypeEnum)8002) == 8002 && hero.AvatarId == 8002);
Check("another character's path cannot be put on the hero", await givePlayer.ChangeAvatarPathType(8001,
    MultiPathAvatarTypeEnum.Mar_7thRogue) == 0 && hero.AvatarId == 8002);
Check("non-hero path change stores the requested path", await givePlayer.ChangeAvatarPathType(1001,
    MultiPathAvatarTypeEnum.Mar_7thRogue) == 1224 && ownedMarch.AvatarId == 1224);
giveSender.Messages.Clear();
await new CommandHero().ChangeType(new CommandArg("type 1001", giveSender, giveTarget));
Check("/hero type rejects a non-Trailblazer path", hero.AvatarId == 8002
    && giveSender.Messages.Single() == I18NManager.Translate("Game.Command.Hero.HeroTypeNotSpecified"));
await givePlayer.SetHeroGender(Gender.Man);
Check("gender switch keeps stored paths and announces only male paths", hero.AvatarId == 8001
    && hero.PathInfos.ContainsKey(8004) && OnlyGender(AnnouncedHeroPaths(), 1));
Check("male choosing a female path id stores the male variant", await givePlayer.ChangeAvatarPathType(8001,
    (MultiPathAvatarTypeEnum)8004) == 8003 && hero.AvatarId == 8003 && OnlyGender(AnnouncedHeroPaths(), 1));

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
if (applies45ProdCompatibility)
{
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
}
else
{
    Check("4.6 keeps content outside the 4.5 Changle compatibility exclusion",
        excludedChangleProps.All(item => changle.Entities.Values.OfType<EntityProp>()
            .Any(prop => prop.GroupId == item.Item1 && prop.InstId == item.Item2)) &&
        changle.Groups.Contains(169) && changle.Entities.Values.Any(entity => entity.GroupId == 169));
    await changle.EntityLoader!.SyncEntity();
    await changle.EntityLoader.SyncEntity();
    Check("4.6 sync does not apply the 4.5 Changle compatibility exclusion",
        excludedChangleProps.All(item => changle.Entities.Values.OfType<EntityProp>()
            .Any(prop => prop.GroupId == item.Item1 && prop.InstId == item.Item2)) &&
        changle.Groups.Contains(169) && changle.Entities.Values.Any(entity => entity.GroupId == 169));
}
var elevatorScene = new SceneInstance(player, GameData.MazePlaneData[20002], 20002001, 2000201);
elevatorScene.EntityLoader!.LoadGroups.Add(8);
var elevatorGroup = elevatorScene.FloorInfo!.Groups[8];
Check("cache keeps ordinary server Hoyo metadata compatible with the active version",
    (applies45ProdCompatibility ? elevatorGroup.IsHoyoGroup == true : elevatorGroup.IsHoyoGroup is null or true)
    && elevatorGroup.Category != GroupCategoryEnum.Atmosphere);
Check("ordinary server Hoyo elevator is not excluded", (await elevatorScene.EntityLoader.LoadGroup(elevatorGroup, true))
    ?.OfType<EntityProp>().Any(prop => prop.Excel.PropType == PropTypeEnum.PROP_ELEVATOR) == true);
var loginScenePacket = new PacketGetCurSceneInfoScRsp(player);
var loginScene = GetCurSceneInfoScRsp.Parser.ParseFrom(loginScenePacket.Data).Scene;
Check("login restoration carries the 4.6 jump-energy data",
    loginScene.FiveDimGameData?.MaxJumpEnergy == 5);
bool ExcludesIncompatibleChangleContent(SceneInfo info) => info.EntityGroupList.All(group =>
    group.GroupId != 169 && (group.GroupId != 208 || group.EntityList.Count == 0)
    && (group.GroupId != 144 || group.EntityList.All(entity => entity.InstId != 300003)));
var teleportScene = EnterSceneByServerScNotify.Parser.ParseFrom(new PacketEnterSceneByServerScNotify(changle).Data).Scene;
Check("login restoration applies only the active version's Changle compatibility policy",
    ExcludesIncompatibleChangleContent(loginScene) == applies45ProdCompatibility);
Check("teleport snapshot applies only the active version's Changle compatibility policy",
    ExcludesIncompatibleChangleContent(teleportScene) == applies45ProdCompatibility);
var mapPacket = new PacketGetSceneMapInfoScRsp(new GetSceneMapInfoCsReq
    { SceneIdentifiers = { new SceneIdentifier { FloorId = 10202001 } } }, player);
var mapInfo = GetSceneMapInfoScRsp.Parser.ParseFrom(mapPacket.Data).SceneMapInfo.Single();
Check("real map response carries Changle Tian floor at the active wire tag",
    UIntFields(mapInfo.ToByteArray()).GetValueOrDefault(sceneMapFloorTag) == 10202001 && mapInfo.Retcode == 0);
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
        Check("all activities stay enabled while snapshots apply the active compatibility policy",
            ExcludesIncompatibleChangleContent(entryPacket.Scene) == applies45ProdCompatibility
            && ExcludesIncompatibleChangleContent(restoredScene) == applies45ProdCompatibility
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
if (applies45ProdCompatibility)
{
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
}
else
{
    Check("4.6 keeps Starskiff content outside the 4.5 compatibility exclusion",
        starskiffEntities?.OfType<EntityProp>().Any(prop => prop.InstId == 300001) == true);
    await starskiff.EntityLoader.SyncEntity();
    Check("4.6 sync retains Starskiff content outside the 4.5 compatibility exclusion",
        starskiff.Entities.Values.OfType<EntityProp>().Any(prop => prop.GroupId == 280 && prop.InstId == 300001));
    await starskiff.EntityLoader.UnloadGroup(280);
    Check("4.6 forced activation retains Starskiff content", (await starskiff.EntityLoader.LoadGroup(
        starskiffGroup, forceLoad: true))?.OfType<EntityProp>().Any(prop => prop.InstId == 300001) == true);
}

// Main-mission state on a content-package Maze floor: missions off keeps the lite-mode 1, missions on keeps the
// resource/official 0 (official 20001001 feed ships SceneStateByMainMission 0).
var missionFloorEntry = GameData.MapEntranceData.Values.Where(entry => entry.FloorID == 20001001).Min(entry => entry.ID);
var missionFloorEntered = await player.TryEnterScene(missionFloorEntry, 0, false);
var missionFloor = player.SceneInstance!;
Check("floor 20001001 declares a zero-default SceneStateByMainMission in the production cache", missionFloorEntered.Success
    && missionFloor.FloorInfo!.FloorSavedValue.Any(value => value.Name == "SceneStateByMainMission" && value.DefaultValue == 0));
Check("missions disabled: content-package floor still exposes SceneStateByMainMission as 1",
    missionFloor.BuildClientFloorSavedData().GetValueOrDefault("SceneStateByMainMission") == 1);
player.MissionManager!.Data.EnableMissionOverride = true;
var missionSnapshot = missionFloor.BuildClientFloorSavedData();
Check("missions enabled: SceneStateByMainMission is not forced to 1",
    missionSnapshot.GetValueOrDefault("SceneStateByMainMission") == 0
    && missionFloor.FloorInfo!.Groups.Values.Any(group => group.SavedValueCondition.Conditions
        .Any(condition => condition.SavedValueName == "SceneStateByMainMission") && !group.SavedValueCondition.IsTrue(missionSnapshot)));
player.SceneData!.FloorSavedData[20001001] = new() { ["SceneStateByMainMission"] = 1 };
Check("missions enabled: a story-written SceneStateByMainMission still wins",
    missionFloor.BuildClientFloorSavedData().GetValueOrDefault("SceneStateByMainMission") == 1);
player.SceneData.FloorSavedData.Remove(20001001);
player.MissionManager.Data.EnableMissionOverride = null;

// Summon-delivered maze buffs (Cyrene 14151 -> 141502) reference the summon entity, keep the authored lifetime,
// and leave with the summon; the summon's lifetime is owned by the caster's timed maze buff 141501.
var fieldScene = missionFloor;
var fieldMonsters = fieldScene.Entities.Values.OfType<EntityMonster>().Where(monster => monster.IsAlive).Take(2).ToList();
var fieldCaster = fieldScene.AvatarInfo.Values.First();
Console.WriteLine($"Summon fixture: floor {fieldScene.FloorId}, {fieldMonsters.Count} monsters, caster entity {fieldCaster.EntityId}.");
Check("summon fixture has two live monsters", fieldMonsters.Count == 2);
Check("cache keeps SummonUnitData destroy flags", GameData.SummonUnitDataData[14151].RemoveMazeBuffOnDestroy
    && !GameData.SummonUnitDataData[14101].RemoveMazeBuffOnDestroy);
await fieldCaster.AddBuff(new SceneBuff(141501, 1, 1415, 30));
var cyreneField = fieldScene.SummonUnit.Values.SingleOrDefault(unit => unit.SummonUnitId == 14151);
Check("timed technique buff creates the field summon with official infinite lifetime",
    cyreneField is { LifeTimeMs: -1 } && cyreneField.AttachEntityId == fieldCaster.EntityId);
if (cyreneField != null && fieldMonsters.Count == 2)
{
    var fieldTargets = fieldMonsters.Select(monster => (uint)monster.EntityId).ToList();
    Check("field trigger accepts the monsters", await fieldScene.TriggerSummonUnit(cyreneField.EntityId,
        "HitNpcMonsterTrigger", fieldTargets) == Retcode.RetSucc);
    var delivered = fieldMonsters.Select(monster => monster.BuffList.SingleOrDefault(buff => buff.BuffId == 141502)).ToList();
    Check("summon-delivered buff references the summon entity with authored lifetime 500", delivered.All(buff =>
        buff is { SummonUnitEntityId: var summonId, Duration: 500 } && summonId == cyreneField.EntityId
        && buff.ToProto().BuffSummonEntityId == (uint)cyreneField.EntityId && buff.ToProto().LifeTime == 500));
    Check("each monster receives its own buff instance", !ReferenceEquals(delivered[0], delivered[1]));
    await fieldScene.TriggerSummonUnit(cyreneField.EntityId, "HitNpcMonsterTrigger", [fieldTargets[0]]);
    var refreshed = fieldMonsters[1].BuffList.SingleOrDefault(buff => buff.BuffId == 141502);
    Check("trigger exit refreshes the lifetime and keeps the summon reference",
        refreshed is { Duration: 1 } && refreshed.SummonUnitEntityId == cyreneField.EntityId
        && fieldMonsters[0].BuffList.Single(buff => buff.BuffId == 141502).Duration == 500);
    await fieldCaster.RemoveBuff(141501);
    Check("technique buff removal destroys the summon and the buffs it delivered",
        !fieldScene.SummonUnit.Values.Any(unit => unit.SummonUnitId == 14151)
        && fieldMonsters.All(monster => monster.BuffList.All(buff => buff.BuffId != 141502)));
}
await fieldCaster.AddBuff(new SceneBuff(141501, 1, 1415));
Check("permanent owning buff keeps the bounded 45s summon lifetime",
    fieldScene.SummonUnit.Values.SingleOrDefault(unit => unit.SummonUnitId == 14151)?.LifeTimeMs == 45000);
await fieldCaster.RemoveBuff(141501);
var keepBuffSummon = new EntitySummonUnit
{
    SummonUnitId = 14101, CreateAvatarEntityId = fieldCaster.EntityId, LifeTimeMs = -1,
    TriggerList = GameData.SummonUnitDataData[14101].ConfigInfo?.TriggerConfig.CustomTriggers ?? []
};
await fieldScene.AddSummonUnitEntity(keepBuffSummon);
if (fieldMonsters.Count == 2)
{
    await fieldScene.TriggerSummonUnit(keepBuffSummon.EntityId, keepBuffSummon.TriggerList[0].TriggerName,
        [(uint)fieldMonsters[0].EntityId]);
    var keptBuffIds = fieldMonsters[0].BuffList.Where(buff => buff.SummonUnitEntityId == keepBuffSummon.EntityId)
        .Select(buff => buff.BuffId).ToList();
    await fieldScene.RemoveSummonUnitById(14101);
    Check("summon without RemoveMazeBuffOnDestroy leaves its delivered buffs", keptBuffIds.Count > 0
        && keptBuffIds.All(id => fieldMonsters[0].BuffList.Any(buff => buff.BuffId == id)));
}

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

// /spawn: runtime monster/prop injection into the real Changle Tian scene and the custom battle it defines.
var spawnCommand = new CommandSpawn();
var spawnScene = player.SceneInstance!;
var spawnOrigin = player.Data.Pos!;
var battleMonsterIds = GameData.MonsterConfigData.Keys.Order().Take(8).ToList();
Check("spawn parser reads two-letter options, clamps them and splits waves at separators and every 5 monsters",
    CommandSpawn.TryParse(["monster", "lv150", "hp900", "x0", "r2", "s7", "@60001",
            $"{battleMonsterIds[0]}", $"{battleMonsterIds[1]}", "/", .. battleMonsterIds.Skip(2).Select(id => $"{id}"), "bogus", "0"],
        out var spawnRequest)
    && spawnRequest.SpawnId == "monster" && spawnRequest.Level == CommandSpawn.MaxLevel && spawnRequest.Hp == 900
    && spawnRequest.Amount == 1 && spawnRequest.Radius == CommandSpawn.MinRadius && spawnRequest.StageId == 7
    && spawnRequest.Waves.Count == 3 && spawnRequest.Waves[0].SequenceEqual(battleMonsterIds.Take(2))
    && spawnRequest.Waves[1].SequenceEqual(battleMonsterIds.Skip(2).Take(5)) && spawnRequest.Waves[2].SequenceEqual(battleMonsterIds.Skip(7))
    && spawnRequest.InvalidBattleMonsters.SequenceEqual(["bogus", "0"]));
Check("spawn parser rejects an overflowing option value", !CommandSpawn.TryParse(["monster", "x99999999999"], out _));
Check("spawn 'monster' resolves the smallest NPC monster", CommandSpawn.Resolve("monster") is
    { Kind: CommandSpawn.SpawnTargetKind.Monster } baseTarget && baseTarget.Monster!.ID == GameData.NpcMonsterDataData.Keys.Min());
var mappedBattleMonster = GameData.MonsterConfigData.Values.FirstOrDefault(monster =>
    !GameData.NpcMonsterDataData.ContainsKey(monster.MonsterID) &&
    GameData.MonsterTemplateConfigData.TryGetValue(monster.MonsterTemplateID, out var template) &&
    template.NPCMonsterList.Any(GameData.NpcMonsterDataData.ContainsKey));
if (mappedBattleMonster != null)
    Check("spawn maps a battle MonsterID to its template's first spawnable NPC monster",
        CommandSpawn.Resolve(mappedBattleMonster.MonsterID.ToString()).Monster?.ID == GameData
            .MonsterTemplateConfigData[mappedBattleMonster.MonsterTemplateID].NPCMonsterList.First(GameData.NpcMonsterDataData.ContainsKey));
var battleOnlyMonster = GameData.MonsterConfigData.Values.FirstOrDefault(monster =>
    !GameData.NpcMonsterDataData.ContainsKey(monster.MonsterID) && !GameData.MazePropData.ContainsKey(monster.MonsterID) &&
    !(GameData.MonsterTemplateConfigData.TryGetValue(monster.MonsterTemplateID, out var template) &&
      template.NPCMonsterList.Any(GameData.NpcMonsterDataData.ContainsKey)));
if (battleOnlyMonster != null)
    Check("spawn reports a battle-only MonsterID instead of a bare invalid ID",
        CommandSpawn.Resolve(battleOnlyMonster.MonsterID.ToString()).Kind == CommandSpawn.SpawnTargetKind.BattleMonsterNoScene);
Check("spawn maze prop and unknown IDs resolve distinctly", CommandSpawn.Resolve("102226").Kind == CommandSpawn.SpawnTargetKind.Prop
    && CommandSpawn.Resolve("abc").Kind == CommandSpawn.SpawnTargetKind.Invalid);

// Changle Tian is a town floor with no monster groups at all: nothing to borrow, nothing spawned.
var townEntityCount = spawnScene.Entities.Count;
sender.Messages.Clear();
await spawnCommand.Spawn(new CommandArg("monster", sender, target));
Check("spawn on a floor without monster configs reports it and spawns nothing",
    spawnScene.Entities.Count == townEntityCount
    && sender.Messages.SequenceEqual([I18NManager.Translate("Game.Command.Spawn.NoMonsterTemplate")]));

// Move to the first ordinary maze floor whose loaded dimension has a usable server monster group.
bool HasSpawnableMonsterGroup(MapEntranceExcel entrance)
{
    if (!GameData.MazePlaneData.TryGetValue(entrance.PlaneID, out var plane) || plane.PlaneType != PlaneTypeEnum.Maze)
        return false;
    GameData.GetFloorInfo(entrance.PlaneID, entrance.FloorID, out var floor);
    var dimension = floor?.DimensionList.Find(item => item.ID == 0);
    return floor != null && dimension != null && floor.Groups.Values.Any(group =>
        group.LoadSide == GroupLoadSideEnum.Server && dimension.GroupIDList.Contains(group.Id)
        && group.MonsterList.Any(monster => monster.FarmElementID == 0 && !monster.IsDelete
            && March7thHoney.GameServer.Game.Battle.BattleInstance.TryResolveStageFromEvent(
                monster.EventID, player.Data.WorldLevel, out _)));
}
var mazeEntry = GameData.MapEntranceData.Values.OrderBy(entrance => entrance.ID).First(HasSpawnableMonsterGroup);
Check($"spawn fixture enters maze entry {mazeEntry.ID} (floor {mazeEntry.FloorID})",
    (await player.TryEnterScene(mazeEntry.ID, 0, false)).Success && player.SceneInstance?.FloorId == mazeEntry.FloorID);
spawnScene = player.SceneInstance!;
spawnOrigin = player.Data.Pos!;

List<T> NewEntities<T>(HashSet<int> before) where T : BaseGameEntity =>
    spawnScene.Entities.Values.OfType<T>().Where(entity => !before.Contains(entity.EntityId)).ToList();
bool UsesMonsterTemplate(EntityMonster monster) => spawnScene.Groups.Contains(monster.GroupId)
    && spawnScene.FloorInfo!.Groups[monster.GroupId].MonsterList.Any(template => template.ID == monster.InstId
        && template.FarmElementID == 0 && template.EventID == monster.EventId);
bool NearOrigin(Position pos) => Math.Abs(pos.X - spawnOrigin.X) <= 5000 && pos.Y == spawnOrigin.Y
    && Math.Abs(pos.Z - spawnOrigin.Z) <= 5000;

var spawnStage = GameData.StageConfigData.Values.First(stage => stage.MonsterList.Count > 0);
var baseNpcMonsterId = GameData.NpcMonsterDataData.Keys.Min();
var entitiesBefore = spawnScene.Entities.Keys.ToHashSet();
sender.Messages.Clear();
await spawnCommand.Spawn(new CommandArg(
    $"monster s{spawnStage.StageID} lv50 hp12345 x2 r1 {battleMonsterIds[0]} {battleMonsterIds[1]} / {battleMonsterIds[2]} bogus",
    sender, target));
var spawnedMonsters = NewEntities<EntityMonster>(entitiesBefore);
Check("spawn adds the requested monsters to a loaded group with the borrowed template identity",
    spawnedMonsters.Count == 2 && spawnedMonsters.All(monster => monster.IsGmSpawned && monster.MonsterData.ID == baseNpcMonsterId
        && UsesMonsterTemplate(monster) && NearOrigin(monster.Position)
        && monster.Rotation is { X: 0, Y: 0, Z: 0 }));
Check("spawn stores stage, level, HP and waves on each spawned monster", spawnedMonsters.All(monster =>
    monster.CustomStageId == spawnStage.StageID && monster.CustomLevel == 50 && monster.CustomBattleHp == 12345
    && monster.CustomBattleWaves is { Count: 2 } waves && waves[0].SequenceEqual(battleMonsterIds.Take(2))
    && waves[1].SequenceEqual([battleMonsterIds[2]])));
Check("spawn reports skipped wave IDs and the spawn result", sender.Messages.Contains(
        I18NManager.Translate("Game.Command.Spawn.InvalidBattleMonster", "bogus"))
    && sender.Messages.Contains(I18NManager.Translate("Game.Command.Spawn.SpawnedMonsters", "2", baseNpcMonsterId.ToString())));
Console.WriteLine($"/spawn monster template: group {spawnedMonsters.FirstOrDefault()?.GroupId}, inst {spawnedMonsters.FirstOrDefault()?.InstId}.");

var castAvatar = spawnScene.AvatarInfo.Values.First();
var gmBattle = await player.BattleManager!.StartBattle(castAvatar, [spawnedMonsters[0]], false);
var gmBattleProto = gmBattle!.ToProto();
Check("custom waves replace the stage waves with sequential wave IDs on the pinned stage",
    gmBattle.StageId == spawnStage.StageID && gmBattle.IsGmSpawnedEncounter && gmBattleProto.MonsterWaveList.Count == 2
    && gmBattleProto.MonsterWaveList.Select(wave => wave.BattleWaveId).SequenceEqual([1u, 2u])
    && gmBattleProto.MonsterWaveList.All(wave => wave.BattleStageId == spawnStage.StageID)
    && gmBattleProto.MonsterWaveList[0].MonsterList.Select(monster => (int)monster.MonsterId).SequenceEqual(battleMonsterIds.Take(2))
    && gmBattleProto.MonsterWaveList[1].MonsterList.Select(monster => (int)monster.MonsterId).SequenceEqual([battleMonsterIds[2]]));
Check("custom battle carries the HP override and level on every wave",
    gmBattleProto.MonsterWaveList.All(wave => wave.MonsterParam.Level == 50
        && wave.MonsterList.All(monster => monster.MaxHp == 12345 && monster.CurHp == 12345)));
player.BattleInstance = null;

entitiesBefore = spawnScene.Entities.Keys.ToHashSet();
await spawnCommand.Spawn(new CommandArg("monster hp777 lv0", sender, target));
var hpOnlyMonster = NewEntities<EntityMonster>(entitiesBefore).Single();
var hpOnlyBattle = await player.BattleManager.StartBattle(castAvatar, [hpOnlyMonster], false);
var hpOnlyProto = hpOnlyBattle!.ToProto();
var expectedStageWaves = hpOnlyBattle.Stages.SelectMany(stage => stage.ToProto()).ToList();
Check("HP override without custom waves keeps the stage waves and level",
    hpOnlyBattle.CustomWaves == null && hpOnlyBattle.CustomLevel == 0 && hpOnlyProto.MonsterWaveList.Count == expectedStageWaves.Count
    && hpOnlyProto.MonsterWaveList.Zip(expectedStageWaves).All(pair => pair.First.MonsterList.Select(monster => monster.MonsterId)
        .SequenceEqual(pair.Second.MonsterList.Select(monster => monster.MonsterId)) && pair.First.MonsterParam.Level == 0)
    && hpOnlyProto.MonsterWaveList.SelectMany(wave => wave.MonsterList).All(monster => monster.MaxHp == 777 && monster.CurHp == 777));
player.BattleInstance = null;
var sceneMonster = spawnScene.Entities.Values.OfType<EntityMonster>().FirstOrDefault(monster => !monster.IsGmSpawned && monster.IsAlive);
if (sceneMonster != null)
{
    var mixedBattle = await player.BattleManager.StartBattle(castAvatar, [sceneMonster, hpOnlyMonster], false);
    Check("a fight that includes a real scene monster still counts for missions", mixedBattle is { IsGmSpawnedEncounter: false });
    player.BattleInstance = null;
}

entitiesBefore = spawnScene.Entities.Keys.ToHashSet();
await spawnCommand.Spawn(new CommandArg("102226 x3", sender, target));
var spawnedProps = NewEntities<EntityProp>(entitiesBefore);
Check("spawn adds open props borrowing a plain prop's group and instance", spawnedProps.Count == 3 && spawnedProps.All(prop =>
    prop.IsGmSpawned && prop.State == PropStateEnum.Open && prop.Excel.ID == 102226 && NearOrigin(prop.Position)
    && spawnScene.Groups.Contains(prop.GroupId) && spawnScene.FloorInfo!.Groups[prop.GroupId].PropList.Any(template =>
        template.ID == prop.InstId && template.FarmElementID == 0 && template.AnchorID == 0)
    && prop.PropInfo is { EventID: 0, ChestID: 0, CocoonID: 0, InteractLinks.Count: 0 }));
var spawnedProp = spawnedProps[0];
var savedTemplateState = player.GetScenePropData(spawnScene.FloorId, spawnedProp.GroupId, spawnedProp.InstId)?.State;
await spawnedProp.SetState(PropStateEnum.Closed);
Check("spawned prop state change is not persisted onto the template prop", spawnedProp.State == PropStateEnum.Closed
    && player.GetScenePropData(spawnScene.FloorId, spawnedProp.GroupId, spawnedProp.InstId)?.State == savedTemplateState);

var entityCount = spawnScene.Entities.Count;
sender.Messages.Clear();
await spawnCommand.Spawn(new CommandArg("abc", sender, target));
await spawnCommand.Spawn(new CommandArg($"monster s{GameData.StageConfigData.Keys.Max() + 1}", sender, target));
await spawnCommand.Spawn(new CommandArg($"monster x{SceneInstance.GmSpawnMaxEntities}", sender, target));
await spawnCommand.Spawn(new CommandArg("", sender, target));
await spawnCommand.Spawn(new CommandArg("monster", sender, new ProbeConnection { Player = roguePlayer }));
Check("spawn rejects invalid ID, unknown stage, entity cap, missing ID and special-mode scenes without spawning",
    spawnScene.Entities.Count == entityCount && sender.Messages.SequenceEqual([
        I18NManager.Translate("Game.Command.Spawn.InvalidId", "abc"),
        I18NManager.Translate("Game.Command.Spawn.InvalidStageId", (GameData.StageConfigData.Keys.Max() + 1).ToString()),
        I18NManager.Translate("Game.Command.Spawn.MaxEntities", SceneInstance.GmSpawnMaxEntities.ToString()),
        I18NManager.Translate("Game.Command.Spawn.Usage"),
        I18NManager.Translate("Game.Command.Spawn.UnsupportedScene")
    ]));
sender.Messages.Clear();
// Tutorials popped from the maze main page hold free-play input, so a merely running target cannot close them.
var tutorialPlayer = new PlayerInstance(60006);
await tutorialPlayer.InitializeAsync();
var tutorialMissions = tutorialPlayer.MissionManager!.Data;
tutorialMissions.EnableMissionOverride = true;
bool TutorialClosesWith(int tutorialId, int mainId, MissionPhaseEnum status)
{
    tutorialMissions.SetMainMissionStatus(mainId, status);
    return TutorialHelper.CanClose(tutorialPlayer, tutorialId);
}
Check("free-play tutorial 3006 is not closable while its target main 1000511 only runs",
    GameData.TutorialDataData[3006].TriggerParams.Any(trigger => trigger.TriggerType == "OnMazeMainPage") &&
    !TutorialClosesWith(3006, 1000511, MissionPhaseEnum.Accept) && TutorialClosesWith(3006, 1000511, MissionPhaseEnum.Finish));
Check("battle tutorial 1001 stays closable on a running target main 1000101",
    TutorialClosesWith(1001, 1000101, MissionPhaseEnum.Accept) && TutorialClosesWith(1001, 1000101, MissionPhaseEnum.Finish) &&
    !TutorialClosesWith(1001, 1000101, MissionPhaseEnum.None));
TutorialDataExcel SyntheticTutorial(int id, bool freePlay, string finishType, string finishParam) => new()
{
    TutorialID = id,
    TriggerParams = freePlay ? [new TutorialTriggerParam { TriggerType = "OnMazeMainPage", TriggerParam = "0" }] : [],
    FinishTriggerParams = [new TutorialTriggerParam { TriggerType = finishType, TriggerParam = finishParam }]
};
GameData.TutorialDataData[99000001] = SyntheticTutorial(99000001, false, "TutorialFinish", "99000002");
GameData.TutorialDataData[99000002] = SyntheticTutorial(99000002, true, "FinishMainMission", "1000511");
GameData.TutorialDataData[99000003] = SyntheticTutorial(99000003, true, "TutorialFinish", "99000004");
GameData.TutorialDataData[99000004] = SyntheticTutorial(99000004, false, "FinishMainMission", "1000511");
Check("TutorialFinish recursion takes strictness from the target tutorial's own triggers",
    !TutorialClosesWith(99000001, 1000511, MissionPhaseEnum.Accept) && TutorialClosesWith(99000003, 1000511, MissionPhaseEnum.Accept));
foreach (var id in new[] { 99000001, 99000002, 99000003, 99000004 }) GameData.TutorialDataData.Remove(id);

// BeginParam is a second gate after TakeParam: 1010502 is taken after 1010403 but begins at Trailblaze Level 13.
var gatedPlayer = new PlayerInstance(60007);
await gatedPlayer.InitializeAsync();
var gatedMissions = gatedPlayer.MissionManager!.Data;
gatedMissions.EnableMissionOverride = true;
const int LevelGatedMain = 1010502;
var levelGatedExcel = GameData.MainMissionData[LevelGatedMain];
gatedPlayer.Data.Level = 11;
gatedPlayer.Data.Exp = 0;
gatedMissions.SetMainMissionStatus(1010403, MissionPhaseEnum.Finish);
await gatedPlayer.MissionManager.AcceptMainMissionByCondition(false);
Check("BeginParam PlayerLevel 13 keeps 1010502 unaccepted at level 11 although its take gate is met",
    levelGatedExcel.IsEqual(gatedMissions) && !levelGatedExcel.IsBeginSatisfied(gatedMissions) &&
    gatedMissions.GetMainMissionStatus(LevelGatedMain) == MissionPhaseEnum.None);
var expToLevel13 = GameData.PlayerLevelConfigData[13].PlayerExp - GameData.PlayerLevelConfigData[11].PlayerExp;
await gatedPlayer.InventoryManager!.AddItem(22, expToLevel13);
Check("reaching level 13 through an exp grant accepts the level-gated main",
    gatedPlayer.Data.Level == 13 && gatedMissions.GetMainMissionStatus(LevelGatedMain) == MissionPhaseEnum.Accept);
Check("an empty or Auto BeginParam does not gate a main", GameData.MainMissionData.Values
    .Where(main => main.BeginParam.All(param => param.Type == MissionTakeTypeEnum.Auto))
    .All(main => main.IsBeginSatisfied(gatedMissions)));

// Rogue Index arms in the Simulated Universe lobby (80112001 group 3, MazeProp 1005 Prop_Rogue_Gallery).
async Task<List<PropStateEnum?>> RogueIndexArmStates(PlayerInstance owner)
{
    var lobby = new SceneInstance(owner, GameData.MazePlaneData[80112], 80112001, RogueManager.RogueEntranceId);
    var armGroup = lobby.FloorInfo!.Groups[3];
    var states = new List<PropStateEnum?>();
    foreach (var armId in new[] { 300003, 300008 })
        states.Add((await lobby.EntityLoader!.LoadProp(armGroup.PropList.Single(prop => prop.ID == armId), armGroup))?.State);
    return states;
}
Check("Rogue Index arms default open with missions disabled",
    (await RogueIndexArmStates(player)).All(state => state == PropStateEnum.Open));
tutorialMissions.SetMainMissionStatus(4030002, MissionPhaseEnum.Accept);
Check("Rogue Index arms stay closed until the Simulated Universe tutorial main 4030002 finishes",
    (await RogueIndexArmStates(tutorialPlayer)).All(state => state == PropStateEnum.Closed));
tutorialMissions.SetMainMissionStatus(4030002, MissionPhaseEnum.Finish);
Check("Rogue Index arms open once main 4030002 is finished",
    (await RogueIndexArmStates(tutorialPlayer)).All(state => state == PropStateEnum.Open));

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
    Check("teleport sends the active scene boundary immediately before scene notify and before response",
        teleportPackets.TakeLast(3).SequenceEqual(new ushort[]
            { CmdIds.SyncServerSceneChangeNotify, CmdIds.EnterSceneByServerScNotify, CmdIds.EnterSceneScRsp }));
    var movePackets = await SentPackets(async () => { await player.TryEnterScene(1000001, 0, true, mapTp: true); });
    Check("same-scene map move does not send a scene-change boundary", movePackets.Contains(CmdIds.SceneEntityMoveScNotify)
        && !movePackets.Contains(CmdIds.SyncServerSceneChangeNotify) && !movePackets.Contains(CmdIds.EnterSceneByServerScNotify));
    var invalidPackets = await SentPackets(async () => { await player.TryEnterScene(10001, 0, true); });
    Check("failed scene entry sends no boundary or scene snapshot", !invalidPackets.Contains(CmdIds.SyncServerSceneChangeNotify)
        && !invalidPackets.Contains(CmdIds.EnterSceneByServerScNotify));
    var quietPackets = await SentPackets(async () => { await player.TryEnterScene(102020107, 0, false); });
    Check("quiet scene build sends no boundary or scene snapshot", !quietPackets.Contains(CmdIds.SyncServerSceneChangeNotify)
        && !quietPackets.Contains(CmdIds.EnterSceneByServerScNotify));
    var restorePackets = await SentPackets(async () => { await player.SendPacket(new PacketGetCurSceneInfoScRsp(player)); });
    Check("login snapshot is not mistaken for a scene transition", restorePackets.SequenceEqual(new ushort[] { CmdIds.GetCurSceneInfoScRsp }));
    // A virtual lineup that only hosted a trial avatar is torn down, announced, and replaced by the own lineup.
    var ownLineupIndex = player.LineupManager!.Data.GetCurLineupIndex();
    player.LineupManager.SetExtraLineup(ExtraLineupType.LineupHeliobus, []);
    player.LineupManager.GetCurLineup()!.BaseAvatars!.Add(new March7thHoney.Database.Lineup.LineupAvatarInfo
        { BaseAvatarId = 1001, SpecialAvatarId = 9_999_001 });
    var trialTeardownPackets = await SentPackets(async () =>
    {
        await player.LineupManager.RemoveTrialAvatarFromCurTeam(9_999_001);
    });
    Check("removing the last trial avatar destroys the virtual lineup and restores the own lineup",
        player.LineupManager.Data.CurExtraLineup == -1
        && !player.LineupManager.Data.Lineups.ContainsKey((int)ExtraLineupType.LineupHeliobus + 10)
        && player.LineupManager.Data.GetCurLineupIndex() == ownLineupIndex);
    Check("trial lineup teardown notifies destroy before syncing the restored lineup",
        trialTeardownPackets.IndexOf((ushort)CmdIds.ExtraLineupDestroyNotify) is >= 0 and var destroyAt
        && trialTeardownPackets.LastIndexOf((ushort)CmdIds.SyncLineupNotify) > destroyAt);
    player.Connection = null;

    // Rogue flows run through the real handlers on the rogue player's connection.
    var rogueConnection = new ProbeConnection(serverPeer) { Player = roguePlayer };
    roguePlayer.Connection = rogueConnection;
    async Task<List<(ushort Id, byte[] Body)>> SentFrames(Func<Task> action)
    {
        await action();
        await roguePlayer.SendPacket(new BasePacket(ushort.MaxValue));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var buffer = new byte[March7thHoneyConnection.MaxPacketBytes];
        var frames = new List<(ushort, byte[])>();
        while (true)
        {
            var received = await clientPeer.ReceiveAsync(buffer, timeout.Token);
            if (received.TransportClosed || received.BytesReceived < 16)
                throw new InvalidDataException("KCP rogue test did not receive a complete game packet.");
            var id = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(4));
            if (id == ushort.MaxValue) return frames;
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(8));
            frames.Add((id, buffer.AsSpan(12, length).ToArray()));
        }
    }

    var rogueInst = roguePlayer.RogueManager.RogueInstance!;
    // Any level-1 blessing rows: the draft mechanics, not the pool contents, are under test here.
    var draftPool = GameData.RogueBuffData.Values.Where(buff => buff.MazeBuffLevel == 1).Take(5).ToList();
    rogueInst.BuffSelect = RogueBuffSelectMenu.Create(rogueInst, draftPool);
    var pickedBuff = rogueInst.BuffSelect.SelectBuffs[0].MazeBuffID;
    Check("Rogue exit stays shut while a blessing draft is pending",
        !await roguePlayer.RogueManager.OpenRoomExit() &&
        (!rogueRoomHasMonsters || rogueDoors.All(door => door.State == PropStateEnum.Closed)));
    var pickRequest = new OFEDNNJIJFL { MPLGEEGOAMC = 3, DLKIKBIDJDB = new BPLNKHGAILK { BAOFIGFFMLN = (uint)pickedBuff } };
    var pickFrames = await SentFrames(() => new HandlerHandleRogueCommonPendingActionCsReq()
        .OnHandle(rogueConnection, [], pickRequest.ToByteArray()));
    var doorRefreshIndex = pickFrames.FindIndex(frame => frame.Id == CmdIds.SceneGroupRefreshScNotify);
    var pickRspIndex = pickFrames.FindIndex(frame => frame.Id == CmdIds.HandleRogueCommonPendingActionScRsp);
    List<SceneEntityInfo> openedDoors = doorRefreshIndex < 0 ? [] : SceneGroupRefreshScNotify.Parser.ParseFrom(pickFrames[doorRefreshIndex].Body)
        .GroupRefreshList.SelectMany(group => group.RefreshEntity).Select(refresh => refresh.AddEntity)
        .Where(entity => entity?.Prop != null).ToList();
    Check("blessing pick refreshes the exit portal Open with its destination before the response",
        doorRefreshIndex >= 0 && doorRefreshIndex < pickRspIndex && openedDoors.Count == rogueDoors.Count &&
        openedDoors.All(entity => entity.Prop.PropState == (uint)PropStateEnum.Open &&
                                  entity.Prop.ExtraInfo?.RogueGameInfo?.RoomId > 0) &&
        rogueDoors.All(door => door.State == PropStateEnum.Open));
    Check("opened Rogue exit portal is not persisted to the floor prop data",
        rogueDoors.All(door => roguePlayer.GetScenePropData(rogueRoomScene.FloorId, door.GroupId, door.InstId) == null));
    Check("an already open Rogue exit is not refreshed again", !await roguePlayer.RogueManager.OpenRoomExit());

    // A won battle with no blessing to draft clears the room at once.
    foreach (var door in rogueDoors) door.State = PropStateEnum.Closed;
    var roomMonsters = rogueRoomScene.Entities.Values.OfType<EntityMonster>().Take(1).ToList();
    var rogueBattle = new BattleInstance(roguePlayer, roguePlayer.LineupManager!.GetCurLineup()!, roomMonsters);
    var rogueBattleEnd = typeof(RogueManager).GetMethod("OnBattleEnd", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var battleFrames = await SentFrames(async () => await (ValueTask)rogueBattleEnd.Invoke(roguePlayer.RogueManager,
        [rogueBattle, new PVEBattleResultCsReq { EndStatus = BattleEndStatus.BattleEndWin }])!);
    var draftedAfterBattle = rogueInst.BuffSelect?.HasSelection == true;
    Check("Rogue battle win opens the exit unless it drafts a blessing", roomMonsters.Count > 0 && (draftedAfterBattle
        ? rogueDoors.All(door => door.State == PropStateEnum.Closed)
        : rogueDoors.All(door => door.State == PropStateEnum.Open) &&
          battleFrames.Any(frame => frame.Id == CmdIds.SceneGroupRefreshScNotify)));

    // The draft pool comes from the real RogueBuffGroup rows (100005 / 100003 / room content + 8000), so the
    // offered blessings must be classic RogueBuff rows that some classic group resolves to.
    var classicGroupBuffIds = GameData.RogueBuffGroupData.Values.OfType<RogueBuffGroupExcel>()
        .SelectMany(group => group.BuffList).Select(buff => buff.GetId()).ToHashSet();
    var draftedBuffs = rogueInst.BuffSelect?.SelectBuffs ?? [];
    var draftNotifies = battleFrames.Where(frame => frame.Id == CmdIds.SyncRogueCommonPendingActionScNotify)
        .Select(frame => SyncRogueCommonPendingActionScNotify.Parser.ParseFrom(frame.Body)).ToList();
    Check("classic Simulated Universe room win drafts classic blessings from the real RogueBuffGroup data",
        draftedAfterBattle && draftedBuffs.Count == 3 &&
        draftedBuffs.All(buff => buff is RogueBuffExcel && classicGroupBuffIds.Contains(buff.GetId()) &&
                                 GameData.RogueBuffData.GetValueOrDefault(buff.GetId()) is RogueBuffExcel) &&
        draftNotifies.Count == 2 && draftNotifies.All(notify =>
            notify.Action?.Detail?.PCHGEGFBKLF?.LEOFNINDJDB.Select(offer => (int)offer.BuffId)
                .SequenceEqual(draftedBuffs.Select(buff => buff.MazeBuffID)) == true));
    var draftedPick = draftedBuffs.FirstOrDefault();
    Check("drafted classic blessing can be picked into the run",
        draftedPick != null && roguePlayer.RogueManager.SelectBuff(draftedPick.MazeBuffID) == Retcode.RetSucc &&
        rogueInst.Buffs.TryGetValue(draftedPick.MazeBuffID, out var ownedPick) &&
        ownedPick.Level == draftedPick.MazeBuffLevel);
    rogueInst.BuffSelect = null;

    // A won run is dropped on relog while login restores its room scene: LeaveRogue has no run to settle.
    typeof(RogueManager).GetProperty(nameof(RogueManager.RogueInstance))!.SetValue(roguePlayer.RogueManager, null);
    async Task<(List<(ushort Id, byte[] Body)> Frames, LeaveRogueScRsp? Rsp)> LeaveRogue()
    {
        var frames = await SentFrames(() => new HandlerLeaveRogueCsReq().OnHandle(rogueConnection, [], []));
        var rsp = frames.Where(frame => frame.Id == CmdIds.LeaveRogueScRsp)
            .Select(frame => LeaveRogueScRsp.Parser.ParseFrom(frame.Body)).SingleOrDefault();
        return (frames, rsp);
    }
    var orphanLeave = await LeaveRogue();
    Check("LeaveRogue with no live run returns an orphaned Rogue room to the lobby",
        orphanLeave.Rsp is { Retcode: 0 } && orphanLeave.Rsp.Scene?.EntryId == RogueManager.RogueEntranceId &&
        orphanLeave.Rsp.Lineup != null && roguePlayer.SceneInstance?.EntryId == RogueManager.RogueEntranceId &&
        roguePlayer.LineupManager.Data.CurExtraLineup == -1 &&
        orphanLeave.Frames.FindIndex(frame => frame.Id == CmdIds.SyncServerSceneChangeNotify) is var boundary and >= 0 &&
        boundary < orphanLeave.Frames.FindIndex(frame => frame.Id == CmdIds.LeaveRogueScRsp) &&
        orphanLeave.Frames.All(frame => frame.Id != CmdIds.SyncRogueFinishScNotify));
    var lobbyLeave = await LeaveRogue();
    Check("LeaveRogue outside a Rogue room with no run still reports status mismatch",
        lobbyLeave.Rsp?.Retcode == (uint)Retcode.RetRogueStatusNotMatch);

    // A degenerate talk report ("0", no sub id) is ACK-only even when the tracked main has exactly one running
    // Talk finish gate on the current floor (the official capture ACKs it and the client finishes the gate later).
    var talkPlayer = new PlayerInstance(60008);
    await talkPlayer.InitializeAsync();
    talkPlayer.MissionManager!.Data.EnableMissionOverride = true;
    const int TalkGateMain = 1010202;
    const int TalkGateSub = 101020206;
    var talkGate = GameData.SubMissionInfoData[TalkGateSub].SubMissionInfo!;
    var talkEntrance = GameData.MapEntranceData.Values.First(entrance => entrance.FloorID == talkGate.LevelFloorID);
    var talkScene = new SceneInstance(talkPlayer, GameData.MazePlaneData[talkEntrance.PlaneID], talkEntrance.FloorID,
        talkEntrance.ID);
    typeof(PlayerInstance).GetProperty(nameof(PlayerInstance.SceneInstance))!.SetValue(talkPlayer, talkScene);
    talkPlayer.MissionManager.Data.SetMainMissionStatus(TalkGateMain, MissionPhaseEnum.Accept);
    talkPlayer.MissionManager.Data.SetSubMissionStatus(TalkGateSub, MissionPhaseEnum.Accept);
    talkPlayer.MissionManager.Data.TrackingMainMissionId = TalkGateMain;
    talkPlayer.Connection = new ProbeConnection(serverPeer) { Player = talkPlayer };
    var talkFrames = await SentFrames(() => new HandlerFinishTalkMissionCsReq()
        .OnHandle(talkPlayer.Connection, [], new FinishTalkMissionCsReq { TalkStr = "0" }.ToByteArray()));
    Check("degenerate FinishTalkMission is ACK-only and leaves the talk finish gate running",
        talkGate.FinishType == MissionFinishTypeEnum.Talk &&
        talkPlayer.MissionManager.GetSubMissionStatus(TalkGateSub) == MissionPhaseEnum.Accept &&
        talkPlayer.MissionManager.GetMainMissionStatus(TalkGateMain) == MissionPhaseEnum.Accept &&
        talkFrames.Select(frame => frame.Id).SequenceEqual(new[] { (ushort)CmdIds.FinishTalkMissionScRsp }));
    await SentFrames(() => new HandlerFinishTalkMissionCsReq()
        .OnHandle(talkPlayer.Connection, [], new FinishTalkMissionCsReq { TalkStr = talkGate.ParamStr1 }.ToByteArray()));
    Check("the client's own talk string still finishes the gate",
        talkPlayer.MissionManager.GetSubMissionStatus(TalkGateSub) == MissionPhaseEnum.Finish);
    talkPlayer.Connection = null;

    // Classic Simulated Universe command pairs (4.6 cmdid-registry.txt): enhance 1885/1807, pick 1864/1821.
    int OpcodeOf(Type handler) => handler.GetCustomAttribute<Opcode>()!.CmdId;
    Check("classic Simulated Universe enhance and pick handlers listen on the 18xx command ids",
        OpcodeOf(typeof(HandlerEnhanceRogueBuffCsReq)) == 1885 && CmdIds.EnhanceRogueBuffScRspCmd1807 == 1807 &&
        OpcodeOf(typeof(HandlerPickRogueAvatarCsReq)) == 1864 && CmdIds.PickRogueAvatarScRsp == 1821);
    var enhanceFrames = await SentFrames(() => new HandlerEnhanceRogueBuffCsReq().OnHandle(rogueConnection, [], []));
    Check("classic Simulated Universe enhance answers on 1807", enhanceFrames.Select(frame => frame.Id)
        .SequenceEqual(new[] { (ushort)CmdIds.EnhanceRogueBuffScRspCmd1807 }));
    var pickAvatarRequest = new PickRogueAvatarCsReq { PropEntityId = 4194339, DFPPCAJDBFK = { 4000005 }, NOEPBNMJFGL = { 1001 } };
    var pickAvatarFrames = await SentFrames(() => new HandlerPickRogueAvatarCsReq()
        .OnHandle(rogueConnection, [], pickAvatarRequest.ToByteArray()));
    var pickAvatarRsp = pickAvatarFrames.Where(frame => frame.Id == CmdIds.PickRogueAvatarScRsp)
        .Select(frame => PickRogueAvatarScRsp.Parser.ParseFrom(frame.Body)).SingleOrDefault();
    Check("PickRogueAvatar echoes special ids (req #11) in rsp #6 and base ids (req #14) in rsp #15",
        PickRogueAvatarCsReq.DFPPCAJDBFKFieldNumber == 11 && PickRogueAvatarCsReq.NOEPBNMJFGLFieldNumber == 14 &&
        PickRogueAvatarScRsp.BFEDFBEKOBIFieldNumber == 6 && PickRogueAvatarScRsp.NOEPBNMJFGLFieldNumber == 15 &&
        pickAvatarRsp is { Retcode: 0 } && pickAvatarRsp.BFEDFBEKOBI.SequenceEqual(new uint[] { 4000005 }) &&
        pickAvatarRsp.NOEPBNMJFGL.SequenceEqual(new uint[] { 1001 }));
    roguePlayer.Connection = null;
    await outbound.StopAsync();
    await inbound.StopAsync();
}

// Role Trial (TrialActivity): start, battle stage choice, clear, reward claim, leave and relogin rescue, driven
// through the real request handlers and captured over the in-memory KCP pair.
Directory.CreateDirectory(Path.Combine(scratch, "Custom"));
File.Copy(Path.Combine(repository, "Config/Custom/ActivityConfig.json"), Path.Combine(scratch, "Custom/ActivityConfig.json"));
var schedule = ResourceManager.LoadCustomFile<ActivityConfig>("Activity", "ActivityConfig");
// The Role Trial modules of the captured 4.6 official schedule (ActivityModuleDemo -> AvatarDemoConfig TrialActivity).
int[] trialModules = [2000603, 2001101, 2001102, 2001103, 2001104, 2001105, 2001106, 2001107, 2001108, 2001109,
    2002303, 2002502, 2006301, 2006302, 2008201, 2008401, 2008601, 2008701, 2009301];
Check("activity schedule lists every Role Trial module", schedule != null &&
    trialModules.All(id => schedule.ScheduleData.Any(row => row.ActivityId == id && row.PanelId is 20011 or 20064 or 20087 or 20091 or 20092 or 20093)));

// Banners.json is parsed row by row and a bad row is only skipped with a warning, so check that every captured 4.6
// pool survives loading and only references avatars and light cones present in the resource cache.
File.Copy(Path.Combine(repository, "Config/Custom/Banners.json"), Path.Combine(scratch, "Custom/Banners.json"));
var banners = ResourceManager.LoadBanners();
int[] bannerIds = [1001, 2139, 2140, 2141, 3139, 3140, 3141, 5001, 5002, 5003, 5004, 6001, 6002, 6003, 6004];
Check("banner config loads every captured 4.6 pool with known rate-up items",
    banners.Banners.Select(b => b.GachaId).Order().SequenceEqual(bannerIds) &&
    banners.Banners.All(b => b.RateUpItems5.Concat(b.RateUpItems4).Concat(b.PrizeItems)
        .All(id => GameData.AvatarConfigData.ContainsKey(id) || GameData.EquipmentConfigData.ContainsKey(id))));

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

var trialPlayer = new PlayerInstance(60010);
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

// Raid runs (ported from yuukisr_go 0a77679/e776655/24d080e): a run whose raid is missing from this version's
// tables must never trap the player, and ResetLocationOnLogin abandons the run behind the saved location.
var raidEntry = GameData.MapEntranceData.Values.OrderBy(e => e.ID).First(e =>
{
    if (!GameData.MazePlaneData.TryGetValue(e.PlaneID, out var plane) || plane.PlaneType != PlaneTypeEnum.Raid)
        return false;
    GameData.GetFloorInfo(e.PlaneID, e.FloorID, out var floor);
    return (floor?.GetAnchorInfo(e.StartGroupID, e.StartAnchorID) ??
            floor?.GetAnchorInfo(floor.StartGroupID, floor.StartAnchorID)) != null;
}).ID;
const int MissingRaidId = 99999;
var knownRaid = GameData.RaidConfigData.Values.OrderBy(r => r.RaidID).First(r => r.HardLevel == 0);

async Task<PlayerInstance> RaidPlayer(int uid, int raidId)
{
    var raider = new PlayerInstance(uid);
    await raider.InitializeAsync();
    await raider.TryEnterScene(TrialReturnEntry, 0, false);
    var raidData = raider.RaidManager!.Data;
    raidData.RaidRecordDatas[raidId] = new Dictionary<int, March7thHoney.Database.Scene.RaidRecord>
    {
        [0] = new()
        {
            RaidId = raidId, WorldLevel = 0, Status = RaidStatus.Doing, OldEntryId = TrialReturnEntry,
            OldPos = raider.Data.Pos!, OldRot = raider.Data.Rot!
        }
    };
    raidData.CurRaidId = raidId;
    raidData.CurRaidWorldLevel = 0;
    raider.TryPlaceAtEntrance(raidEntry);
    return raider;
}

var orphanRaider = await RaidPlayer(60011, MissingRaidId);
var orphanRelogin = new PlayerInstance(orphanRaider.Data);
await orphanRelogin.InitializeAsync();
await orphanRelogin.OnLogin();
Check("relogin inside a raid missing from the tables abandons it and returns to the entry point",
    orphanRelogin.RaidManager!.Data.CurRaidId == 0 && !orphanRelogin.RaidManager.Data.RaidRecordDatas.ContainsKey(MissingRaidId)
    && orphanRelogin.Data.EntryId == TrialReturnEntry && orphanRelogin.SceneInstance?.Excel.PlaneType != PlaneTypeEnum.Raid);

var orphanLeaver = await RaidPlayer(60012, MissingRaidId);
await orphanLeaver.RaidManager!.LeaveRaid(true);
Check("leaving a raid missing from the tables returns to the entry point and drops the record",
    orphanLeaver.RaidManager.Data.CurRaidId == 0 && !orphanLeaver.RaidManager.Data.RaidRecordDatas.ContainsKey(MissingRaidId)
    && orphanLeaver.Data.EntryId == TrialReturnEntry);

var recordlessLeaver = await RaidPlayer(60013, knownRaid.RaidID);
recordlessLeaver.RaidManager!.Data.RaidRecordDatas.Clear();
await recordlessLeaver.RaidManager.LeaveRaid(true);
Check("leaving a raid whose record is gone releases the player to the default spawn",
    recordlessLeaver.RaidManager.Data.CurRaidId == 0 && recordlessLeaver.Data.EntryId == PlayerInstance.DefaultSpawnEntryId);

var keptRaider = await RaidPlayer(60014, knownRaid.RaidID);
var keptRelogin = new PlayerInstance(keptRaider.Data);
await keptRelogin.InitializeAsync();
await keptRelogin.OnLogin();
Check("relogin inside a known raid still resumes it by default",
    keptRelogin.RaidManager!.Data.CurRaidId == knownRaid.RaidID && keptRelogin.Data.EntryId == raidEntry);

var resetRaider = await RaidPlayer(60015, knownRaid.RaidID);
config.ServerOption.ResetLocationOnLogin = true;
var resetRelogin = new PlayerInstance(resetRaider.Data);
await resetRelogin.InitializeAsync();
await resetRelogin.OnLogin();
config.ServerOption.ResetLocationOnLogin = false;
Check("ResetLocationOnLogin abandons the raid run and logs in at the default spawn",
    resetRelogin.RaidManager!.Data.CurRaidId == 0 && !resetRelogin.RaidManager.Data.RaidRecordDatas.ContainsKey(knownRaid.RaidID)
    && resetRelogin.Data.EntryId == PlayerInstance.DefaultSpawnEntryId
    && resetRelogin.SceneInstance?.EntryId == PlayerInstance.DefaultSpawnEntryId
    && !resetRelogin.LineupManager!.GetCurLineup()!.IsExtraLineup());
Check("reset raid info notify keeps an empty item_list",
    RaidInfoNotify.Parser.ParseFrom(new March7thHoney.GameServer.Server.Packet.Send.Raid.PacketRaidInfoNotify().Data).ItemList != null);

// CommonRogueQuery: the query type is the oneof arm's field number in the active protocol (4.6 moved the
// function-unlock arm BBEKBJPHEJK from 15 to 1), so the unlock set must follow the arm, not the 4.5 literal.
var commonQuery = RogueManager.BuildCommonQueryEntries([
    (uint)CCKJAEDDDCL.BBEKBJPHEJKFieldNumber, (uint)CCKJAEDDDCL.MECOEFEJANEFieldNumber,
    (uint)CCKJAEDDDCL.ABPOOCOOKCFFieldNumber, (uint)CCKJAEDDDCL.BMGOLFNGHGHFieldNumber]).ToList();
Check("CommonRogueQuery answers the function-unlock arm by its active field number",
    commonQuery.Count == 4 && commonQuery[0].BBEKBJPHEJK?.LFCGPOKMJLC.Count == Enum.GetValues<MGJDNGHDIKG>().Length
    && commonQuery.Skip(1).All(entry => entry.KKNBOACNCONCase == CCKJAEDDDCL.KKNBOACNCONOneofCase.None));

// Data responses whose sub-messages the 4.6 client dereferences ship them present and empty.
T Present<T>(int cmdId, T rsp) where T : IMessage<T>, new() =>
    new MessageParser<T>(() => new T()).ParseFrom(
        new March7thHoney.GameServer.Server.Packet.Send.MiscModule.PacketPresentEmptyScRsp(cmdId, rsp).Data);
var socialPlay = Present(CmdIds.SocialPlayGetDataScRsp, new SocialPlayGetDataScRsp());
Check("SocialPlayGetDataScRsp ships its room gate data present",
    socialPlay.ELDIONMAAHO != null && socialPlay.NDDJFLPBHBL != null && socialPlay.ELDIONMAAHO.KABGCGENKLK != null);
Check("PetMarbleGetDataScRsp ships its data present",
    Present(CmdIds.PetMarbleGetDataScRsp, new PetMarbleGetDataScRsp()).BJBJLBIOPKA != null);
Check("PetMarbleGetCycleTrialPetsScRsp ships its trial pet data present",
    Present(CmdIds.PetMarbleGetCycleTrialPetsScRsp, new PetMarbleGetCycleTrialPetsScRsp()).ACCGIILHFKL != null);
if (HandlerManager.handlers.Count == 0) HandlerManager.Init();
Check("presence handlers are registered for the three requests", new[]
    {
        CmdIds.SocialPlayGetDataCsReq, CmdIds.PetMarbleGetDataCsReq, CmdIds.PetMarbleGetCycleTrialPetsCsReq
    }.All(id => HandlerManager.GetHandler(id) != null));

Console.WriteLine($"Login/scene regression: {checks} checks, {failures.Count} failures. Temporary artifacts: {scratch}");
Environment.ExitCode = failures.Count == 0 ? 0 : 1;

sealed class ProbeEntity : BaseGameEntity
{
    public override int EntityId { get; set; }
    public override int GroupId { get; set; }
    public override SceneEntityInfo ToProto() => new() { EntityId = (uint)EntityId, GroupId = (uint)GroupId };
}

sealed class ProbeSender(int uid = 0) : ICommandSender
{
    public List<string> Messages { get; } = [];
    public ValueTask SendMsg(string msg) { Messages.Add(msg); return ValueTask.CompletedTask; }
    public bool HasPermission(string permission) => true;
    public int GetSender() => uid;
    public bool IsConsole => uid == 0;
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
