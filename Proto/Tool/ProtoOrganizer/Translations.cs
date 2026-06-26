using System.Text.RegularExpressions;

namespace March7thHoney.ProtoOrganizer;

/// <summary>Loads <c>Raw/translations.txt</c> (<c>OBF = RealName</c>) and a reverse index.</summary>
public sealed class Translations
{
    public Dictionary<string, string> ObfToReal { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<string>> RealToObf { get; } = new(StringComparer.Ordinal);

    private static readonly Regex RowRx =
        new(@"^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*([A-Za-z_][A-Za-z0-9_]*)\s*$", RegexOptions.Compiled);

    public static Translations Load(string path)
    {
        var t = new Translations();
        if (!File.Exists(path)) return t;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;
            var m = RowRx.Match(line);
            if (!m.Success) continue;
            var obf = m.Groups[1].Value;
            var real = m.Groups[2].Value;
            t.ObfToReal[obf] = real;
            if (!t.RealToObf.TryGetValue(real, out var list)) t.RealToObf[real] = list = new();
            list.Add(obf);
        }
        return t;
    }
}
