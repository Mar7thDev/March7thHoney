using System.Text;

namespace March7thHoney.ProtoOrganizer;

internal sealed record CarryOptions(
    string DumpPath,
    string PrevDumpPath,
    string SchemaPath,
    string OutPath,
    string? OverridesPath,
    string? ReportPath,
    string? Label);

/// <summary>
/// Carries the audited names of the current schema (<c>Proto/StarRail.proto</c>) onto a newer Demiurge
/// dump. Obfuscated type names, property names and enum member names are stable across client builds
/// while field numbers and cmdIds are not, so every name is moved by that identity and never by number.
/// The current schema must be exactly what its own dump yields under the derived names; the command
/// refuses to continue otherwise, so a hand edit that changed structure cannot be silently dropped.
/// Types the current schema left out stay left out; anything new keeps the name the dump gave it.
/// </summary>
internal static class CarryCommand
{
    /// <summary>What the previous schema calls everything in its dump, keyed by obfuscated identity.</summary>
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

    public static int Run(CarryOptions options)
    {
        List<TypeDecl> prevDump, prevSchema, dump;
        Overrides? overrides;
        try
        {
            prevDump = ProtoSchema.Parse(options.PrevDumpPath);
            prevSchema = ProtoSchema.Parse(options.SchemaPath);
            dump = ProtoSchema.Parse(options.DumpPath);
            overrides = options.OverridesPath != null ? Overrides.Load(options.OverridesPath) : null;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            Console.Error.WriteLine($"[carry] {ex.Message}");
            return 5;
        }
        Console.WriteLine($"[carry] previous dump   = {Count(prevDump)}");
        Console.WriteLine($"[carry] previous schema = {Count(prevSchema)}");
        Console.WriteLine($"[carry] new dump        = {Count(dump)}");

        var errors = new List<string>();
        var names = Derive(prevDump, prevSchema, errors);
        if (errors.Count == 0) CheckReproduces(prevDump, names, options.SchemaPath, errors);
        if (errors.Count > 0) return Fail("the previous schema is not a pure renaming of its dump", errors);
        Console.WriteLine($"[carry] previous schema reproduces from its dump: {names.Types.Count} types, {names.Fields.Count} fields, {names.Members.Count} enum members, {names.LeftOut.Count} types left out");

        var report = new List<string>();
        var result = Carry(prevDump, dump, names, overrides, report, errors);
        if (errors.Count > 0) return Fail("the new dump cannot take the previous names", errors);
        var commands = CmdEnumNames.Apply(result.Types, dump.ToDictionary(t => t.Ident, StringComparer.Ordinal));

        var label = options.Label ?? "Demiurge static dump";
        var utf8 = new UTF8Encoding(false);
        CreateDirectoryFor(options.OutPath);
        File.WriteAllText(options.OutPath, ProtoSchema.Emit(result.Types, $"syntax = \"proto3\"; // {label}"), utf8);
        Console.WriteLine($"[carry] wrote {options.OutPath}: {Count(result.Types)}; {commands} command enum members named after their message");

        foreach (var line in report.Where(l => l.StartsWith('#'))) Console.WriteLine($"[carry] {line[1..].Trim()}");
        if (options.ReportPath != null)
        {
            CreateDirectoryFor(options.ReportPath);
            var head = $"// Carry report for {label}: the previous schema's names moved onto this dump by obfuscated\n" +
                       "// identity, with what is new, what the client dropped and which messages changed shape.\n" +
                       "// Written by ProtoOrganizer carry.\n";
            File.WriteAllText(options.ReportPath, head + string.Join('\n', report) + "\n", utf8);
            Console.WriteLine($"[carry] report -> {options.ReportPath}");
        }
        return 0;
    }

    private static void CreateDirectoryFor(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
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

    private static string Suffix(string member, string enumName) => ProtoSchema.MemberSuffix(member, enumName);

    private static string Labelled(Field field, string type, string name) =>
        $"{(field.Label.Length > 0 ? field.Label + " " : "")}{type} {name} = {field.Number}";

    // ------------------------------------------------------------------ names of the previous version

    private static Names Derive(List<TypeDecl> prevDump, List<TypeDecl> prevSchema, List<string> errors)
    {
        var names = new Names();
        var schema = new Dictionary<string, TypeDecl>(StringComparer.Ordinal);
        foreach (var type in prevSchema)
            if (!schema.TryAdd(type.Ident, type)) errors.Add($"previous schema defines {type.Ident} twice");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in prevDump)
        {
            if (!seen.Add(type.Ident)) { errors.Add($"previous dump defines {type.Ident} twice"); continue; }
            if (!schema.TryGetValue(type.Ident, out var named)) { names.LeftOut.Add(type.Ident); continue; }
            if (named.IsEnum != type.IsEnum) { errors.Add($"{type.Ident} is a {type.Kind} in the dump but a {named.Kind} in the schema"); continue; }
            names.Types[type.Ident] = named.Name;

            if (type.IsEnum)
            {
                if (type.Members.Count != named.Members.Count)
                {
                    errors.Add($"enum {named.Name} ({type.Ident}): {type.Members.Count} members in the dump, {named.Members.Count} in the schema");
                    continue;
                }
                for (var i = 0; i < type.Members.Count; i++)
                {
                    if (type.Members[i].Value != named.Members[i].Value)
                    {
                        errors.Add($"enum {named.Name} ({type.Ident}): member {i} is {type.Members[i].Value} in the dump, {named.Members[i].Value} in the schema");
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
                errors.Add($"message {named.Name} ({type.Ident}): field numbers differ between the dump and the schema");
                continue;
            }
            foreach (var field in type.Fields)
                names.Fields[(type.Ident, field.Name)] = byNumber[field.Number].Name;
            if (type.Oneofs.Count != named.Oneofs.Count) { errors.Add($"message {named.Name} ({type.Ident}): oneof count differs"); continue; }
            for (var i = 0; i < type.Oneofs.Count; i++)
                names.Oneofs[(type.Ident, type.Oneofs[i])] = named.Oneofs[i];
        }

        foreach (var type in prevSchema)
            if (!seen.Contains(type.Ident)) errors.Add($"previous schema {type.Kind} {type.Name} has no counterpart in its dump");
        return names;
    }

    /// <summary>The previous schema, rebuilt from its dump and the derived names, must be the file on disk.</summary>
    private static void CheckReproduces(List<TypeDecl> prevDump, Names names, string schemaPath, List<string> errors)
    {
        var rebuilt = Rename(prevDump, names, ident => ident, null, errors);
        if (errors.Count > 0) return;
        // Only the definitions are compared: the header carries the label of whichever run wrote the file.
        var expected = ProtoSchema.Definitions(ProtoSchema.Emit(rebuilt.Types, ""));
        var actual = ProtoSchema.Definitions(File.ReadAllText(schemaPath));
        var differences = 0;
        for (var i = 0; i < Math.Max(actual.Length, expected.Length); i++)
        {
            var a = i < actual.Length ? actual[i] : "<end of file>";
            var e = i < expected.Length ? expected[i] : "<end of file>";
            if (a == e) continue;
            if (differences++ < 12) errors.Add($"definition line {i + 1}: schema has '{a.Trim()}', its dump yields '{e.Trim()}'");
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
        if (errors.Count > 0) return new Renamed();

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

        // The same things under the names a reader of the schema knows.
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
                changes.Add($"    - {Labelled(f, ProtoSchema.MapType(f.Type, OldType), FieldName(f.Name))}");
            foreach (var f in type.Fields.Where(f => !before.ContainsKey(f.Name)))
            {
                var given = result.Fields[(type.Ident, f.Name)];
                changes.Add($"    + {Labelled(f, ProtoSchema.MapType(f.Type, NewType), given == f.Name ? given : $"{given} ({f.Name}, by override)")}");
            }
            foreach (var f in type.Fields)
            {
                if (!before.TryGetValue(f.Name, out var b)) continue;
                var expected = ProtoSchema.MapType(b.Type, t => successor.GetValueOrDefault(t, t));
                if (expected == f.Type && b.Label == f.Label && (b.Oneof.Length > 0) == (f.Oneof.Length > 0)) continue;
                changes.Add($"    ~ {FieldName(f.Name)}: {(b.Label.Length > 0 ? b.Label + " " : "")}{ProtoSchema.MapType(b.Type, OldType)} -> {(f.Label.Length > 0 ? f.Label + " " : "")}{ProtoSchema.MapType(f.Type, NewType)}");
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
                report.Add($"  kept {type.Ident} although the dump recovers {type.Name} for it, as the previous schema does");
        return result;
    }

    /// <summary>
    /// Renames a dump with the derived names. <paramref name="previous"/> gives the identity a type had
    /// in the previous version, or null for a type that is new; the identity function with the previous
    /// dump itself rebuilds the previous schema.
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
                    // A carried member keeps its previous spelling (not every enum prefixes its members);
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
                        Type = ProtoSchema.MapType(field.Type, token =>
                        {
                            if (dropped.Contains(token)) errors.Add($"message {name}.{fieldName} refers to {token}, which the schema leaves out");
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
}
