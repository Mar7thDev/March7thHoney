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
using March7thHoney.Data.Excel;
using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Database.Quests;
using March7thHoney.Enums;
using March7thHoney.Enums.Mission;
using March7thHoney.Enums.Scene;
using March7thHoney.Enums.Task;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.GameServer.Game.RogueMagic;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Game.Tutorial;
using March7thHoney.GameServer.Server;
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

if (args is ["--legacy-cache", var legacyCachePath])
{
    File.Copy(Path.GetFullPath(legacyCachePath, repository), Path.Combine(scratch, "Resource.bin"));
    Check("legacy cache deserializes but is rejected when server atmosphere metadata is missing", !ResourceCache.LoadCache()
        && GameData.FloorInfoData["P10202_F10202001"].Groups[169].IsHoyoGroup == null);
}
File.Copy(Path.Combine(repository, "Config/Resource.bin"), Path.Combine(scratch, "Resource.bin"), overwrite: true);
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
    var talkPlayer = new PlayerInstance(60005);
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
    roguePlayer.Connection = null;
    await outbound.StopAsync();
    await inbound.StopAsync();
}

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
