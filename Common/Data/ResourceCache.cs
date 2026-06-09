using MemoryPack;
using March7thHoney.Internationalization;
using March7thHoney.Util;

namespace March7thHoney.Data;

/// <summary>
///     Binary resource cache. The full game data is serialized to a single MemoryPack blob (Resource.bin)
///     via <see cref="GameData.CreateSnapshot" /> and restored via <see cref="GameData.ApplySnapshot" /> —
///     no reflection, no JSON. Generated offline from the raw JSON resources, read at runtime for fast startup.
/// </summary>
public static class ResourceCache
{
    public static Logger Logger { get; } = new("ResCache");
    public static string CachePath { get; } = ConfigManager.Config.Path.ConfigPath + "/Resource.bin";
    public static bool IsComplete { get; set; } = true; // cleared on partial load errors during generation

    static ResourceCache()
    {
        // Enable serializing the untyped `object` JSON values used by some resource models.
        MemoryPackFormatterProvider.Register(new JsonObjectMemoryPackFormatter());
    }

    public static Task SaveCache()
    {
        return Task.Run(() =>
        {
            var bytes = MemoryPackSerializer.Serialize(GameData.CreateSnapshot());
            File.WriteAllBytes(CachePath, bytes);
            Logger.Info(I18NManager.Translate("Server.ServerInfo.GeneratedItem",
                I18NManager.Translate("Word.Cache")));
        });
    }

    public static bool LoadCache()
    {
        var snapshot = MemoryPackSerializer.Deserialize<GameDataSnapshot>(File.ReadAllBytes(CachePath));
        if (snapshot == null) return false;

        GameData.ApplySnapshot(snapshot);

        // Loaded()/AfterAllDone() post-load hooks are skipped on the cache path, and the get-only
        // `ChallengeMonsters` dictionaries they populate can't round-trip through MemoryPack — re-derive
        // them here, otherwise endgame bosses fail to spawn: ChallengePeak normal mode, and the 3rd/tierce
        // node of MOC / Pure Fiction / Apocalyptic Shadow.
        ResourceManager.ApplyChallengePeakOverrides();
        foreach (var tierce in GameData.ChallengeMazeTierceConfigData.Values)
            tierce.RebuildChallengeMonsters();

        Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadedItem", I18NManager.Translate("Word.Cache")));

        return ValidateLoadedCache();
    }

    public static void ClearGameData()
    {
        GameData.ApplySnapshot(new GameDataSnapshot());
    }

    private static bool ValidateLoadedCache()
    {
        if (GameData.EquipmentConfigData.Count == 0)
        {
            Logger.Warn("Resource cache is stale: EquipmentConfigData is missing.");
            return false;
        }

        var staleEquipmentConfig = GameData.EquipmentConfigData.Values
            .Any(config => config.Release && (config.ExpProvide <= 0 || config.CoinCost <= 0));
        if (!staleEquipmentConfig) return true;

        Logger.Warn("Resource cache is stale: EquipmentConfigData is missing Light Cone EXP/cost fields.");
        return false;
    }
}
