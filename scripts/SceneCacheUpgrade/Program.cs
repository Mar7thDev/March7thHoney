using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using March7thHoney.Data;
using March7thHoney.Enums.Scene;
using MemoryPack;

// Offline, targeted migration: no configuration, database, network, global GameData, or load hooks.
// Keep the existing cache's content, including operator changes; only enrich the new resource flag.
if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: SceneCacheUpgrade <existing Resource.bin> <Resources directory> <new output file>");
    return 2;
}
var inputPath = Path.GetFullPath(args[0]);
var resourceRoot = Path.GetFullPath(args[1]).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;
var outputPath = Path.GetFullPath(args[2]);
if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase) || File.Exists(outputPath))
    throw new InvalidOperationException("Choose a new output file; this tool never overwrites the input or an existing file.");

MemoryPackFormatterProvider.Register(new JsonObjectMemoryPackFormatter());
MemoryPackFormatterProvider.Register(new BigIntegerMemoryPackFormatter());
using var input = File.OpenRead(inputPath);
using var gzip = new GZipStream(input, CompressionMode.Decompress);
using var decoded = new MemoryStream();
gzip.CopyTo(decoded);
var snapshot = MemoryPackSerializer.Deserialize<GameDataSnapshot>(decoded.ToArray())
               ?? throw new InvalidDataException("Empty resource snapshot.");
var baseline = MemoryPackSerializer.Serialize(snapshot);
var oldFlags = new Dictionary<(string Floor, int Group), bool?>();
var rawFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
var atmosphereCount = 0;
var missingUnaffectedGroups = 0;

foreach (var (floorKey, floor) in snapshot.FloorInfoData)
{
    var floorPath = Path.GetFullPath(Path.Combine(resourceRoot, "Config/LevelOutput/RuntimeFloor", floorKey + ".json"));
    if (!floorPath.StartsWith(resourceRoot, StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException($"Floor resource is outside the resource directory: {floorKey}");
    using var floorJson = File.Exists(floorPath) ? JsonDocument.Parse(File.ReadAllBytes(floorPath)) : null;
    var commonMap = default(JsonElement);
    floorJson?.RootElement.TryGetProperty("GroupInstanceCommonMap", out commonMap);
    foreach (var (groupId, group) in floor.Groups)
    {
        // Per-instance floor metadata wins, just like FloorInfo.StampCommonMapFields. Some old cached
        // groups have no remaining standalone export, but their authoritative common-map row survives.
        if (commonMap.ValueKind == JsonValueKind.Object
            && commonMap.TryGetProperty(groupId.ToString(), out var common)
            && common.TryGetProperty("IsHoyoGroup", out var commonFlag))
        {
            oldFlags.Add((floorKey, groupId), group.IsHoyoGroup);
            group.IsHoyoGroup = commonFlag.GetBoolean();
            if (group.IsHoyoGroup == true && group.Category == GroupCategoryEnum.Atmosphere) atmosphereCount++;
            continue;
        }
        // ResourceManager loads in declaration order and the last existing, non-deleted definition
        // wins when a floor reuses an instance id. Match that behavior instead of assuming uniqueness.
        var definition = floor.GroupInstanceList.LastOrDefault(info => info.ID == groupId && !info.IsDelete
            && !info.GroupPath.Contains("_D100") && File.Exists(Path.Combine(resourceRoot, info.GroupPath)));
        if (definition == null)
        {
            if (group.LoadSide != GroupLoadSideEnum.Client && group.Category == GroupCategoryEnum.Atmosphere)
                throw new InvalidDataException($"Missing server atmosphere source group: {floorKey}/{groupId} ({group.GroupName})");
            // Older caches can retain unrelated groups whose exports no longer exist. Keep them intact;
            // this flag only controls server Atmosphere groups. Client-only groups are already excluded.
            missingUnaffectedGroups++;
            continue;
        }
        var groupPath = Path.GetFullPath(Path.Combine(resourceRoot, definition.GroupPath));
        if (!groupPath.StartsWith(resourceRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Group resource is outside the resource directory: {floorKey}/{groupId}");
        if (!rawFlags.TryGetValue(groupPath, out var flag))
        {
            using var json = JsonDocument.Parse(File.ReadAllBytes(groupPath));
            flag = json.RootElement.TryGetProperty("IsHoyoGroup", out var value) && value.GetBoolean();
            rawFlags.Add(groupPath, flag);
        }
        oldFlags.Add((floorKey, groupId), group.IsHoyoGroup);
        group.IsHoyoGroup = flag;
        if (flag && group.Category == GroupCategoryEnum.Atmosphere) atmosphereCount++;
    }
}
if (oldFlags.Count == 0) throw new InvalidDataException("No scene groups to upgrade.");

var upgraded = MemoryPackSerializer.Serialize(snapshot);
var verification = MemoryPackSerializer.Deserialize<GameDataSnapshot>(upgraded)!;
foreach (var ((floorKey, groupId), oldFlag) in oldFlags)
{
    verification.FloorInfoData[floorKey].Groups[groupId].IsHoyoGroup = oldFlag;
    snapshot.FloorInfoData[floorKey].Groups[groupId].IsHoyoGroup = oldFlag;
}
// ConcurrentBag enumeration reverses during deserialization. Compare equally round-tripped
// snapshots, and also verify that the original in-memory snapshot changed only in the new flag.
var baselineRoundTrip = MemoryPackSerializer.Serialize(MemoryPackSerializer.Deserialize<GameDataSnapshot>(baseline));
if (!baseline.AsSpan().SequenceEqual(MemoryPackSerializer.Serialize(snapshot)) ||
    !baselineRoundTrip.AsSpan().SequenceEqual(MemoryPackSerializer.Serialize(verification)))
    throw new InvalidDataException("Round-trip changed data other than IsHoyoGroup; no output was written.");

using (var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write))
using (var compressed = new GZipStream(output, CompressionLevel.Optimal))
    compressed.Write(upgraded);
Console.WriteLine($"Upgraded {oldFlags.Count} groups across {snapshot.FloorInfoData.Count} floors; " +
                  $"{atmosphereCount} Hoyo atmosphere groups. All other snapshot data verified unchanged.");
Console.WriteLine($"Preserved {missingUnaffectedGroups} unaffected groups without current source files.");
Console.WriteLine($"Output: {outputPath}");
Console.WriteLine($"SHA256: {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(outputPath)))}");
return 0;
