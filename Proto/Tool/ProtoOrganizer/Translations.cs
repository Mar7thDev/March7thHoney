using System.Text.RegularExpressions;

namespace March7thHoney.ProtoOrganizer;

/// <summary>Loads <c>Raw/translations.txt</c> (<c>OBF = RealName</c>) and a reverse index.</summary>
public sealed class Translations
{
    public Dictionary<string, string> ObfToReal { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<string>> RealToObf { get; } = new(StringComparer.Ordinal);

    private static readonly Regex RowRx =
        new(@"^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*([A-Za-z_][A-Za-z0-9_]*)\s*$", RegexOptions.Compiled);

    public void Add(string obf, string real)
    {
        ObfToReal[obf] = real;
        if (!RealToObf.TryGetValue(real, out var list)) RealToObf[real] = list = new();
        if (!list.Contains(obf, StringComparer.Ordinal)) list.Add(obf);
    }

    public static Translations Load(string path) => Load(new[] { path });

    public static Translations Load(IEnumerable<string> paths)
    {
        var t = new Translations();
        foreach (var path in paths.Where(File.Exists))
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;
            var m = RowRx.Match(line);
            if (!m.Success) continue;
            var obf = m.Groups[1].Value;
            var real = m.Groups[2].Value;
            t.Add(obf, real);
        }
        return t;
    }

    public static Translations FromMappings(IReadOnlyDictionary<string, string> mappings)
    {
        var t = new Translations();
        foreach (var (obf, real) in mappings)
        {
            t.Add(obf, real);
        }
        return t;
    }
}
