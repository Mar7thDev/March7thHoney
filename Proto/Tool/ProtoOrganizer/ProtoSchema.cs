using System.Text;
using System.Text.RegularExpressions;

namespace March7thHoney.ProtoOrganizer;

internal sealed class Field
{
    public required string Label { get; init; }
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required int Number { get; init; }
    public required string Oneof { get; init; }
}

internal sealed class TypeDecl
{
    private static readonly Regex CmdEnumRx = new(@"^Cmd[A-Za-z0-9]*Type$", RegexOptions.Compiled);

    public required bool IsEnum { get; init; }
    public required string Name { get; init; }
    public string? Obf { get; set; }
    public int? CmdId { get; set; }
    public List<Field> Fields { get; } = new();
    public List<string> Oneofs { get; } = new();
    public List<(string Name, long Value)> Members { get; } = new();
    public string Ident => Obf ?? Name;
    public string Kind => IsEnum ? "enum" : "message";

    /// <summary>A <c>Cmd&lt;Module&gt;Type</c> enum lists the cmdIds of one module; nothing refers to it.</summary>
    public bool IsCmdEnum => IsEnum && CmdEnumRx.IsMatch(Name);
}

/// <summary>
/// Reads and writes the flat schema form shared by a Demiurge dump and <c>Proto/StarRail.proto</c>:
/// top-level messages and enums only, each optionally preceded by <c>// Obf:</c> and <c>// CmdID:</c>
/// comments that record its obfuscated identity and the command it carries.
/// </summary>
internal static class ProtoSchema
{
    /// <summary>The generated C# lands in the namespace the server code uses.</summary>
    private const string NamespaceOption = "option csharp_namespace = \"March7thHoney.Proto\";";

    private static readonly Regex HeadRx = new(@"^(message|enum)\s+(\w+)\s*\{\s*(//.*)?$", RegexOptions.Compiled);
    private static readonly Regex OneofRx = new(@"^oneof\s+(\w+)\s*\{$", RegexOptions.Compiled);
    private static readonly Regex FieldRx = new(
        @"^(?:(repeated|optional)\s+)?(map<\s*[\w.]+\s*,\s*[\w.]+\s*>|[\w.]+)\s+(\w+)\s*=\s*(\d+)\s*;\s*(?://.*)?$",
        RegexOptions.Compiled);
    private static readonly Regex MemberRx = new(@"^(\w+)\s*=\s*(-?\d+)\s*;\s*(?://.*)?$", RegexOptions.Compiled);
    private static readonly Regex MapRx = new(@"^map<\s*([\w.]+)\s*,\s*([\w.]+)\s*>$", RegexOptions.Compiled);
    private static readonly Regex ObfRx = new(@"^//\s*obf:\s*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CmdIdRx = new(@"^//\s*cmdid\s*[=:]\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Anything the parser does not understand is an error, so no definition is dropped silently.</summary>
    public static List<TypeDecl> Parse(string path)
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
                if (IsHeader(line)) continue;
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

    /// <summary>Writes the types sorted by name under the given first line, normally the syntax line with its label.</summary>
    public static string Emit(IEnumerable<TypeDecl> types, string firstLine)
    {
        var sb = new StringBuilder();
        sb.Append(firstLine).Append("\n\n").Append(NamespaceOption).Append('\n');
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

    /// <summary>The definitions of a schema text without its header, for comparing two files type by type.</summary>
    public static string[] Definitions(string text)
    {
        var lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        var start = 0;
        while (start < lines.Length && (lines[start].Trim().Length == 0 || IsHeader(lines[start].Trim()))) start++;
        return lines[start..];
    }

    /// <summary>The part of a member name after its enum's prefix; not every enum prefixes its members.</summary>
    public static string MemberSuffix(string member, string enumName) =>
        member.StartsWith(enumName + "_", StringComparison.Ordinal) ? member[(enumName.Length + 1)..] : member;

    /// <summary>Renames the message or enum a field type refers to; scalars and map keys are left alone.</summary>
    public static string MapType(string type, Func<string, string> rename)
    {
        var map = MapRx.Match(type);
        if (map.Success) return $"map<{map.Groups[1].Value}, {Rename(map.Groups[2].Value)}>";
        return Rename(type);

        string Rename(string token) => Scalars.Contains(token) ? token : rename(token);
    }

    private static readonly HashSet<string> Scalars = new(StringComparer.Ordinal)
    {
        "double","float","int32","int64","uint32","uint64","sint32","sint64",
        "fixed32","fixed64","sfixed32","sfixed64","bool","string","bytes",
    };

    private static bool IsHeader(string line) =>
        line.StartsWith("syntax", StringComparison.Ordinal) || line.StartsWith("option", StringComparison.Ordinal);

    private static string NormalizeType(string type)
    {
        var map = MapRx.Match(type);
        return map.Success ? $"map<{map.Groups[1].Value}, {map.Groups[2].Value}>" : type;
    }
}
