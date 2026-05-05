using March7thHoney.Configuration;

namespace March7thHoney.Util.License;

public static class PublicConfigProvider
{
    public static ConfigContainer Create()
    {
        // Reuse ConfigContainer defaults, then apply Public-mode overrides only.
        var config = new ConfigContainer();

        config.MuipServer.AdminKey = "March7thHoneyPublicAdminKey";

        config.ServerOption.Language = UtilTools.GetCurrentLanguage();
        config.ServerOption.FallbackLanguage = config.ServerOption.Language;

        // Reduce runtime log visibility in Public mode.
        config.ServerOption.LogOption.EnableGamePacketLog = false;
        config.ServerOption.LogOption.LogPacketToConsole = false;
        config.ServerOption.LogOption.DisableLogDetailPacket = true;
        config.ServerOption.LogOption.SavePersonalDebugFile = false;
        config.ServerOption.LogOption.LogResourceCacheLoading = false;

        return config;
    }
}

