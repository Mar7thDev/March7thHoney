using System.Text;

namespace March7thHoney.ProtoOrganizer;

/// <summary>
/// Writes the grouped, version-aware proto tree. Each output file re-emits the curated bodies of its
/// member types verbatim (regrouping only), and declares the minimal set of cross-file imports.
/// </summary>
public static class Emitter
{
    public static void Emit(GroupResult gr, string outDir, bool active)
    {
        Directory.CreateDirectory(outDir);

        var byName = gr.Buckets.Values.SelectMany(b => b.Members)
            .ToDictionary(d => d.Name, StringComparer.Ordinal);

        foreach (var bucket in gr.Buckets.Values.OrderBy(b => b.FileName, StringComparer.Ordinal))
        {
            // Minimal imports: home file of every referenced type, excluding self and missing types.
            var importFiles = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var d in bucket.Members)
                foreach (var r in d.Refs)
                {
                    if (!byName.ContainsKey(r)) continue;            // referenced type not in curated layer
                    var home = gr.Assignment[r];
                    if (home != bucket.FileName) importFiles.Add(home);
                }

            var sb = new StringBuilder();
            sb.Append("syntax = \"proto3\";\n\n");
            if (active)
                sb.Append("option csharp_namespace = \"March7thHoney.Proto\";\n\n");
            foreach (var f in importFiles)
                sb.Append("import \"").Append(f).Append("\";\n");
            if (importFiles.Count > 0) sb.Append('\n');

            bool first = true;
            foreach (var d in bucket.Members.OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                if (!first) sb.Append('\n');
                first = false;
                sb.Append(d.Body).Append('\n');
            }

            File.WriteAllText(Path.Combine(outDir, bucket.FileName), sb.ToString(), new UTF8Encoding(false));
        }
    }
}
