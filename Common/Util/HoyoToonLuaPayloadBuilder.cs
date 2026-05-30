using System.Text;

namespace March7thHoney.Util;

public static class HoyoToonLuaPayloadBuilder
{
    private static readonly string[] HoyoToonLuaModules =
    [
        "core",
        "catalog",
        "ids",
        "values",
        "resources",
        "scope",
        "runtime",
        "hotkeys",
        "character"
    ];

    public static async Task<byte[]> BuildAsync(string luaRoot, string relativePath)
    {
        var filePath = ResolveLuaPath(luaRoot, relativePath);
        var fileText = await File.ReadAllTextAsync(filePath);

        if (!ShouldBundle(relativePath, fileText))
            return Encoding.UTF8.GetBytes(fileText);

        return Encoding.UTF8.GetBytes(await BuildBundleAsync(luaRoot, relativePath, fileText));
    }

    public static async Task<byte[]> BuildInlineAsync(string luaRoot, string virtualPath, string fileText)
    {
        return Encoding.UTF8.GetBytes(await BuildBundleAsync(luaRoot, virtualPath, fileText));
    }

    public static string ResolveLuaPath(string luaRoot, string relativePath)
    {
        luaRoot = Path.GetFullPath(luaRoot);
        var filePath = Path.GetFullPath(Path.Combine(luaRoot, relativePath));
        var rootedRelative = Path.GetRelativePath(luaRoot, filePath);
        if (Path.IsPathRooted(rootedRelative) ||
            rootedRelative.Equals("..", StringComparison.Ordinal) ||
            rootedRelative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            rootedRelative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new IOException("Lua path escapes Lua root: " + relativePath);
        }
        return filePath;
    }

    public static bool ShouldBundle(string relativePath, string fileText)
    {
        var normalized = relativePath.Replace("\\", "/");
        return fileText.Contains("--#hoyotoonlua", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("HoyoToonLua.lua", StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith("Avatar/", StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith("Characters/", StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith("Auto/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> BuildBundleAsync(string luaRoot, string relativePath, string entryText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Bundled by March7thHoney for HoyoToonLua.");
        sb.AppendLine("_G.HoyoToonLua_BUNDLE = _G.HoyoToonLua_BUNDLE or {}");
        if (relativePath.Replace("\\", "/").StartsWith("Auto/", StringComparison.OrdinalIgnoreCase))
            sb.AppendLine("_G.HoyoToonLua_QUIET_BOOT = true");

        foreach (var module in HoyoToonLuaModules)
        {
            var modulePath = ResolveFirstExistingPath(luaRoot,
            [
                Path.Combine("SDK", module + ".lua"),
                Path.Combine("HoyoToonLua", module + ".lua")
            ]);
            if (!File.Exists(modulePath)) continue;

            var moduleText = await File.ReadAllTextAsync(modulePath);
            sb.Append("_G.HoyoToonLua_BUNDLE[");
            sb.Append(ToLuaString(module));
            sb.AppendLine("] = function()");
            sb.AppendLine(moduleText);
            sb.AppendLine("end");
        }

        var normalized = relativePath.Replace("\\", "/");
        if (!normalized.Equals("HoyoToonLua.lua", StringComparison.OrdinalIgnoreCase))
        {
            var bootPath = Path.Combine(luaRoot, "HoyoToonLua.lua");
            if (File.Exists(bootPath))
            {
                sb.AppendLine("-- HoyoToonLua bootstrap.");
                sb.AppendLine(await File.ReadAllTextAsync(bootPath));
            }
        }

        sb.AppendLine("-- /windy or auto-run entry.");
        sb.AppendLine(entryText);
        return sb.ToString();
    }

    private static string ResolveFirstExistingPath(string luaRoot, IEnumerable<string> relativePaths)
    {
        foreach (var relativePath in relativePaths)
        {
            var filePath = Path.Combine(luaRoot, relativePath);
            if (File.Exists(filePath)) return filePath;
        }

        return Path.Combine(luaRoot, relativePaths.First());
    }

    private static string ToLuaString(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal) + "\"";
    }
}
