namespace March7thHoney.ProtoOrganizer;

/// <summary>
/// Names the members of the <c>Cmd&lt;Module&gt;Type</c> enums. Every member value is a cmdId, so a member
/// takes the name of the message that carries that cmdId and otherwise keeps the suffix the dump gave it.
/// The names are derived from the dump on every run and never carried from the previous version, so a
/// member follows its message when the message is renamed.
/// </summary>
internal static class CmdEnumNames
{
    /// <summary>Obfuscated names are all upper case; a name a person or the client chose has lower case.</summary>
    public static bool IsReadable(string name) => name.Any(char.IsLower);

    /// <summary>cmdId -> the message that carries it. Where two messages share a cmdId the later one wins.</summary>
    public static Dictionary<int, string> MessageNames(IEnumerable<TypeDecl> types)
    {
        var names = new Dictionary<int, string>();
        foreach (var type in types)
            if (type.CmdId is { } cmdId) names[cmdId] = type.Name;
        return names;
    }

    /// <summary>A constant name for a command enum member: without the enum prefix and a leading <c>Cmd</c>.</summary>
    public static string Constant(string member, string enumName)
    {
        var name = ProtoSchema.MemberSuffix(member, enumName);
        return name.StartsWith("Cmd", StringComparison.Ordinal) && name.Length > 3 ? name[3..] : name;
    }

    /// <summary>
    /// Renames the command enum members of <paramref name="output"/>, a renamed copy of the dump whose
    /// types keep the dump's member order. Returns how many members took a message name.
    /// </summary>
    public static int Apply(IReadOnlyList<TypeDecl> output, IReadOnlyDictionary<string, TypeDecl> dump)
    {
        var enums = output.Where(t => t.IsCmdEnum).Select(t => (Named: t, Raw: dump[t.Ident])).ToList();

        var names = MessageNames(output);
        // A member the dump itself spells readably names its cmdId, also where no message carries it.
        foreach (var (_, raw) in enums)
        foreach (var (member, value) in raw.Members)
        {
            var name = Constant(member, raw.Name);
            if (value != 0 && IsReadable(name)) names[(int)value] = name;
        }

        var named = 0;
        foreach (var (type, raw) in enums)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < raw.Members.Count; i++)
            {
                var (member, value) = raw.Members[i];
                var suffix = ProtoSchema.MemberSuffix(member, raw.Name);
                if (value != 0 && names.TryGetValue((int)value, out var message) && IsReadable(message))
                {
                    suffix = message;
                    named++;
                }
                var full = $"{type.Name}_{suffix}";
                if (!used.Add(full))
                {
                    full = $"{type.Name}_{suffix}_{value}";
                    used.Add(full);
                }
                type.Members[i] = (full, value);
            }
        }
        return named;
    }
}
