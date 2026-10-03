using System.IO.Compression;
using March7thHoney.Data;
using March7thHoney.Data.Config.Rogue;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.TournRogue;
using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Offline, targeted migration: no configuration, database, network, global GameData, or load hooks.
// Keeps every table of the existing cache byte-for-byte and only adds the Divergent Universe data the
// RogueTourn port reads: the Persona layer/preset/composition/gift tables, the exp-reward ladder, the
// blessing-pool TournMode column and the event Act graphs.
if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: RogueTournCacheUpgrade <existing Resource.bin> <Resources directory> <new output file>");
    return 2;
}

var inputPath = Path.GetFullPath(args[0]);
var resourceRoot = Path.GetFullPath(args[1]);
var outputPath = Path.GetFullPath(args[2]);
if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase) || File.Exists(outputPath))
    throw new InvalidOperationException("Choose a new output file; this tool never overwrites the input or an existing file.");

MemoryPackFormatterProvider.Register(new JsonObjectMemoryPackFormatter());
MemoryPackFormatterProvider.Register(new BigIntegerMemoryPackFormatter());

GameDataSnapshot Load(string path)
{
    using var input = File.OpenRead(path);
    using var gzip = new GZipStream(input, CompressionMode.Decompress);
    using var decoded = new MemoryStream();
    gzip.CopyTo(decoded);
    return MemoryPackSerializer.Deserialize<GameDataSnapshot>(decoded.ToArray())
           ?? throw new InvalidDataException("Empty resource snapshot.");
}

var snapshot = Load(inputPath);
var runtime = snapshot.GridFightRuntimeData ?? throw new InvalidDataException("Cache has no runtime snapshot.");
if (runtime.RogueTournRuntimeData != null)
    throw new InvalidDataException("This cache already carries the RogueTourn tables.");
var tournGroups = snapshot.RogueBuffGroupData.Values.OfType<RogueTournBuffGroupExcel>().ToList();
if (tournGroups.Any(group => group.TournMode != RogueTournModeEnum.Permanent))
    throw new InvalidDataException("This cache already carries RogueTournBuffGroup.TournMode.");
var baseline = MemoryPackSerializer.Serialize(snapshot);

List<T> ReadExcel<T>(string name)
{
    var path = Path.Combine(resourceRoot, "ExcelOutput", name);
    if (!File.Exists(path)) throw new FileNotFoundException($"Missing raw table {path}");
    return JArray.Parse(File.ReadAllText(path)).Select(row => row.ToObject<T>()!).ToList();
}

var data = new RogueTournRuntimeDataSnapshot();
foreach (var row in ReadExcel<RoguePersonaLayerRoomExcel>("RoguePersonaLayerRoom.json"))
{
    if (!data.RoguePersonaLayerRoomData.TryGetValue(row.LayerID, out var rooms))
        data.RoguePersonaLayerRoomData[row.LayerID] = rooms = [];
    if (rooms.All(room => room.RoomIndex != row.RoomIndex)) rooms.Add(row);
}

foreach (var rooms in data.RoguePersonaLayerRoomData.Values) rooms.Sort((a, b) => a.RoomIndex.CompareTo(b.RoomIndex));
foreach (var row in ReadExcel<RoguePersonaRoomPresetExcel>("RoguePersonaRoomPreset.json"))
    data.RoguePersonaRoomPresetData.TryAdd(row.PresetID, row);
foreach (var row in ReadExcel<RoguePersonaRoomCompTypeExcel>("RoguePersonaRoomCompType.json"))
    data.RoguePersonaRoomCompTypeData.TryAdd(row.CompositionType, row);
foreach (var row in ReadExcel<RoguePersonaStyleGiftExcel>("RoguePersonaStyleGift.json"))
    data.RoguePersonaStyleGiftData.TryAdd(row.GiftID, row);
foreach (var row in ReadExcel<RogueTournExpRewardExcel>("RogueTournExpReward.json"))
    data.RogueTournExpRewardData.TryAdd(row.GetId(), row);

var groupModes = ReadExcel<RogueTournBuffGroupExcel>("RogueTournBuffGroup.json")
    .ToDictionary(row => row.RogueBuffGroupID, row => row.TournMode);
var stampedGroups = 0;
foreach (var group in tournGroups)
    if (groupModes.TryGetValue(group.RogueBuffGroupID, out var mode) && mode != RogueTournModeEnum.Permanent)
    {
        group.TournMode = mode;
        stampedGroups++;
    }

var missingGraphs = new List<string>();
foreach (var npc in snapshot.RogueNPCData.Values.OrderBy(npc => npc.RogueNPCID))
{
    var entry = npc.RogueNpcConfig?.DialogueList.FirstOrDefault();
    if (entry == null || !entry.DialoguePath.Contains("RogueEventTourn", StringComparison.Ordinal)) continue;
    var path = Path.Combine(resourceRoot, entry.DialoguePath.Trim());
    if (!File.Exists(path))
    {
        missingGraphs.Add(entry.DialoguePath);
        continue;
    }

    var graph = RogueTournDialogueGraph.Parse(npc.RogueNPCID, entry.TalkNameID, File.ReadAllText(path));
    if (graph != null) data.RogueTournDialogueData[npc.RogueNPCID] = graph;
}

if (missingGraphs.Count > 0)
    throw new FileNotFoundException($"{missingGraphs.Count} Divergent Universe dialogue graphs are missing, e.g. {missingGraphs[0]}");

// Everything the cache carried before must round-trip unchanged once the additions are taken away again.
runtime.RogueTournRuntimeData = null;
var stamped = tournGroups.Where(group => group.TournMode != RogueTournModeEnum.Permanent).ToList();
var modes = stamped.ToDictionary(group => group, group => group.TournMode);
foreach (var group in stamped) group.TournMode = RogueTournModeEnum.Permanent;
if (!MemoryPackSerializer.Serialize(snapshot).AsSpan().SequenceEqual(baseline))
    throw new InvalidDataException("Existing cache content changed during the upgrade.");
foreach (var (group, mode) in modes) group.TournMode = mode;
runtime.RogueTournRuntimeData = data;

var raw = MemoryPackSerializer.Serialize(snapshot);
using (var output = File.Create(outputPath))
using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
    gzip.Write(raw, 0, raw.Length);

// Re-read the written file and verify the additions survived.
var reloaded = Load(outputPath).GridFightRuntimeData?.RogueTournRuntimeData
               ?? throw new InvalidDataException("Written cache lost the RogueTourn tables.");
Console.WriteLine($"RoguePersonaLayerRoom layers: {reloaded.RoguePersonaLayerRoomData.Count} " +
                  $"(rows {reloaded.RoguePersonaLayerRoomData.Values.Sum(rooms => rooms.Count)})");
Console.WriteLine($"RoguePersonaRoomPreset: {reloaded.RoguePersonaRoomPresetData.Count}");
Console.WriteLine($"RoguePersonaRoomCompType: {reloaded.RoguePersonaRoomCompTypeData.Count}");
Console.WriteLine($"RoguePersonaStyleGift: {reloaded.RoguePersonaStyleGiftData.Count}");
Console.WriteLine($"RogueTournExpReward: {reloaded.RogueTournExpRewardData.Count}");
Console.WriteLine($"RogueTournDialogueGraph: {reloaded.RogueTournDialogueData.Count}");
Console.WriteLine($"RogueTournBuffGroup TournMode stamped: {stampedGroups} of {tournGroups.Count}");
Console.WriteLine($"Wrote {outputPath}");
return 0;
