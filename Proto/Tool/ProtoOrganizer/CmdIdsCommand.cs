using System.Text;

namespace March7thHoney.ProtoOrganizer;

internal sealed record CmdIdsOptions(string SchemaPath, string? AliasesPath, string OutPath, bool Check);

/// <summary>
/// Writes <c>KcpSharp/CmdIds.cs</c> from the schema: one constant per cmdId, named after the message
/// that carries it, followed by the hand-kept aliases under which server code refers to a command the
/// schema knows by another name.
/// </summary>
internal static class CmdIdsCommand
{
    public static int Run(CmdIdsOptions options)
    {
        List<TypeDecl> types;
        try
        {
            types = ProtoSchema.Parse(options.SchemaPath);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            Console.Error.WriteLine($"[cmdids] {ex.Message}");
            return 5;
        }

        var entries = new List<(string Name, int Value)>();
        var used = new Dictionary<string, int>(StringComparer.Ordinal);

        void Add(string rawName, int value)
        {
            if (value == 0 || rawName == "None") return;
            var name = rawName;
            if (used.TryGetValue(name, out var existing))
            {
                if (existing == value) return;
                name = $"{rawName}Cmd{value}";
                var serial = 2;
                while (used.ContainsKey(name)) name = $"{rawName}Cmd{value}_{serial++}";
            }
            used[name] = value;
            entries.Add((name, value));
        }

        // The command enums give the order: by module, then as the client lists the commands.
        var messages = CmdEnumNames.MessageNames(types);
        foreach (var type in types.Where(t => t.IsCmdEnum).OrderBy(t => t.Name, StringComparer.Ordinal))
        foreach (var (member, memberValue) in type.Members)
        {
            if (memberValue < 0) continue;
            var value = (int)memberValue;
            var name = CmdEnumNames.Constant(member, type.Name);
            if (messages.TryGetValue(value, out var message) && CmdEnumNames.IsReadable(message)) name = message;
            Add(name, value);
        }
        // A message whose cmdId no command enum lists still gets its constant.
        foreach (var (value, message) in messages.OrderBy(pair => pair.Key)) Add(message, value);
        var constants = entries.Count;

        var unresolved = new List<string>();
        if (options.AliasesPath != null)
        {
            foreach (var raw in File.ReadLines(options.AliasesPath))
            {
                var line = raw.Split("//", 2)[0].Split('#', 2)[0].Trim();
                if (line.Length == 0) continue;
                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2 || !used.TryGetValue(parts[1], out var value))
                {
                    unresolved.Add(line);
                    continue;
                }
                Add(parts[0], value);
            }
        }

        var builder = new StringBuilder();
        builder.Append("namespace March7thHoney.Kcp;\n\npublic class CmdIds\n{\n");
        builder.Append("    public const int None = 0;\n");
        foreach (var (name, value) in entries)
            builder.Append("    public const int ").Append(name).Append(" = ").Append(value).Append(";\n");
        builder.Append("}\n");
        var text = builder.ToString();

        foreach (var line in unresolved)
            Console.Error.WriteLine($"[cmdids] alias names no existing constant and was skipped: {line}");
        var summary = $"{constants} constants, {entries.Count - constants} aliases";

        if (options.Check)
        {
            var current = File.Exists(options.OutPath) ? File.ReadAllText(options.OutPath).Replace("\r\n", "\n") : null;
            if (current == text)
            {
                Console.WriteLine($"[cmdids] {options.OutPath} matches {options.SchemaPath}: {summary}");
                return 0;
            }
            Console.Error.WriteLine($"[cmdids] {options.OutPath} does not match {options.SchemaPath}; regenerate it with the cmdids command.");
            return 1;
        }

        var directory = Path.GetDirectoryName(options.OutPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(options.OutPath, text, new UTF8Encoding(false));
        Console.WriteLine($"[cmdids] wrote {options.OutPath}: {summary}");
        return 0;
    }
}
