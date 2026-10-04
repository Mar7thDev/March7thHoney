using System.IO.Compression;
using System.Text.Json;
using March7thHoney.Data;
using March7thHoney.Internationalization;
using March7thHoney.Util;
using MemoryPack;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: ResourceStartupRegression <raw resources> <matching cache> <new report directory>");
    return 2;
}
var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var rawRoot = Path.GetFullPath(args[0]);
var sourceCache = Path.GetFullPath(args[1]);
var reportRoot = Path.GetFullPath(args[2]);
if (Directory.Exists(reportRoot)) throw new IOException("Choose a new report directory.");
Directory.CreateDirectory(reportRoot);
Environment.CurrentDirectory = reportRoot;
var config = ConfigManager.Config;
config.Path.ConfigPath = Path.Combine(reportRoot, "Config");
config.Path.ResourcePath = rawRoot;
config.Path.LanguagePath = Path.Combine(repository, "Config", "Languages");
config.Path.LogPath = reportRoot;
config.ServerOption.Language = "EN";
config.ServerOption.UseCache = false;
Directory.CreateDirectory(config.Path.ConfigPath);
// Read operator JSON from the checkout, but cache writes only target this isolated copy.
foreach (var source in Directory.EnumerateFiles(Path.Combine(repository, "Config"), "*.json"))
    File.Copy(source, Path.Combine(config.Path.ConfigPath, Path.GetFileName(source)));
var custom = Path.Combine(repository, "Config", "Custom");
foreach (var source in Directory.EnumerateFiles(custom, "*.json", SearchOption.AllDirectories))
{
    var destination = Path.Combine(config.Path.ConfigPath, "Custom", Path.GetRelativePath(custom, source));
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    File.Copy(source, destination);
}
Logger.SetLogFile(new FileInfo(Path.Combine(reportRoot, "startup.log")));
I18NManager.LoadLanguage();
var checks = 0;
var failures = new List<string>();
void Check(string name, bool success)
{
    checks++;
    Console.WriteLine($"{(success ? "PASS" : "FAIL")} {name}");
    if (!success) failures.Add(name);
}
var cachePath = ResourceCache.CachePath;
File.Copy(sourceCache, cachePath);
if (!ResourceCache.LoadCache(validateAgainstResources: false)) throw new InvalidDataException("Matching cache cannot be loaded.");
var expectedAvatarCount = GameData.AvatarConfigData.Count;
var expectedFloorCount = GameData.FloorInfoData.Count;
var snapshot = GameData.CreateSnapshot();
var marker = GameData.FinalityBattleTurnLimit + 10000;
(snapshot.ActivityRuntimeData ?? throw new InvalidDataException("Missing activity runtime snapshot.")).FinalityBattleTurnLimit = marker;
byte[] Serialize(GameDataSnapshot data)
{
    using var output = new MemoryStream();
    using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        gzip.Write(MemoryPackSerializer.Serialize(data));
    return output.ToArray();
}
var cacheBytes = Serialize(snapshot);
File.WriteAllBytes(cachePath, cacheBytes);

async Task Fallback(string name, string path)
{
    config.Path.ResourcePath = path;
    // Ensure failed startup cannot retain data from a preceding partial load.
    GameData.AvatarConfigData.Remove(GameData.AvatarConfigData.Keys.First());
    var loadedFromRaw = await ResourceStartupLoader.LoadAsync();
    Check(name, !loadedFromRaw && ResourceCache.IsComplete &&
        GameData.FinalityBattleTurnLimit == marker && GameData.AvatarConfigData.Count == expectedAvatarCount &&
        GameData.FloorInfoData.Count == expectedFloorCount);
}
var absent = Path.Combine(reportRoot, "MissingResources");
await Fallback("missing directory loads the cache even with UseCache disabled", absent);
var wrongFile = Path.Combine(reportRoot, "Resources-file.json");
File.WriteAllText(wrongFile, "{}");
await Fallback("ResourcePath naming a file falls back to Config/Resource.bin", wrongFile);
await Fallback("invalid path falls back without running raw-table cache checks", "invalid\0path");
await Fallback("empty path does not read resources from the current directory", "");
var empty = Path.Combine(reportRoot, "EmptyResources");
Directory.CreateDirectory(empty);
await Fallback("existing but empty resource directory falls back", empty);

var broken = Path.Combine(reportRoot, "BrokenResources");
var floorDirectory = Path.Combine("Config", "LevelOutput", "RuntimeFloor");
Directory.CreateDirectory(Path.Combine(broken, floorDirectory));
var floor = Directory.EnumerateFiles(Path.Combine(rawRoot, floorDirectory), "*.json").First();
File.Copy(floor, Path.Combine(broken, floorDirectory, Path.GetFileName(floor)));
foreach (var directory in config.Path.ExcelOutputDirs)
{
    Directory.CreateDirectory(Path.Combine(broken, directory));
    foreach (var source in Directory.EnumerateFiles(Path.Combine(rawRoot, directory), "*.json"))
        File.Copy(source, Path.Combine(broken, directory, Path.GetFileName(source)));
}
var avatarTable = config.Path.ExcelOutputDirs.Select(dir => Path.Combine(broken, dir, "AvatarConfig.json"))
    .First(File.Exists);
var avatarBytes = File.ReadAllBytes(avatarTable);
File.Delete(avatarTable);
await Fallback("missing required table loads the cache directly", broken);
File.WriteAllBytes(avatarTable, avatarBytes);
using (var locked = new FileStream(avatarTable, FileMode.Open, FileAccess.Read, FileShare.None))
    await Fallback("unreadable required table loads the cache directly", broken);
File.WriteAllText(avatarTable, "{ malformed JSON");
await Fallback("parse failure discards partially loaded tables and restores the whole cache", broken);
Check("failed raw loads never overwrite the existing cache", File.ReadAllBytes(cachePath).SequenceEqual(cacheBytes));

async Task Reject(string name)
{
    config.Path.ResourcePath = absent;
    var rejected = false;
    try { await ResourceStartupLoader.LoadAsync(); }
    catch (InvalidDataException exception)
    {
        rejected = exception.InnerException is AggregateException { InnerExceptions.Count: 2 };
    }
    Check(name, rejected && !ResourceCache.IsComplete && GameData.AvatarConfigData.Count == 0 &&
        GameData.FloorInfoData.Count == 0);
}
File.Delete(cachePath);
await Reject("missing resources and missing cache stop startup without partial state");
File.WriteAllBytes(cachePath, [0x1F, 0x8B, 0]);
await Reject("corrupt cache stops startup rather than being accepted as a fallback");
snapshot.EquipmentConfigData.Clear();
File.WriteAllBytes(cachePath, Serialize(snapshot));
await Reject("fallback still rejects a decoded cache missing required equipment data");
File.WriteAllBytes(cachePath, cacheBytes);
if (!ResourceCache.LoadCache(validateAgainstResources: false)) throw new InvalidDataException("Fixture cache cannot be restored.");
var sparse = GameData.CreateSnapshot();
sparse.FloorInfoData.Clear();
File.WriteAllBytes(cachePath, Serialize(sparse));
await Reject("fallback rejects a decoded cache without scene data");
File.WriteAllBytes(cachePath, cacheBytes);
if (!ResourceCache.LoadCache(validateAgainstResources: false)) throw new InvalidDataException("Fixture cache cannot be restored.");
sparse = GameData.CreateSnapshot();
sparse.AvatarConfigData.Clear();
File.WriteAllBytes(cachePath, Serialize(sparse));
await Reject("fallback rejects a decoded cache without character data");
File.WriteAllBytes(cachePath, cacheBytes);
config.Path.ResourcePath = rawRoot;
config.ServerOption.UseCache = false;
Check("readable ResourcePath takes priority over an existing cache", await ResourceStartupLoader.LoadAsync() &&
    GameData.FinalityBattleTurnLimit != marker && GameData.AvatarConfigData.Count == expectedAvatarCount &&
    GameData.FloorInfoData.Count == expectedFloorCount);
Check("UseCache false leaves the existing cache unchanged", File.ReadAllBytes(cachePath).SequenceEqual(cacheBytes));
File.WriteAllBytes(cachePath, [0x1F, 0x8B, 0]);
config.ServerOption.UseCache = true;
Check("readable resources rebuild an unusable existing cache", await ResourceStartupLoader.LoadAsync());
var rawLimit = GameData.FinalityBattleTurnLimit;
ResourceCache.ClearGameData();
Check("rebuilt cache is readable and contains the selected raw data", ResourceCache.LoadCache() &&
    GameData.FinalityBattleTurnLimit == rawLimit && GameData.AvatarConfigData.Count == expectedAvatarCount);
Check("cache replacement leaves no temporary cache files", !Directory.EnumerateFiles(config.Path.ConfigPath, ".Resource.*.tmp").Any());
File.Delete(cachePath);
Directory.CreateDirectory(cachePath);
Check("cache write failure keeps successfully loaded raw data available", await ResourceStartupLoader.LoadAsync() &&
    ResourceCache.IsComplete && GameData.AvatarConfigData.Count == expectedAvatarCount && Directory.Exists(cachePath));
Check("failed cache replacement cleans up its temporary file", !Directory.EnumerateFiles(config.Path.ConfigPath, ".Resource.*.tmp").Any());

File.WriteAllText(Path.Combine(reportRoot, "audit.json"), JsonSerializer.Serialize(new
{
    RawResources = rawRoot, SourceCache = sourceCache, Checks = checks, Failures = failures,
    ProductionDatabaseUsed = false, ProductionCacheModified = false
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Resource startup regression checks: {checks}, failures: {failures.Count}");
return failures.Count == 0 ? 0 : 1;
