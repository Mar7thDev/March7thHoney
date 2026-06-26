using March7thHoney.ProtoOrganizer;

// ProtoOrganizer — reorganizes the flat curated Proto/ProtoFile/*.proto into a version-aware,
// CmdType-grouped tree (see Proto plan). Definition source is the curated layer (byte-stable types);
// grouping is driven by the Cmd*Type enums + Raw/packetIds.txt.

var opts = Args.Parse(args);

Console.WriteLine($"[organizer] proto-file = {opts.ProtoFileDir}");
Console.WriteLine($"[organizer] packetIds  = {opts.PacketIdsPath}");

var rawDefs = CuratedParser.ParseDirectory(opts.ProtoFileDir);

// A type name may appear in more than one curated file; keep the first and report the rest so we
// never emit a duplicate definition (protoc rejects those).
var defs = new List<TypeDef>();
var seenNames = new HashSet<string>(StringComparer.Ordinal);
int dupCount = 0;
foreach (var d in rawDefs)
    if (seenNames.Add(d.Name)) defs.Add(d); else dupCount++;
if (dupCount > 0) Console.WriteLine($"[organizer] dropped {dupCount} duplicate-named curated defs (kept first)");

var packetIds = PacketIds.Load(opts.PacketIdsPath);

var rawDir = Path.GetDirectoryName(opts.PacketIdsPath) ?? opts.RawDir;
var translations = Translations.Load(Path.Combine(rawDir, "translations.txt"));
var masterPath = Path.Combine(rawDir, "StarRail.proto");
var masterEdges = File.Exists(masterPath)
    ? MasterParser.ParseEdges(masterPath)
    : new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
Console.WriteLine($"[organizer] translations = {translations.ObfToReal.Count}, master types = {masterEdges.Count}");

var graph = GraphContext.Build(defs, translations, masterEdges);
var gr = Grouper.Build(defs, packetIds, graph);

Console.WriteLine();
Console.WriteLine("=== Grouping report ===");
foreach (var line in gr.Report) Console.WriteLine(line);
Console.WriteLine();

if (opts.ReportOnly)
{
    Console.WriteLine("[organizer] --report: no files written.");
    return 0;
}

// Emit the grouped tree.
Console.WriteLine($"[organizer] emitting -> {opts.OutDir} (active={opts.Active})");
Emitter.Emit(gr, opts.OutDir, opts.Active);

// Copy the Raw dump alongside (skip the giant dump.cs by default).
if (Directory.Exists(opts.RawDir))
{
    var destRaw = Path.Combine(opts.OutDir, "Raw");
    Directory.CreateDirectory(destRaw);
    foreach (var f in Directory.EnumerateFiles(opts.RawDir))
    {
        var name = Path.GetFileName(f);
        if (!opts.IncludeDumpCs && name.Equals("dump.cs", StringComparison.OrdinalIgnoreCase)) continue;
        File.Copy(f, Path.Combine(destRaw, name), overwrite: true);
    }
    Console.WriteLine($"[organizer] copied Raw -> {destRaw}");
}

Console.WriteLine($"[organizer] wrote {gr.Buckets.Count} proto files.");

if (opts.Compile)
{
    if (!opts.Active)
    {
        Console.Error.WriteLine("[organizer] --compile requires --active (only the active version compiles to C#).");
        return 2;
    }
    return Compiler.Run(gr, opts);
}

return 0;
