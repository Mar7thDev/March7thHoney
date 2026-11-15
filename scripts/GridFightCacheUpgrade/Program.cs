using System.IO.Compression;
using March7thHoney.Data;
using March7thHoney.Data.Excel;
using MemoryPack;
using Newtonsoft.Json;

// Targeted offline migration. Never opens a player database or overwrites the source cache.
if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: GridFightCacheUpgrade <Resource.bin> <resources directory> <new output file>");
    return 2;
}
var input = Path.GetFullPath(args[0]);
var root = Path.GetFullPath(args[1]);
var output = Path.GetFullPath(args[2]);
if (input.Equals(output, StringComparison.OrdinalIgnoreCase) || File.Exists(output))
    throw new InvalidOperationException("Output must be a new file, different from the input cache.");
MemoryPackFormatterProvider.Register(new JsonObjectMemoryPackFormatter());
MemoryPackFormatterProvider.Register(new BigIntegerMemoryPackFormatter());

GameDataSnapshot Load(string path)
{
    using var file = File.OpenRead(path);
    using var gzip = new GZipStream(file, CompressionMode.Decompress);
    using var data = new MemoryStream();
    gzip.CopyTo(data);
    return MemoryPackSerializer.Deserialize<GameDataSnapshot>(data.ToArray())
        ?? throw new InvalidDataException("Empty cache.");
}

var snapshot = Load(input);
var added = new List<int>();
foreach (var table in new[] { "GridFightTraitMazebuffS2.json", "GridFightSpotLightMazebuff.json" })
{
    var path = Path.Combine(root, "ExcelOutputGameCore", table);
    var rows = JsonConvert.DeserializeObject<List<MazeBuffExcel>>(File.ReadAllText(path))
        ?? throw new InvalidDataException($"Empty table {table}.");
    foreach (var row in rows)
    {
        if (row.ID <= 0 || row.Lv <= 0 || row.InBattleBindingType != "StageAbilityBeforeCharacterBorn" ||
            string.IsNullOrWhiteSpace(row.InBattleBindingKey))
            throw new InvalidDataException($"Invalid buff definition in {table}: {row.ID}/{row.Lv}.");
        var key = row.GetId();
        if (snapshot.MazeBuffData.TryAdd(key, row)) added.Add(key);
    }
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
using (var file = new FileStream(output, FileMode.CreateNew))
using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
    gzip.Write(MemoryPackSerializer.Serialize(snapshot));

var original = Load(input);
var reloaded = Load(output);
foreach (var key in added)
{
    if (!reloaded.MazeBuffData.ContainsKey(key)) throw new InvalidDataException($"Missing buff {key} after round trip.");
    reloaded.MazeBuffData.Remove(key);
}
// Same preservation check as AbilityCacheUpgrade: floor collections are unordered across deserialization.
foreach (var property in typeof(GameDataSnapshot).GetProperties())
{
    if (property.Name is nameof(GameDataSnapshot.FloorInfoData) or nameof(GameDataSnapshot.CurrentChallengePeakGroupId)) continue;
    if (!MemoryPackSerializer.Serialize(property.PropertyType, property.GetValue(original)).AsSpan()
        .SequenceEqual(MemoryPackSerializer.Serialize(property.PropertyType, property.GetValue(reloaded))))
        throw new InvalidDataException($"Unexpected change to {property.Name}.");
}
if (original.CurrentChallengePeakGroupId != reloaded.CurrentChallengePeakGroupId ||
    !original.FloorInfoData.Keys.ToHashSet().SetEquals(reloaded.FloorInfoData.Keys) ||
    original.FloorInfoData.Any(pair => MemoryPackSerializer.Serialize(pair.Value).Length !=
        MemoryPackSerializer.Serialize(reloaded.FloorInfoData[pair.Key]).Length))
    throw new InvalidDataException("Unexpected floor or challenge-peak change.");
Console.WriteLine($"Added {added.Count} GridFight buff rows; other cache content preserved. Output: {output}");
return 0;
