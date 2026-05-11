using March7thHoney.Internationalization;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;

namespace March7thHoney.Util.License;

public static class LicenseManager
{
    private static readonly Logger Logger = new("License");
    private const string LicensePath = "Config/March7thHoney.license";
    private const string HelpUrl = "discord.gg/castoriceps";
    private const string BuildUtcMetadataKey = "BuildUtc";

    public static bool ValidateInPublicMode()
    {
        try
        {
            var currentHwid = HardwareIdManager.GetHwid();
            Logger.Info(I18NManager.Translate("Server.ServerInfo.LicenseCurrentHwid", currentHwid));

            if (!File.Exists(LicensePath))
            {
                if (IsWithinBuildTrialWindow())
                {
                    Logger.Warn(I18NManager.Translate("Server.ServerInfo.PublicModeTrialActive",
                        LicenseInfo.TrialDays.ToString()));
                    return true;
                }

                Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseFileNotFound", LicensePath));
                Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseInvalid", HelpUrl));
                return false;
            }

            var encrypted = File.ReadAllText(LicensePath);
            var json = LicenseCryptoManager.Decrypt(encrypted);
            var license = JsonConvert.DeserializeObject<LicenseInfo>(json);

            if (license == null)
            {
                Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseParseFailed"));
                Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseInvalid", HelpUrl));
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (license.ExpireAt <= now)
            {
                Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseExpired"));
                Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseInvalid", HelpUrl));
                return false;
            }

            if (!string.Equals(license.Hwid?.Trim(), currentHwid, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseHwidMismatch"));
                Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseInvalid", HelpUrl));
                return false;
            }

            var localTime = DateTimeOffset.FromUnixTimeSeconds(license.ExpireAt).ToLocalTime();
            Logger.Info(I18NManager.Translate("Server.ServerInfo.LicenseVerified",
                license.Holder, localTime.ToString("yyyy-MM-dd HH:mm:ss")));
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseValidateFailed"), ex);
            Logger.Error(I18NManager.Translate("Server.ServerInfo.LicenseInvalid", HelpUrl));
            return false;
        }
    }

    private static bool IsWithinBuildTrialWindow()
    {
        var buildTimeUtc = GetCompileTimeUtcFromEntryAssembly();
        if (buildTimeUtc == null)
        {
            Logger.Warn(I18NManager.Translate("Server.ServerInfo.PublicModeBuildTimeUnavailable"));
            return false;
        }

        var trialExpireUtc = buildTimeUtc.Value.AddDays(LicenseInfo.TrialDays);
        return DateTime.UtcNow <= trialExpireUtc;
    }

    private static DateTime? GetCompileTimeUtcFromEntryAssembly()
    {
        var candidates = new[]
        {
            Assembly.GetEntryAssembly(),
            Assembly.GetExecutingAssembly()
        };

        foreach (var asm in candidates.Where(x => x != null))
        {
            var metadata = asm!.GetCustomAttributes<AssemblyMetadataAttribute>();
            var buildUtcRaw = metadata.FirstOrDefault(x => x.Key == BuildUtcMetadataKey)?.Value;
            if (string.IsNullOrWhiteSpace(buildUtcRaw))
                continue;

            if (DateTime.TryParseExact(buildUtcRaw, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var buildUtc))
                return buildUtc;
        }

        return null;
    }
}

