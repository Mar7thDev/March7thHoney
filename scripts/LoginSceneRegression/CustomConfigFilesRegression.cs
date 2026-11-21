using March7thHoney.Data;
using March7thHoney.Data.Custom;
using March7thHoney.Util;

// The operator files a server reads after Resource.bin (CustomConfigFiles.Load(AfterCacheLoad), the pass EntryPoint
// runs on a cache boot) against the tracked Config/Custom, without a raw resource tree. Runs last: the pass replaces
// GameData values the earlier checks set up.
internal static class CustomConfigFilesRegression
{
    public static void Run(string repository, string scratch, Action<string, bool> check)
    {
        var paths = ConfigManager.Config.Path;
        var configPath = paths.ConfigPath;
        var resourcePath = paths.ResourcePath;
        try
        {
            paths.ConfigPath = Path.Combine(repository, "Config");
            paths.ResourcePath = Path.Combine(scratch, "no-raw-resource-json");
            GameData.BannersConfig = new BannersConfig();
            GameData.VideoKeysConfig = new VideoKeysConfig();
            GameData.RealityShowConfig = new RealityShowConfig();
            GameData.DiceyDungeonConfig = new DiceyDungeonConfig();
            GameData.ActivityBangbooConfig = new ActivityBangbooConfig();

            CustomConfigFiles.Load(CustomConfigPass.AfterCacheLoad);
            check("cache boot installs RealityShow.json", GameData.RealityShowConfig.Challenges.Count > 0);
            check("cache boot installs VideoKeysConfig.json", GameData.VideoKeysConfig.TotalCount > 0);
            check("cache boot installs Banners.json", GameData.BannersConfig.Banners.Count > 0);
            check("cache boot installs DiceyDungeon.json and ActivityBangboo.json",
                GameData.DiceyDungeonConfig.Levels.Count > 0 && GameData.ActivityBangbooConfig.Holes.Count > 0);

            // A raw-resource boot read these files inside LoadGameData; its later pass must not read them again.
            var banners = new BannersConfig();
            var videoKeys = new VideoKeysConfig();
            GameData.BannersConfig = banners;
            GameData.VideoKeysConfig = videoKeys;
            GameData.ActivityBangbooConfig = new ActivityBangbooConfig();
            CustomConfigFiles.Load(CustomConfigPass.AfterResourceLoad);
            check("raw-resource boot reads ActivityBangboo.json once and leaves the files LoadGameData read",
                ReferenceEquals(GameData.BannersConfig, banners) && ReferenceEquals(GameData.VideoKeysConfig, videoKeys) &&
                GameData.ActivityBangbooConfig.Holes.Count > 0);

            GameData.VideoKeysConfig = new VideoKeysConfig();
            CustomConfigFiles.Reload(ReloadableCustomConfig.VideoKeys);
            check("reloading the video keys reads VideoKeysConfig.json", GameData.VideoKeysConfig.TotalCount > 0);
        }
        finally
        {
            paths.ConfigPath = configPath;
            paths.ResourcePath = resourcePath;
        }
    }
}
