using March7thHoney.Internationalization;
using March7thHoney.Util;

namespace March7thHoney.Data;

/// <summary>Loads the configured raw resources first, falling back to a complete cache on failure.</summary>
public static class ResourceStartupLoader
{
    public static async Task<bool> LoadAsync()
    {
        ResourceCache.ClearGameData();
        ResourceCache.IsComplete = true;
        try
        {
            CheckResourceFiles();
            ResourceManager.Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem",
                I18NManager.Translate("Word.GameData")));
            ResourceManager.LoadGameData();
            if (!ResourceCache.IsComplete || GameData.AvatarConfigData.Count == 0 || GameData.FloorInfoData.Count == 0)
                throw new InvalidDataException("The configured raw resources could not be loaded completely.");
        }
        catch (Exception resourceError)
        {
            var displayPath = ConfigManager.Config.Path.ResourcePath?.Replace("\0", "\\0");
            ResourceManager.Logger.Warn($"Cannot load ResourcePath '{displayPath}': " +
                $"{resourceError.Message} Falling back to '{ResourceCache.CachePath}'.");
            // Never retain partially loaded raw tables beside cached data.
            ResourceCache.ClearGameData();
            try
            {
                // Raw-table stale checks do not apply to an unavailable resource tree.
                // Cache decoding and its independent integrity checks still apply.
                if (!ResourceCache.LoadCache(validateAgainstResources: false))
                    throw new InvalidDataException("The fallback resource cache is missing, unreadable or invalid.");
                if (GameData.AvatarConfigData.Count == 0 || GameData.FloorInfoData.Count == 0)
                    throw new InvalidDataException("The fallback resource cache is missing character or scene data.");
                ResourceCache.IsComplete = true;
                return false;
            }
            catch (Exception cacheError)
            {
                ResourceCache.ClearGameData();
                ResourceCache.IsComplete = false;
                throw new InvalidDataException("Neither ResourcePath nor Resource.bin could be loaded.",
                    new AggregateException(resourceError, cacheError));
            }
        }

        if (ConfigManager.Config.ServerOption.UseCache)
        {
            try
            {
                await ResourceCache.SaveCache();
            }
            catch (Exception cacheError) when (cacheError is IOException or UnauthorizedAccessException)
            {
                // A cache write failure does not invalidate successfully loaded raw data.
                ResourceManager.Logger.Warn($"Raw resources loaded, but Resource.bin could not be saved: {cacheError.Message}");
            }
        }
        return true;
    }

    private static void CheckResourceFiles()
    {
        var root = ConfigManager.Config.Path.ResourcePath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            throw new DirectoryNotFoundException("ResourcePath must name an existing resource directory.");
        var floors = Path.Combine(root, "Config", "LevelOutput", "RuntimeFloor");
        if (!Directory.EnumerateFiles(floors, "*.json").Any())
            throw new FileNotFoundException("ResourcePath has no runtime floor files.", floors);

        foreach (var type in ResourceManager.GetActiveExcelResourceTypes())
        {
            if (Attribute.GetCustomAttribute(type, typeof(ResourceEntity)) is not ResourceEntity attribute) continue;
            var optional = attribute.OptionalFileNames.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var name in attribute.FileName)
            {
                var path = ConfigManager.Config.Path.ExcelOutputDirs
                    .Select(dir => Path.Combine(root, dir, name)).FirstOrDefault(File.Exists);
                if (path == null)
                {
                    if (optional.Contains(name, StringComparer.Ordinal)) continue;
                    throw new FileNotFoundException($"ResourcePath is missing the required table '{name}'.");
                }
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.ReadByte() < 0)
                    throw new InvalidDataException($"ResourcePath contains an empty table '{path}'.");
            }
        }
    }
}
