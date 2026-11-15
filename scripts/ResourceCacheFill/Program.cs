using System.Collections;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using March7thHoney.Data;
using March7thHoney.Internationalization;
using March7thHoney.Util;
using MemoryPack;

// Builds a resource cache from raw resources that are known to be incomplete (a client export that lacks
// tables or files) and completes it from an existing cache. Offline only: no database, no network, nothing
// written under Config/, and neither the base cache nor an existing output file is ever overwritten.
if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: ResourceCacheFill <resource directory> <base cache> <new output file> <new report directory>");
    return 2;
}
var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var resourceRoot = Path.GetFullPath(args[0]);
var basePath = Path.GetFullPath(args[1]);
var outputPath = Path.GetFullPath(args[2]);
var reportRoot = Path.GetFullPath(args[3]);
if (!Directory.Exists(resourceRoot)) throw new DirectoryNotFoundException(resourceRoot);
if (!File.Exists(basePath)) throw new FileNotFoundException("Base cache not found.", basePath);
if (File.Exists(outputPath) || outputPath.Equals(basePath, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Output must be a new file, different from the base cache.");

Directory.CreateDirectory(reportRoot);
Environment.CurrentDirectory = reportRoot;
var config = ConfigManager.Config;
config.Path.ResourcePath = resourceRoot;
config.Path.ConfigPath = Path.Combine(repository, "Config"); // operator config is read; no cache is saved here
config.Path.LanguagePath = Path.Combine(repository, "Config", "Languages");
config.Path.DatabasePath = Path.Combine(reportRoot, "UnusedDatabase");
config.Path.LogPath = reportRoot;
config.ServerOption.Language = "EN";
config.ServerOption.UseCache = false;
Logger.SetLogFile(new FileInfo(Path.Combine(reportRoot, "loader.log")));
// ResMgr/ResCache warnings only reach the debug log (see Logger.Log); they are read back from it.
var debugLog = new FileInfo(Path.Combine(reportRoot, "loader-debug.log"));
Logger.SetDebugLogFile(debugLog);
var errors = new ConcurrentQueue<string>();
PluginEventCommon.OnLogEntry += (module, level, message) =>
{
    if (level is LoggerLevel.ERROR or LoggerLevel.FATAL) errors.Enqueue($"[{module}/{level}] {message}");
};
I18NManager.LoadLanguage();

// ---------------------------------------------------------------------------- fresh load
ResourceCache.IsComplete = true;
string? fatal = null;
try { ResourceManager.LoadGameData(); }
catch (Exception exception) { fatal = exception.ToString(); }
var warnings = debugLog.Exists
    ? File.ReadAllLines(debugLog.FullName)
        .Where(line => line.Contains("][ResMgr][WARN] ", StringComparison.Ordinal) ||
                       line.Contains("][ResCache][WARN] ", StringComparison.Ordinal))
        .ToList()
    : [];
if (fatal != null || !errors.IsEmpty)
{
    // A table that is present but cannot be parsed is a loader defect; filling would hide it.
    Console.Error.WriteLine($"The resources do not load cleanly: {errors.Count} error line(s){(fatal != null ? " and an exception" : "")}.");
    foreach (var line in errors.Take(20)) Console.Error.WriteLine("  " + line.Split('\n')[0]);
    if (fatal != null) Console.Error.WriteLine(fatal);
    return 1;
}

GameDataSnapshot Clone(GameDataSnapshot snapshot) =>
    MemoryPackSerializer.Deserialize<GameDataSnapshot>(MemoryPackSerializer.Serialize(snapshot))
    ?? throw new InvalidDataException("Snapshot does not round-trip.");
GameDataSnapshot Load(string path)
{
    using var file = File.OpenRead(path);
    using var gzip = new GZipStream(file, CompressionMode.Decompress);
    using var data = new MemoryStream();
    gzip.CopyTo(data);
    return MemoryPackSerializer.Deserialize<GameDataSnapshot>(data.ToArray())
           ?? throw new InvalidDataException($"Empty cache: {path}");
}
// Same test as ResourceManager.ReportTruncatedMissionGraphs, on a snapshot.
List<int> TruncatedMissions(GameDataSnapshot snapshot) => snapshot.MainMissionData.Values
    .Where(main => main.MissionInfo.StartSubMissionList.Concat(main.MissionInfo.FinishSubMissionList)
        .Any(sub => !snapshot.SubMissionInfoData.ContainsKey(sub)))
    .Select(main => main.MainMissionID).Order().ToList();

// A deep copy: the live GameData collections stay as loaded.
var merged = Clone(GameData.CreateSnapshot());
var old = Load(basePath);
var truncatedFresh = TruncatedMissions(merged);

// ---------------------------------------------------------------------------- fill
// GridFight members always come from the resources alone: the season tables replace each other as a whole,
// and the base cache may predate the client's enum values. GridFightRuntimeDataSnapshot also holds unrelated
// tables (Rogue*, Daily*, MainMissionSchedule) that were parked there for MemoryPack's member cap; those are
// filled like any other.
static IEnumerable<Type> TypeTree(Type type) =>
    new[] { type }.Concat(type.GetGenericArguments().SelectMany(TypeTree));
static bool IsContainer(Type type) => (Nullable.GetUnderlyingType(type) ?? type).Name.EndsWith("Snapshot", StringComparison.Ordinal);
static bool IsGridFight(System.Reflection.PropertyInfo property)
{
    if (property.Name.StartsWith("GridFight", StringComparison.Ordinal)) return true;
    var types = TypeTree(property.PropertyType).ToList();
    var inGridFightGroup = property.DeclaringType == typeof(GridFightRuntimeDataSnapshot);
    // GridFightDynamicValue is also the value type of the client constant table, which is not a GridFight table.
    if (types.Any(type => type.Name.StartsWith("GridFight", StringComparison.Ordinal) &&
                          (inGridFightGroup || type.Name != "GridFightDynamicValue"))) return true;
    // The season membership lists in the GridFight group hold nothing but ids.
    return inGridFightGroup && types.All(type => type.IsPrimitive || type.IsGenericType || type == typeof(string));
}
// Phone message tables also come from the resources alone. The client resolves every contact, group and section
// the server names from its own tables (MessageModule._InitSection throws on an unknown section), so a base row the
// client dropped describes a chat it cannot show: the OSBETAWin4.7.51 fill carried 156 groups over from 4.6.52, and
// the six of them under 4.7.51 contacts aborted the client's whole GetNpcMessageGroupScRsp handling.
static bool IsMessage(System.Reflection.PropertyInfo property) =>
    property.DeclaringType == typeof(GameDataSnapshot) && property.Name is "MessageContactsConfigData"
        or "MessageGroupConfigData" or "MessageSectionConfigData" or "MessageItemConfigData";
static int Size(Type type, object? value) => value == null ? 0 : MemoryPackSerializer.Serialize(type, value).Length;
static bool IsDefault(Type type, object? value)
{
    if (value == null) return true;
    var underlying = Nullable.GetUnderlyingType(type) ?? type;
    if (underlying.IsValueType) return value.Equals(Activator.CreateInstance(underlying));
    if (underlying.GetConstructor(Type.EmptyTypes) == null) return false;
    return MemoryPackSerializer.Serialize(type, value).AsSpan()
        .SequenceEqual(MemoryPackSerializer.Serialize(type, Activator.CreateInstance(underlying)));
}

var rows = new List<(string Name, string Kind, string Fresh, string Base, string Filled, string Note)>();
int filledRows = 0, filledMembers = 0;
void Fill(object target, object source, string prefix)
{
    foreach (var property in target.GetType().GetProperties().OrderBy(p => p.Name, StringComparer.Ordinal))
    {
        var type = property.PropertyType;
        var name = prefix + property.Name;
        var freshValue = property.GetValue(target);
        var baseValue = property.GetValue(source);

        if (IsContainer(type))
        {
            // A group of members, not a table: its members are filled one by one.
            if (freshValue != null && baseValue != null) Fill(freshValue, baseValue, name + ".");
            else if (freshValue == null && baseValue != null)
            {
                property.SetValue(target, baseValue);
                filledMembers++;
                rows.Add((name, "group", "null", $"{Size(type, baseValue)} B", "1", "taken whole from the base"));
            }
            continue;
        }

        var gridFight = IsGridFight(property);
        var resourcesOnly = gridFight || IsMessage(property);
        var note = gridFight ? "resources only (GridFight)" : resourcesOnly ? "resources only (Message)" : "";

        if (freshValue is IDictionary freshMap && baseValue is IDictionary baseMap)
        {
            var before = freshMap.Count;
            var added = 0;
            if (!resourcesOnly)
                foreach (DictionaryEntry entry in baseMap)
                {
                    if (freshMap.Contains(entry.Key)) continue;
                    freshMap.Add(entry.Key, entry.Value);
                    added++;
                }
            var dropped = resourcesOnly ? baseMap.Keys.Cast<object>().Count(key => !freshMap.Contains(key)) : 0;
            if (dropped > 0) note += $"; {dropped} base row(s) not carried";
            if (added > 0) { filledRows += added; filledMembers++; }
            rows.Add((name, "rows", before.ToString(), baseMap.Count.ToString(), added.ToString(), note));
            continue;
        }

        if (freshValue is HashSet<int> freshSet && baseValue is HashSet<int> baseSet)
        {
            var before = freshSet.Count;
            if (!resourcesOnly) freshSet.UnionWith(baseSet);
            var added = freshSet.Count - before;
            if (added > 0) { filledRows += added; filledMembers++; }
            rows.Add((name, "set", before.ToString(), baseSet.Count.ToString(), added.ToString(), note));
            continue;
        }

        if (freshValue is IList freshList && baseValue is IList baseList)
        {
            // A list has no key to merge on: it is taken whole, and only when the resources gave nothing.
            var take = !resourcesOnly && freshList.Count == 0 && baseList.Count > 0;
            if (take) { property.SetValue(target, baseValue); filledRows += baseList.Count; filledMembers++; }
            rows.Add((name, "list", freshList.Count.ToString(), baseList.Count.ToString(),
                take ? baseList.Count.ToString() : "0", take ? "taken whole from the base" : note));
            continue;
        }

        // Single objects, scalars and collections absent on one side: taken from the base only when the
        // resources left them at their default.
        var takeObject = !resourcesOnly && IsDefault(type, freshValue) && !IsDefault(type, baseValue);
        var shownFresh = type.IsValueType ? $"{freshValue ?? "null"}" : $"{Size(type, freshValue)} B";
        var shownBase = type.IsValueType ? $"{baseValue ?? "null"}" : $"{Size(type, baseValue)} B";
        if (takeObject) { property.SetValue(target, baseValue); filledMembers++; }
        rows.Add((name, type.IsValueType ? "value" : "object", shownFresh, shownBase,
            takeObject ? "1" : "0", takeObject ? "taken from the base" : note));
    }
}
Fill(merged, old, "");
var truncatedMerged = TruncatedMissions(merged);

// ---------------------------------------------------------------------------- write and verify
var bytes = MemoryPackSerializer.Serialize(merged);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
using (var file = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write))
using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
    gzip.Write(bytes);
void SameCounts(object expected, object actual, string prefix)
{
    foreach (var property in expected.GetType().GetProperties())
    {
        var a = property.GetValue(expected);
        var b = property.GetValue(actual);
        if (IsContainer(property.PropertyType))
        {
            if ((a == null) != (b == null)) throw new InvalidDataException($"{prefix}{property.Name} is not preserved by the round trip.");
            if (a != null) SameCounts(a, b!, prefix + property.Name + ".");
        }
        else if (a is ICollection left && (b is not ICollection right || left.Count != right.Count))
            throw new InvalidDataException($"{prefix}{property.Name} does not keep its {left.Count} entries across the round trip.");
    }
}
SameCounts(merged, Load(outputPath), "");
var report = new StringBuilder();
report.Append($"resources : {resourceRoot}\nbase cache: {basePath}\noutput    : {outputPath}\n\n");
report.Append($"loader warnings ({warnings.Count}):\n");
foreach (var line in warnings) report.Append("  ").Append(line).Append('\n');
report.Append($"\nmain missions whose start or finish sub-missions are not registered: {truncatedFresh.Count} from the resources alone, {truncatedMerged.Count} after the fill\n");
if (truncatedMerged.Count > 0) report.Append("  still affected: ").Append(string.Join(", ", truncatedMerged)).Append('\n');
report.Append($"\nfilled {filledRows} row(s) in {filledMembers} member(s)\n\n");
report.Append($"{"member",-70} {"kind",-7} {"resources",12} {"base",12} {"filled",8}  note\n");
foreach (var row in rows.OrderByDescending(r => r.Filled != "0").ThenBy(r => r.Name, StringComparer.Ordinal))
    report.Append($"{row.Name,-70} {row.Kind,-7} {row.Fresh,12} {row.Base,12} {row.Filled,8}  {row.Note}\n");
File.WriteAllText(Path.Combine(reportRoot, "fill-report.txt"), report.ToString());

Console.WriteLine($"Loader warnings: {warnings.Count}; filled {filledRows} row(s) in {filledMembers} member(s).");
Console.WriteLine($"Main missions with unregistered sub-missions: {truncatedFresh.Count} -> {truncatedMerged.Count}.");
Console.WriteLine($"Output: {outputPath} ({new FileInfo(outputPath).Length} bytes). Report: {Path.Combine(reportRoot, "fill-report.txt")}");
return 0;
