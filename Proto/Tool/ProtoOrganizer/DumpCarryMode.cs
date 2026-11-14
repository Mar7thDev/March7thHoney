using System.Text;
using System.Text.RegularExpressions;

namespace March7thHoney.ProtoOrganizer;

/// <summary>
/// Carries the audited names of one version's translated master (<c>Raw/StarRail.proto</c>) onto a
/// newer Demiurge dump. Obfuscated type names, property names and enum member names are stable across
/// client builds while field numbers and cmdIds are not, so every name is moved by that identity and
/// never by number. The previous master must be exactly what its own dump yields under the derived
/// names; the mode refuses to continue otherwise, so a hand edit that changed structure cannot be
/// silently dropped. Types the previous master left out stay left out; anything new keeps the name the
/// dump gave it.
/// </summary>
public static class DumpCarryMode
{
    private sealed class Field
    {
        public required string Label { get; init; }
        public required string Type { get; init; }
        public required string Name { get; init; }
        public required int Number { get; init; }
        public required string Oneof { get; init; }
    }

    private sealed class TypeDecl
    {
        public required bool IsEnum { get; init; }
        public required string Name { get; init; }
        public string? Obf { get; set; }
        public int? CmdId { get; set; }
        public List<Field> Fields { get; } = new();
        public List<string> Oneofs { get; } = new();
        public List<(string Name, long Value)> Members { get; } = new();
        public string Ident => Obf ?? Name;
        public string Kind => IsEnum ? "enum" : "message";
    }

    /// <summary>What the previous master calls everything in its dump, keyed by obfuscated identity.</summary>
    private sealed class Names
    {
        public Dictionary<string, string> Types { get; } = new(StringComparer.Ordinal);
        public Dictionary<(string Type, string Field), string> Fields { get; } = new();
        public Dictionary<(string Type, string Oneof), string> Oneofs { get; } = new();
        public Dictionary<(string Type, string Member), string> Members { get; } = new();
        public HashSet<string> LeftOut { get; } = new(StringComparer.Ordinal);
    }

    private sealed class Renamed
    {
        public List<TypeDecl> Types { get; } = new();
        /// <summary>type token as the dump spells it -> name in the output.</summary>
        public Dictionary<string, string> ByDumpName { get; } = new(StringComparer.Ordinal);
        /// <summary>obfuscated identity -> name in the output.</summary>
        public Dictionary<string, string> ByIdent { get; } = new(StringComparer.Ordinal);
        /// <summary>(obfuscated identity, property as the dump spells it) -> field name in the output.</summary>
        public Dictionary<(string Type, string Field), string> Fields { get; } = new();
    }

    /// <summary>
    /// Hand-checked decisions, highest priority: <c>OBF = TypeName</c>, or <c>OBF.PROPERTY = field_name</c>
    /// for a property the client renamed, which identity alone cannot follow.
    /// </summary>
    private sealed class Overrides
    {
        public Dictionary<string, string> Types { get; } = new(StringComparer.Ordinal);
        public Dictionary<(string Type, string Field), string> Fields { get; } = new();
        public HashSet<string> Used { get; } = new(StringComparer.Ordinal);

        public static Overrides Load(string path)
        {
            var overrides = new Overrides();
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Split("//", 2)[0].Trim();
                if (line.Length == 0) continue;
                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                    throw new InvalidDataException($"{path}: expected 'OBF = Name' or 'OBF.PROPERTY = name', got: {line}");
                var key = parts[0].Split('.', 2);
                if (key.Length == 2) overrides.Fields[(key[0], key[1])] = parts[1];
                else overrides.Types[key[0]] = parts[1];
            }
            return overrides;
        }

        public IEnumerable<string> Unused() =>
            Types.Keys.Concat(Fields.Keys.Select(k => $"{k.Type}.{k.Field}")).Where(k => !Used.Contains(k));
    }

    private static readonly HashSet<string> Scalars = new(StringComparer.Ordinal)
    {
        "double","float","int32","int64","uint32","uint64","sint32","sint64",
        "fixed32","fixed64","sfixed32","sfixed64","bool","string","bytes",
    };

    private static readonly Regex HeadRx = new(@"^(message|enum)\s+(\w+)\s*\{\s*(//.*)?$", RegexOptions.Compiled);
    private static readonly Regex OneofRx = new(@"^oneof\s+(\w+)\s*\{$", RegexOptions.Compiled);
    private static readonly Regex FieldRx = new(
        @"^(?:(repeated|optional)\s+)?(map<\s*[\w.]+\s*,\s*[\w.]+\s*>|[\w.]+)\s+(\w+)\s*=\s*(\d+)\s*;\s*(?://.*)?$",
        RegexOptions.Compiled);
    private static readonly Regex MemberRx = new(@"^(\w+)\s*=\s*(-?\d+)\s*;\s*(?://.*)?$", RegexOptions.Compiled);
    private static readonly Regex MapRx = new(@"^map<\s*([\w.]+)\s*,\s*([\w.]+)\s*>$", RegexOptions.Compiled);
    private static readonly Regex ObfRx = new(@"^//\s*obf:\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CmdIdRx = new(@"^//\s*cmdid\s*[=:]\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static int Run(Args opts)
    {
        if (opts.CarryPrevDumpPath == null || opts.CarryPrevMasterPath == null || opts.CarryOutDir == null)
        {
            Console.Error.WriteLine("[carry] --carry-dump needs --carry-prev-dump, --carry-prev-master and --carry-out.");
            return 2;
        }

        List<TypeDecl> prevDump, prevMaster, dump;
        Overrides? overrides;
        try
        {
            prevDump = Parse(opts.CarryPrevDumpPath);
            prevMaster = Parse(opts.CarryPrevMasterPath);
            dump = Parse(opts.CarryDumpPath!);
            overrides = opts.CarryOverridesPath != null ? Overrides.Load(opts.CarryOverridesPath) : null;
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine($"[carry] {ex.Message}");
            return 5;
        }
        Console.WriteLine($"[carry] previous dump   = {Count(prevDump)}");
        Console.WriteLine($"[carry] previous master = {Count(prevMaster)}");
        Console.WriteLine($"[carry] new dump        = {Count(dump)}");

        var errors = new List<string>();
        var names = Derive(prevDump, prevMaster, errors);
        if (errors.Count == 0) CheckReproduces(prevDump, names, opts.CarryPrevMasterPath, errors);
        if (errors.Count > 0) return Fail("the previous master is not a pure renaming of its dump", errors);
        Console.WriteLine($"[carry] previous master reproduces from its dump: {names.Types.Count} types, {names.Fields.Count} fields, {names.Members.Count} enum members, {names.LeftOut.Count} types left out");

        var report = new List<string>();
        var result = Carry(prevDump, dump, names, overrides, report, errors);
        if (errors.Count > 0) return Fail("the new dump cannot take the previous names", errors);

        var label = opts.CarryLabel ?? "Demiurge static dump";
        Directory.CreateDirectory(opts.CarryOutDir);
        var utf8 = new UTF8Encoding(false);
        File.WriteAllText(Path.Combine(opts.CarryOutDir, "StarRail.proto"), Emit(result.Types, $"syntax = \"proto3\"; // {label}"), utf8);
        File.WriteAllText(Path.Combine(opts.CarryOutDir, "packetIds.txt"), EmitPacketIds(result.Types, label), utf8);
        File.WriteAllText(Path.Combine(opts.CarryOutDir, "translations.txt"), EmitTranslations(result.Types, label), utf8);
        Console.WriteLine($"[carry] wrote StarRail.proto, packetIds.txt, translations.txt -> {opts.CarryOutDir}");

        foreach (var line in report.Where(l => l.StartsWith('#'))) Console.WriteLine($"[carry] {line[1..].Trim()}");
        if (opts.CarryReportPath != null)
        {
            var directory = Path.GetDirectoryName(opts.CarryReportPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var head = $"// Carry report for {label}: the previous master's names moved onto this dump by obfuscated\n" +
                       "// identity, with what is new, what the client dropped and which messages changed shape.\n" +
                       "// Regenerated by ProtoOrganizer --carry-dump.\n";
            File.WriteAllText(opts.CarryReportPath, head + string.Join('\n', report) + "\n", utf8);
            Console.WriteLine($"[carry] report -> {opts.CarryReportPath}");
        }
        return 0;
    }

    private static string Count(List<TypeDecl> types) =>
        $"{types.Count(t => !t.IsEnum)} messages, {types.Count(t => t.IsEnum)} enums, {types.Count(t => t.CmdId != null)} cmdIds";

    private static int Fail(string what, List<string> errors)
    {
        Console.Error.WriteLine($"[carry] {what} ({errors.Count} problem(s)):");
        foreach (var error in errors.Take(40)) Console.Error.WriteLine($"  {error}");
        if (errors.Count > 40) Console.Error.WriteLine($"  ... {errors.Count - 40} more");
        return 5;
    }

    // ---------------------------------------------------------------------------------------- parsing

    /// <summary>Reads a Demiurge dump or a translated master; anything it does not understand is an error.</summary>
    private static List<TypeDecl> Parse(string path)
    {
        var types = new List<TypeDecl>();
        var pending = new List<string>();
        TypeDecl? current = null;
        var oneof = "";
        var lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (current == null)
            {
                if (line.Length == 0) { pending.Clear(); continue; }
                if (line.StartsWith("//", StringComparison.Ordinal)) { pending.Add(line); continue; }
                if (line.StartsWith("syntax", StringComparison.Ordinal)) continue;
                var head = HeadRx.Match(line);
                if (!head.Success) throw new InvalidDataException($"{path}:{i + 1}: unexpected line: {line}");
                current = new TypeDecl { IsEnum = head.Groups[1].Value == "enum", Name = head.Groups[2].Value };
                foreach (var comment in pending)
                {
                    var obf = ObfRx.Match(comment);
                    if (obf.Success) current.Obf = obf.Groups[1].Value;
                    var cmdId = CmdIdRx.Match(comment);
                    if (cmdId.Success) current.CmdId = int.Parse(cmdId.Groups[1].Value);
                }
                pending.Clear();
                oneof = "";
                continue;
            }

            if (line.Length == 0) continue;
            if (line == "}")
            {
                if (oneof.Length > 0) { oneof = ""; continue; }
                types.Add(current);
                current = null;
                continue;
            }

            if (current.IsEnum)
            {
                var member = MemberRx.Match(line);
                if (!member.Success) throw new InvalidDataException($"{path}:{i + 1}: unexpected enum line: {line}");
                current.Members.Add((member.Groups[1].Value, long.Parse(member.Groups[2].Value)));
                continue;
            }

            var block = OneofRx.Match(line);
            if (block.Success)
            {
                oneof = block.Groups[1].Value;
                current.Oneofs.Add(oneof);
                continue;
            }
            var field = FieldRx.Match(line);
            if (!field.Success) throw new InvalidDataException($"{path}:{i + 1}: unexpected message line: {line}");
            current.Fields.Add(new Field
            {
                Label = field.Groups[1].Value,
                Type = NormalizeType(field.Groups[2].Value),
                Name = field.Groups[3].Value,
                Number = int.Parse(field.Groups[4].Value),
                Oneof = oneof,
            });
        }
        if (current != null) throw new InvalidDataException($"{path}: unterminated {current.Kind} {current.Name}");
        return types;
    }

    private static string NormalizeType(string type)
    {
        var map = MapRx.Match(type);
        return map.Success ? $"map<{map.Groups[1].Value}, {map.Groups[2].Value}>" : type;
    }

    /// <summary>Renames the message or enum a field type refers to; scalars and map keys are left alone.</summary>
    private static string MapType(string type, Func<string, string> rename)
    {
        var map = MapRx.Match(type);
        if (map.Success) return $"map<{map.Groups[1].Value}, {Rename(map.Groups[2].Value)}>";
        return Rename(type);

        string Rename(string token) => Scalars.Contains(token) ? token : rename(token);
    }

    private static string Suffix(string member, string enumName) =>
        member.StartsWith(enumName + "_", StringComparison.Ordinal) ? member[(enumName.Length + 1)..] : member;

    private static string Labelled(Field field, string type, string name) =>
        $"{(field.Label.Length > 0 ? field.Label + " " : "")}{type} {name} = {field.Number}";

    // ------------------------------------------------------------------ names of the previous version

    private static Names Derive(List<TypeDecl> prevDump, List<TypeDecl> prevMaster, List<string> errors)
    {
        var names = new Names();
        var master = new Dictionary<string, TypeDecl>(StringComparer.Ordinal);
        foreach (var type in prevMaster)
            if (!master.TryAdd(type.Ident, type)) errors.Add($"previous master defines {type.Ident} twice");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in prevDump)
        {
            if (!seen.Add(type.Ident)) { errors.Add($"previous dump defines {type.Ident} twice"); continue; }
            if (!master.TryGetValue(type.Ident, out var named)) { names.LeftOut.Add(type.Ident); continue; }
            if (named.IsEnum != type.IsEnum) { errors.Add($"{type.Ident} is a {type.Kind} in the dump but a {named.Kind} in the master"); continue; }
            names.Types[type.Ident] = named.Name;

            if (type.IsEnum)
            {
                if (type.Members.Count != named.Members.Count)
                {
                    errors.Add($"enum {named.Name} ({type.Ident}): {type.Members.Count} members in the dump, {named.Members.Count} in the master");
                    continue;
                }
                for (var i = 0; i < type.Members.Count; i++)
                {
                    if (type.Members[i].Value != named.Members[i].Value)
                    {
                        errors.Add($"enum {named.Name} ({type.Ident}): member {i} is {type.Members[i].Value} in the dump, {named.Members[i].Value} in the master");
                        break;
                    }
                    names.Members[(type.Ident, Suffix(type.Members[i].Name, type.Name))] = named.Members[i].Name;
                }
                continue;
            }

            var byNumber = new Dictionary<int, Field>();
            foreach (var field in named.Fields) byNumber[field.Number] = field;
            if (type.Fields.Count != named.Fields.Count || byNumber.Count != named.Fields.Count || type.Fields.Any(f => !byNumber.ContainsKey(f.Number)))
            {
                errors.Add($"message {named.Name} ({type.Ident}): field numbers differ between the dump and the master");
                continue;
            }
            foreach (var field in type.Fields)
                names.Fields[(type.Ident, field.Name)] = byNumber[field.Number].Name;
            if (type.Oneofs.Count != named.Oneofs.Count) { errors.Add($"message {named.Name} ({type.Ident}): oneof count differs"); continue; }
            for (var i = 0; i < type.Oneofs.Count; i++)
                names.Oneofs[(type.Ident, type.Oneofs[i])] = named.Oneofs[i];
        }

        foreach (var type in prevMaster)
            if (!seen.Contains(type.Ident)) errors.Add($"previous master {type.Kind} {type.Name} has no counterpart in its dump");
        return names;
    }

    /// <summary>The previous master, rebuilt from its dump and the derived names, must be the file on disk.</summary>
    private static void CheckReproduces(List<TypeDecl> prevDump, Names names, string masterPath, List<string> errors)
    {
        var rebuilt = Rename(prevDump, names, ident => ident, null, errors);
        if (errors.Count > 0) return;
        var expected = Emit(rebuilt.Types, "").TrimEnd('\n').Split('\n');
        var actual = File.ReadAllText(masterPath).Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        var differences = 0;
        for (var i = 1; i < Math.Max(actual.Length, expected.Length); i++)   // line 0 is the syntax line with its label
        {
            var a = i < actual.Length ? actual[i] : "<end of file>";
            var e = i < expected.Length ? expected[i] : "<end of file>";
            if (a == e) continue;
            if (differences++ < 12) errors.Add($"line {i + 1}: master has '{a.Trim()}', its dump yields '{e.Trim()}'");
        }
        if (differences > 12) errors.Add($"... {differences} differing lines in total");
    }

    // ------------------------------------------------------------------------- carrying onto the dump

    private static Renamed Carry(List<TypeDecl> prevDump, List<TypeDecl> dump, Names names, Overrides? overrides, List<string> report, List<string> errors)
    {
        var prev = prevDump.ToDictionary(t => t.Ident, StringComparer.Ordinal);
        var prevByName = prevDump.ToDictionary(t => t.Name, StringComparer.Ordinal);
        var now = new Dictionary<string, TypeDecl>(StringComparer.Ordinal);
        foreach (var type in dump)
            if (!now.TryAdd(type.Ident, type)) errors.Add($"new dump defines {type.Ident} twice");

        // An enum whose obfuscated name changed while every member kept its name and value is the same enum.
        var predecessor = new Dictionary<string, string>(StringComparer.Ordinal);   // new ident -> previous ident
        var successor = new Dictionary<string, string>(StringComparer.Ordinal);     // previous ident -> new ident
        string Signature(TypeDecl e) => string.Join(',', e.Members.Select(m => $"{Suffix(m.Name, e.Name)}={m.Value}"));
        var goneEnums = prevDump.Where(t => t.IsEnum && !now.ContainsKey(t.Ident) && !names.LeftOut.Contains(t.Ident)).ToLookup(Signature);
        var newEnums = dump.Where(t => t.IsEnum && !prev.ContainsKey(t.Ident)).ToLookup(Signature);
        foreach (var group in newEnums)
        {
            if (group.Count() != 1 || goneEnums[group.Key].Count() != 1 || group.Single().Members.Count == 0) continue;
            predecessor[group.Single().Ident] = goneEnums[group.Key].Single().Ident;
            successor[goneEnums[group.Key].Single().Ident] = group.Single().Ident;
        }

        string? Previous(string ident) => prev.ContainsKey(ident) ? ident : predecessor.GetValueOrDefault(ident);

        var result = Rename(dump, names, Previous, overrides, errors);
        if (overrides != null)
            foreach (var key in overrides.Unused()) errors.Add($"override {key} matches nothing in the new dump");
        if (errors.Count > 0) return result;

        // The same things under the names a reader of the master knows.
        string OldType(string token) =>
            prevByName.TryGetValue(token, out var t) ? names.Types.GetValueOrDefault(t.Ident, token) : token;
        string NewType(string token) => result.ByDumpName.GetValueOrDefault(token, token);
        string Described(string name, string ident) => name == ident ? name : $"{name} ({ident})";

        var added = dump.Where(t => Previous(t.Ident) == null).ToList();
        var leftOut = dump.Count(t => Previous(t.Ident) is { } p && names.LeftOut.Contains(p));
        var removed = prevDump.Where(t => !now.ContainsKey(t.Ident) && !successor.ContainsKey(t.Ident) && !names.LeftOut.Contains(t.Ident)).ToList();
        report.Add($"# types: {prevDump.Count} in the previous dump, {dump.Count} in this one; {dump.Count - added.Count - leftOut} carried, {added.Count} new, {removed.Count} removed, {leftOut} left out as before");

        int sameCmd = 0, movedCmd = 0;
        foreach (var type in dump)
        {
            if (type.CmdId is not { } id || !prev.TryGetValue(type.Ident, out var old) || old.CmdId is not { } oldId) continue;
            if (id == oldId) sameCmd++; else movedCmd++;
        }
        report.Add($"# cmdIds: {prevDump.Count(t => t.CmdId != null)} before, {dump.Count(t => t.CmdId != null)} now; {movedCmd} moved, {sameCmd} unchanged, {added.Count(t => t.CmdId != null)} on new messages");

        int renumbered = 0, carriedFields = 0, reshaped = 0;
        var shape = new List<string>();
        foreach (var type in dump.Where(t => !t.IsEnum))
        {
            if (!prev.TryGetValue(type.Ident, out var old) || !result.ByIdent.TryGetValue(type.Ident, out var outName)) continue;
            var before = old.Fields.ToDictionary(f => f.Name, StringComparer.Ordinal);
            var after = type.Fields.ToDictionary(f => f.Name, StringComparer.Ordinal);
            carriedFields += after.Keys.Count(before.ContainsKey);
            if (type.Fields.Any(f => before.TryGetValue(f.Name, out var b) && b.Number != f.Number)) renumbered++;

            string FieldName(string dumpName) => names.Fields.GetValueOrDefault((type.Ident, dumpName), dumpName);
            var changes = new List<string>();
            foreach (var f in old.Fields.Where(f => !after.ContainsKey(f.Name)))
                changes.Add($"    - {Labelled(f, MapType(f.Type, OldType), FieldName(f.Name))}");
            foreach (var f in type.Fields.Where(f => !before.ContainsKey(f.Name)))
            {
                var given = result.Fields[(type.Ident, f.Name)];
                changes.Add($"    + {Labelled(f, MapType(f.Type, NewType), given == f.Name ? given : $"{given} ({f.Name}, by override)")}");
            }
            foreach (var f in type.Fields)
            {
                if (!before.TryGetValue(f.Name, out var b)) continue;
                var expected = MapType(b.Type, t => successor.GetValueOrDefault(t, t));
                if (expected == f.Type && b.Label == f.Label && (b.Oneof.Length > 0) == (f.Oneof.Length > 0)) continue;
                changes.Add($"    ~ {FieldName(f.Name)}: {(b.Label.Length > 0 ? b.Label + " " : "")}{MapType(b.Type, OldType)} -> {(f.Label.Length > 0 ? f.Label + " " : "")}{MapType(f.Type, NewType)}");
            }
            if (changes.Count == 0) continue;
            reshaped++;
            shape.Add($"  changed message {Described(outName, type.Ident)}");
            shape.AddRange(changes);
        }
        report.Add($"# fields: {carriedFields} carried by property name; {renumbered} messages renumbered, {reshaped} changed shape");

        foreach (var type in removed)
            report.Add($"  removed {type.Kind} {Described(names.Types[type.Ident], type.Ident)}{(type.CmdId is { } id ? $" (was cmdId {id})" : "")}");
        foreach (var (old, current) in successor.OrderBy(p => p.Key, StringComparer.Ordinal))
            report.Add($"  enum {Described(names.Types[old], old)} is now {Described(result.ByIdent[current], current)}: members and values identical");
        foreach (var type in added)
        {
            var detail = type.IsEnum
                ? $"{type.Members.Count} members"
                : $"{type.Fields.Count} fields{(type.CmdId is { } id ? $", cmdId {id}" : "")}";
            report.Add($"  new {type.Kind} {Described(result.ByIdent[type.Ident], type.Ident)}: {detail}");
        }
        report.AddRange(shape);

        foreach (var type in dump.Where(t => t.IsEnum))
        {
            if (!prev.TryGetValue(type.Ident, out var old) || !result.ByIdent.TryGetValue(type.Ident, out var outName)) continue;
            var before = old.Members.Select(m => Suffix(m.Name, old.Name)).ToHashSet(StringComparer.Ordinal);
            var after = type.Members.Select(m => Suffix(m.Name, type.Name)).ToHashSet(StringComparer.Ordinal);
            if (before.SetEquals(after)) continue;
            string Member(string suffix) => names.Members.GetValueOrDefault((type.Ident, suffix), suffix);
            report.Add($"  changed enum {Described(outName, type.Ident)}");
            foreach (var m in old.Members.Where(m => !after.Contains(Suffix(m.Name, old.Name))))
                report.Add($"    - {Member(Suffix(m.Name, old.Name))} = {m.Value}");
            foreach (var m in type.Members.Where(m => !before.Contains(Suffix(m.Name, type.Name))))
                report.Add($"    + {Suffix(m.Name, type.Name)} = {m.Value}");
        }

        foreach (var type in dump)
            if (type.Obf != null && result.ByIdent.GetValueOrDefault(type.Ident) == type.Ident)
                report.Add($"  kept {type.Ident} although the dump recovers {type.Name} for it, as the previous master does");
        return result;
    }

    /// <summary>
    /// Renames a dump with the derived names. <paramref name="previous"/> gives the identity a type had
    /// in the previous version, or null for a type that is new; the identity function with the previous
    /// dump itself rebuilds the previous master.
    /// </summary>
    private static Renamed Rename(List<TypeDecl> dump, Names names, Func<string, string?> previous, Overrides? overrides, List<string> errors)
    {
        var result = new Renamed();
        var identity = new Dictionary<string, string?>(StringComparer.Ordinal);
        var kept = new List<TypeDecl>();
        var taken = new HashSet<string>(StringComparer.Ordinal);

        // Carried names first, so a new type can never take a name the server already uses.
        foreach (var type in dump)
        {
            var old = identity[type.Ident] = previous(type.Ident);
            if (old != null && names.LeftOut.Contains(old)) continue;
            kept.Add(type);
            string? name = null;
            if (overrides != null && overrides.Types.TryGetValue(type.Ident, out var forced))
            {
                name = forced;
                overrides.Used.Add(type.Ident);
            }
            else if (old != null && names.Types.TryGetValue(old, out var carried))
            {
                name = carried == old && old != type.Ident ? type.Ident : carried;   // never named: follow the new obfuscated name
            }
            if (name == null) continue;
            result.ByIdent[type.Ident] = name;
            if (!taken.Add(name)) errors.Add($"carried name {name} is used by two types");
        }
        foreach (var type in kept)
        {
            if (result.ByIdent.ContainsKey(type.Ident)) continue;
            var name = taken.Add(type.Name) ? type.Name : type.Ident;
            if (name != type.Name && !taken.Add(name)) errors.Add($"new type {type.Ident} has no free name");
            result.ByIdent[type.Ident] = name;
        }

        foreach (var type in kept) result.ByDumpName[type.Name] = result.ByIdent[type.Ident];
        var dropped = dump.Where(t => !result.ByIdent.ContainsKey(t.Ident)).Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var type in kept)
        {
            var old = identity[type.Ident];
            var name = result.ByIdent[type.Ident];
            var renamed = new TypeDecl { IsEnum = type.IsEnum, Name = name, Obf = name != type.Ident ? type.Ident : null, CmdId = type.CmdId };
            if (type.IsEnum)
            {
                var members = new HashSet<string>(StringComparer.Ordinal);
                foreach (var (member, value) in type.Members)
                {
                    // A carried member keeps its master spelling (not every enum prefixes its members);
                    // only an enum that follows a new obfuscated name has its prefix follow too.
                    var suffix = Suffix(member, type.Name);
                    var full = $"{name}_{suffix}";
                    if (old != null && names.Members.TryGetValue((old, suffix), out var carried))
                    {
                        var before = names.Types[old];
                        full = before != name && carried.StartsWith(before + "_", StringComparison.Ordinal)
                            ? name + carried[before.Length..]
                            : carried;
                    }
                    if (!members.Add(full)) errors.Add($"enum {name}: member {full} appears twice");
                    renamed.Members.Add((full, value));
                }
            }
            else
            {
                var fields = new HashSet<string>(StringComparer.Ordinal);
                foreach (var field in type.Fields)
                {
                    var fieldName = old != null && names.Fields.TryGetValue((old, field.Name), out var carried) ? carried : field.Name;
                    if (overrides != null && overrides.Fields.TryGetValue((type.Ident, field.Name), out var forced))
                    {
                        fieldName = forced;
                        overrides.Used.Add($"{type.Ident}.{field.Name}");
                    }
                    result.Fields[(type.Ident, field.Name)] = fieldName;
                    if (!fields.Add(fieldName)) errors.Add($"message {name}: field name {fieldName} appears twice");
                    var oneof = field.Oneof.Length > 0 && old != null && names.Oneofs.TryGetValue((old, field.Oneof), out var carriedOneof)
                        ? carriedOneof
                        : field.Oneof;
                    renamed.Fields.Add(new Field
                    {
                        Label = field.Label,
                        Type = MapType(field.Type, token =>
                        {
                            if (dropped.Contains(token)) errors.Add($"message {name}.{fieldName} refers to {token}, which the master leaves out");
                            return result.ByDumpName.GetValueOrDefault(token, token);
                        }),
                        Name = fieldName,
                        Number = field.Number,
                        Oneof = oneof,
                    });
                }
            }
            result.Types.Add(renamed);
        }
        return result;
    }

    // ---------------------------------------------------------------------------------------- emission

    private static string Emit(List<TypeDecl> types, string firstLine)
    {
        var sb = new StringBuilder();
        sb.Append(firstLine).Append('\n');
        foreach (var type in types.OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            sb.Append('\n');
            if (type.Obf != null) sb.Append("// Obf: ").Append(type.Obf).Append('\n');
            if (type.CmdId is { } cmdId) sb.Append("// CmdID: ").Append(cmdId).Append('\n');
            sb.Append(type.Kind).Append(' ').Append(type.Name).Append(" {\n");
            if (type.IsEnum)
            {
                foreach (var (member, value) in type.Members)
                    sb.Append('\t').Append(member).Append(" = ").Append(value).Append(";\n");
            }
            else
            {
                var oneof = "";
                foreach (var field in type.Fields)
                {
                    if (field.Oneof != oneof)
                    {
                        if (oneof.Length > 0) sb.Append("\t}\n");
                        if (field.Oneof.Length > 0) sb.Append("\toneof ").Append(field.Oneof).Append(" {\n");
                        oneof = field.Oneof;
                    }
                    sb.Append(oneof.Length > 0 ? "\t\t" : "\t");
                    if (field.Label.Length > 0) sb.Append(field.Label).Append(' ');
                    sb.Append(field.Type).Append(' ').Append(field.Name).Append(" = ").Append(field.Number).Append(";\n");
                }
                if (oneof.Length > 0) sb.Append("\t}\n");
            }
            sb.Append("}\n");
        }
        return sb.ToString();
    }

    private static string EmitPacketIds(List<TypeDecl> types, string label)
    {
        var sb = new StringBuilder();
        sb.Append("// cmdid = message (").Append(label).Append(", packet registry); real name where known, // obf: original\n");
        foreach (var type in types.Where(t => t.CmdId != null).OrderBy(t => t.CmdId))
        {
            sb.Append(type.CmdId).Append(" = ").Append(type.Name);
            if (type.Obf != null) sb.Append("  // obf: ").Append(type.Obf);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    private static string EmitTranslations(List<TypeDecl> types, string label)
    {
        var sb = new StringBuilder();
        sb.Append("// obfClass = name (").Append(label).Append("; names carried by obfuscated identity, the dump's own for the rest)\n");
        foreach (var type in types.Where(t => t.Obf != null).OrderBy(t => t.Obf, StringComparer.Ordinal))
            sb.Append(type.Obf).Append(" = ").Append(type.Name).Append('\n');
        return sb.ToString();
    }
}
