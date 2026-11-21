using March7thHoney.Data.Custom;

namespace March7thHoney.Data;

/// <summary>The startup passes of <see cref="CustomConfigFiles.Load" />.</summary>
public enum CustomConfigPass
{
    /// <summary>
    ///     Part of <see cref="ResourceManager.LoadGameData" />: the operator files a generated Resource.bin carries,
    ///     plus DiceyDungeon and RealityShow, which the cache does not keep.
    /// </summary>
    ResourceLoad,

    /// <summary>After a raw-resource load: the overlays <see cref="ResourceLoad" /> does not cover.</summary>
    AfterResourceLoad,

    /// <summary>
    ///     After Resource.bin was read: every operator file again, so editing one only needs a restart instead of a
    ///     regenerated cache (which needs the raw resource tree most deployments do not ship).
    /// </summary>
    AfterCacheLoad
}

/// <summary>Operator files that /reload and the admin console re-read while the server runs.</summary>
public enum ReloadableCustomConfig
{
    Banners,
    Activity,
    VideoKeys
}

/// <summary>
///     Operator-authored JSON under Config/Custom (or the legacy Config root), as opposed to dumped game data. Each
///     file is declared once here; the startup passes and the runtime reload are ordered lists over those entries.
/// </summary>
public static class CustomConfigFiles
{
    private static readonly CustomFile<ActivityConfig> Activity =
        Json<ActivityConfig>("Activity", "ActivityConfig", value => GameData.ActivityConfig = value);

    private static readonly CustomFile<BannersConfig> Banners =
        new(ResourceManager.LoadBanners, () => new BannersConfig(), value => GameData.BannersConfig = value);

    private static readonly CustomFile<VideoKeysConfig> VideoKeys =
        Json<VideoKeysConfig>("VideoKeys", "VideoKeysConfig", value => GameData.VideoKeysConfig = value);

    private static readonly CustomFile<Dictionary<int, List<int>>> RogueMapGen =
        Json<Dictionary<int, List<int>>>("Rogue Map", "RogueMapGen", value => GameData.RogueMapGenData = value);

    private static readonly CustomFile<QueryProductInfoConfig> QueryProductInfo =
        Json<QueryProductInfoConfig>("QueryProductInfo", "QueryProductInfoConfig",
            value => GameData.QueryProductInfoConfig = value);

    private static readonly CustomFile<DiceyDungeonConfig> DiceyDungeon =
        Json<DiceyDungeonConfig>("DiceyDungeon", "DiceyDungeon", value => GameData.DiceyDungeonConfig = value);

    private static readonly CustomFile<RealityShowConfig> RealityShow =
        Json<RealityShowConfig>("RealityShow", "RealityShow", value => GameData.RealityShowConfig = value);

    private static readonly CustomFile<SceneRainbowGroupPropertyConfig> SceneRainbowGroupProperty =
        Json<SceneRainbowGroupPropertyConfig>("Scene Rainbow Group Property", "SceneRainbowGroupProperty",
            value => GameData.SceneRainbowGroupPropertyData = value);

    private static readonly CustomFile<ChallengePeakOverrideConfig> ChallengePeak =
        Json<ChallengePeakOverrideConfig>("ChallengePeak Override", "ChallengePeak",
            value => GameData.ChallengePeakOverrideConfig = value);

    private static readonly CustomFile<GridFightRewardRulesConfig> GridFightRewardRules =
        Json<GridFightRewardRulesConfig>("GridFight Reward Rules", "GridFightRewardRules",
            value => GameData.GridFightRewardRulesConfig = value);

    private static readonly CustomFile<GridFightBasicOrbRewardsConfig> GridFightBasicOrbRewards =
        Json<GridFightBasicOrbRewardsConfig>("GridFight Orb Rewards", "GridFightBasicOrbRewards",
            value => GameData.GridFightBasicOrbRewardsConfig = value);

    private static readonly CustomFile<GridFightRuntimeConfig> GridFightRuntime =
        Json<GridFightRuntimeConfig>("GridFight Runtime Config", "GridFightRuntime",
            value => GameData.GridFightRuntimeConfig = value);

    // Planarcadia Reverie (ActivityBangboo) stages, holes and Bangboo, decoded from client tables the resource export
    // does not produce; never part of Resource.bin, so it is read after either boot path.
    private static readonly CustomFile<ActivityBangbooConfig> ActivityBangboo =
        Json<ActivityBangbooConfig>("ActivityBangboo", "ActivityBangboo",
            value => GameData.ActivityBangbooConfig = value);

    private static readonly Action[] ResourceLoadSteps =
    [
        Activity.Replace,
        // Runs while the mission graph is still loading; AfterResourceLoad applies it again to the complete graph.
        ResourceManager.LoadMissionInfoOverrides,
        Banners.Replace,
        VideoKeys.Replace,
        RogueMapGen.Replace,
        QueryProductInfo.Replace,
        DiceyDungeon.Replace,
        RealityShow.Replace,
        SceneRainbowGroupProperty.Replace,
        // Applied by ResourceManager.LoadGameData once the floors are loaded.
        ChallengePeak.Replace,
        GridFightRewardRules.Replace,
        GridFightBasicOrbRewards.Replace,
        GridFightRuntime.Replace,
        LinkGridFightRewards,
        ResourceManager.LoadRogueMagicRoomData,
        ResourceManager.LoadChessRogueRoomData,
        ResourceManager.LoadChessRogueDiceSurfaceEffectData
    ];

    private static readonly Action[] AfterResourceLoadSteps =
    [
        ResourceManager.LoadMissionInfoOverrides,
        ResourceManager.ReportTruncatedMissionGraphs,
        ActivityBangboo.Replace,
        // ExRank (奇缘星魂) materials are an operator overlay.
        AvatarExRankMaterials.Apply,
        // Story-trial skill limits and IsUseWorldLevel live on the raw excel rows, not in Resource.bin.
        StoryTrialTables.Load
    ];

    private static readonly Action[] AfterCacheLoadSteps =
    [
        ResourceManager.LoadMissionInfoOverrides,
        ResourceManager.ReportTruncatedMissionGraphs,
        Banners.Replace,
        VideoKeys.Replace,
        // The cache carries a copy of these two; keep it when the file is missing or unreadable.
        Activity.Overlay,
        RogueMapGen.Overlay,
        ActivityBangboo.Replace,
        AvatarExRankMaterials.Apply,
        StoryTrialTables.Load,
        // Seasons ChallengePeak.json leaves out are derived from the floors in the cache.
        OverlayChallengePeak,
        DiceyDungeon.Replace,
        RealityShow.Replace,
        // Room pools and dice surfaces added after a cache was built must not require replacing it.
        ResourceManager.LoadRogueMagicRoomData,
        ResourceManager.LoadChessRogueRoomData,
        ResourceManager.LoadChessRogueDiceSurfaceEffectData,
        ResourceManager.LoadRogueChestMapInfo
    ];

    private static readonly Dictionary<ReloadableCustomConfig, Action> ReloadSteps = new()
    {
        [ReloadableCustomConfig.Banners] = Banners.Replace,
        [ReloadableCustomConfig.Activity] = Activity.Replace,
        [ReloadableCustomConfig.VideoKeys] = VideoKeys.Replace
    };

    /// <summary>Reads the operator files of one startup pass, in a fixed order.</summary>
    public static void Load(CustomConfigPass pass)
    {
        var steps = pass switch
        {
            CustomConfigPass.ResourceLoad => ResourceLoadSteps,
            CustomConfigPass.AfterResourceLoad => AfterResourceLoadSteps,
            CustomConfigPass.AfterCacheLoad => AfterCacheLoadSteps,
            _ => throw new ArgumentOutOfRangeException(nameof(pass), pass, null)
        };
        foreach (var step in steps) step();
    }

    /// <summary>Re-reads one operator file; a missing or unreadable file installs an empty configuration.</summary>
    public static void Reload(ReloadableCustomConfig file)
    {
        ReloadSteps[file]();
    }

    // Resource.bin stores the reward rules and the runtime settings inside the orb-reward configuration, so they are
    // linked before a cache can be written from this load.
    private static void LinkGridFightRewards()
    {
        GameData.GridFightBasicOrbRewardsConfig.RewardRules = GameData.GridFightRewardRulesConfig;
        GameData.GridFightBasicOrbRewardsConfig.RuntimeConfig = GameData.GridFightRuntimeConfig;
    }

    private static void OverlayChallengePeak()
    {
        if (ChallengePeak.TryOverlay()) ResourceManager.ApplyChallengePeakOverrides();
    }

    private static CustomFile<T> Json<T>(string label, string fileName, Action<T> assign) where T : class, new()
    {
        return new CustomFile<T>(() => ResourceManager.LoadCustomFile<T>(label, fileName), () => new T(), assign);
    }

    private sealed class CustomFile<T>(Func<T?> read, Func<T> empty, Action<T> assign) where T : class
    {
        /// <summary>Installs the file, or an empty configuration when it is missing or unreadable.</summary>
        public void Replace()
        {
            assign(read() ?? empty());
        }

        /// <summary>Installs the file, keeping the loaded value when the file is missing or unreadable.</summary>
        public void Overlay()
        {
            TryOverlay();
        }

        public bool TryOverlay()
        {
            var value = read();
            if (value == null) return false;
            assign(value);
            return true;
        }
    }
}
