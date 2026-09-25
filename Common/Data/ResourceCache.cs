using System.IO.Compression;
using MemoryPack;
using March7thHoney.Data.Excel;
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
        // BigInteger has no built-in MemoryPack formatter; without this every cached name hash (HashName.Hash)
        // round-trips to 0. Registered here so it is in place before the first cache Save/Load.
        MemoryPackFormatterProvider.Register(new BigIntegerMemoryPackFormatter());
    }

    public static Task SaveCache()
    {
        return Task.Run(() =>
        {
            // The snapshot is dominated by highly repetitive level-graph / mission trees, so the raw
            // MemoryPack blob (~120 MB) shrinks several-fold under Deflate. GZip(Optimal) keeps both
            // generation (cache-miss only) and per-boot decompression fast without an extra dependency.
            var raw = MemoryPackSerializer.Serialize(GameData.CreateSnapshot());
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
                gzip.Write(raw, 0, raw.Length);
            File.WriteAllBytes(CachePath, output.ToArray());
            Logger.Info(I18NManager.Translate("Server.ServerInfo.GeneratedItem",
                I18NManager.Translate("Word.Cache")));
        });
    }

    public static bool LoadCache()
    {
        GameDataSnapshot? snapshot;
        try
        {
            var bytes = File.ReadAllBytes(CachePath);

            // Legacy uncompressed caches (raw MemoryPack) lack the gzip magic (0x1F 0x8B). Reject them so
            // the caller rebuilds in the current compressed format instead of feeding a non-gzip blob to
            // the decoder. A corrupt/truncated cache likewise falls through to a clean rebuild below.
            if (bytes.Length < 2 || bytes[0] != 0x1F || bytes[1] != 0x8B) return false;

            using var input = new MemoryStream(bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            snapshot = MemoryPackSerializer.Deserialize<GameDataSnapshot>(output.ToArray());
        }
        catch (Exception ex)
        {
            Logger.Error(I18NManager.Translate("Server.ServerInfo.FailedToLoadItem",
                I18NManager.Translate("Word.Cache")), ex);
            return false;
        }

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
        // 4.5 moved content-package entrances (including the Divergent Universe lobby) into
        // MapEntranceLD.json. Reject an old cache only when the matching raw table is available,
        // so deployments that intentionally ship cache-only resources keep working.
        var hasMapEntranceLd = ConfigManager.Config.Path.ExcelOutputDirs.Any(dir =>
            File.Exists(Path.Combine(ConfigManager.Config.Path.ResourcePath, dir, "MapEntranceLD.json")));
        if (hasMapEntranceLd && GameData.ContentPackageConfigData.Values.Any(package =>
                (package.InitEntranceID != 0 && !GameData.MapEntranceData.ContainsKey(package.InitEntranceID)) ||
                (package.AfterGuideEntranceID != 0 &&
                 !GameData.MapEntranceData.ContainsKey(package.AfterGuideEntranceID))))
        {
            Logger.Warn("Resource cache is stale: MapEntranceLD content-package entrances are missing.");
            return false;
        }

        // Caches built before RogueTournArea's obfuscated-key mapping collapse every area onto id 0.
        if (GameData.RogueTournAreaData.ContainsKey(0) ||
            (GameData.RogueTournAreaData.Count > 0 && GameData.RoguePersonaTalentData.Count == 0))
        {
            Logger.Warn("Resource cache is stale: RogueTourn area/persona tables are missing.");
            return false;
        }

        // Caches built before the 4.5 RogueBuffGroup key mapping collapse every classic Simulated Universe group
        // onto id 0, and caches built before the buff/group unions lose the classic RogueBuff subtype, so no
        // blessing draft pool resolves. Only rejected when the raw table is available to rebuild from.
        var hasRogueBuffGroup = ConfigManager.Config.Path.ExcelOutputDirs.Any(dir =>
            File.Exists(Path.Combine(ConfigManager.Config.Path.ResourcePath, dir, "RogueBuffGroup.json")));
        if (hasRogueBuffGroup && (GameData.RogueBuffGroupData.ContainsKey(0) ||
                                  !GameData.RogueBuffData.Values.Any(buff => buff is RogueBuffExcel) ||
                                  !GameData.RogueBuffGroupData.Values.Any(group =>
                                      group is RogueBuffGroupExcel && group.BuffList.Any(buff => buff is RogueBuffExcel))))
        {
            Logger.Warn("Resource cache is stale: classic Simulated Universe blessing groups are missing.");
            return false;
        }

        // Caches built before RogueMagicRoomGen.json was loaded, or before the 4.5 scepter lock-unit key mapping.
        if ((GameData.RogueMagicRoomGenData.Count == 0 &&
             File.Exists(ConfigManager.Config.Path.ConfigPath + "/RogueMagicRoomGen.json")) ||
            GameData.RogueMagicScepterData.Values.Any(scepter => scepter.LockMagicUnit.Any(unit => unit.MagicUnitId == 0)))
        {
            Logger.Warn("Resource cache is stale: RogueMagic room/scepter data is missing.");
            return false;
        }

        // Caches built while the ChessRogue room/dice configs and chessboard maps were not loaded, or before the
        // 4.5 RogueDLCArea score-map keys were mapped.
        if (GameData.RogueSwarmChessBoardData.Count > 0 &&
            (GameData.ChessRogueRoomData.Count == 0 || GameData.ChessRogueDiceSurfaceEffectData.Count == 0 ||
             GameData.RogueSwarmChessBoardData.Values.SelectMany(boards => boards).All(board => board.MapInfo == null) ||
             GameData.RogueDLCAreaData.Values.Any(area =>
                 area.AreaScoreMap.Count > 0 && area.AreaScoreMap.All(score => score.Layer == 0))))
        {
            Logger.Warn("Resource cache is stale: ChessRogue room/dice/chessboard data is missing.");
            return false;
        }

        if (GameData.FloorInfoData.Values.Any(floor => floor.Groups.Values.Any(group =>
                group.LoadSide != Enums.Scene.GroupLoadSideEnum.Client &&
                group.Category == Enums.Scene.GroupCategoryEnum.Atmosphere && group.IsHoyoGroup == null)))
        {
            Logger.Warn("Resource cache is stale: scene IsHoyoGroup metadata is missing. " +
                        "Deploy the matching Resource.bin or upgrade it with scripts/SceneCacheUpgrade.");
            return false;
        }

        // Caches built before AvatarDemoConfig.StageType/SPList were modelled deserialize with empty values,
        // which would make every Role Trial stage look like a non-trial row. Like MapEntranceLD above, only
        // reject when the raw table is available to rebuild from.
        var hasAvatarDemoConfig = ConfigManager.Config.Path.ExcelOutputDirs.Any(dir =>
            File.Exists(Path.Combine(ConfigManager.Config.Path.ResourcePath, dir, "AvatarDemoConfig.json")));
        if (hasAvatarDemoConfig && GameData.AvatarDemoConfigData.Count > 0 &&
            GameData.AvatarDemoConfigData.Values.All(stage => string.IsNullOrEmpty(stage.StageType)))
        {
            Logger.Warn("Resource cache is stale: AvatarDemoConfig StageType/SPList are missing.");
            return false;
        }

        // Caches built before the daily-training tables (DailyQuest / DailyMissionData / DailyMissionCount) and
        // the CocoonConfig type columns were loaded. Only rejected when the raw table is available to rebuild from.
        var hasDailyQuest = ConfigManager.Config.Path.ExcelOutputDirs.Any(dir =>
            File.Exists(Path.Combine(ConfigManager.Config.Path.ResourcePath, dir, "DailyQuest.json")));
        if (hasDailyQuest && (GameData.DailyQuestData.Count == 0 ||
                              (GameData.CocoonConfigData.Count > 0 &&
                               GameData.CocoonConfigData.Values.All(cocoon => cocoon.FarmType == 0))))
        {
            Logger.Warn("Resource cache is stale: daily training tables are missing.");
            return false;
        }

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
