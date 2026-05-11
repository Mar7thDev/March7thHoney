using March7thHoney.Internationalization;
using Newtonsoft.Json;
using System.Reflection;

namespace March7thHoney.Util.License;

public static class LicenseManager
{
    private static readonly Logger Logger = new("License");
    private const string LicensePath = "Config/March7thHoney.license";
    private const string HelpUrl = "discord.gg/castoriceps";
    private const int DefaultTrialDays = 30;
    private const int PublicModeBuildTrialDays = 30;

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
                    Logger.Warn(
                        $"Public mode trial active. Remaining license-free window is based on build time + {PublicModeBuildTrialDays} days.");
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
            var effectiveExpireAt = GetEffectiveExpireAt(license);
            if (effectiveExpireAt <= now)
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

            var localTime = DateTimeOffset.FromUnixTimeSeconds(effectiveExpireAt).ToLocalTime();
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

    private static long GetEffectiveExpireAt(LicenseInfo license)
    {
        var fallbackIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var issuedAt = license.IssuedAt > 0 ? license.IssuedAt : fallbackIssuedAt;
        var trialDays = license.TrialDays > 0 ? license.TrialDays : DefaultTrialDays;
        var trialExpireAt = DateTimeOffset.FromUnixTimeSeconds(issuedAt).AddDays(trialDays).ToUnixTimeSeconds();

        return license.ExpireAt > 0 ? Math.Min(license.ExpireAt, trialExpireAt) : trialExpireAt;
    }

    private static bool IsWithinBuildTrialWindow()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var entryLocation = entryAssembly?.Location;
        if (string.IsNullOrWhiteSpace(entryLocation) || !File.Exists(entryLocation))
            return false;

        var buildTimeUtc = File.GetLastWriteTimeUtc(entryLocation);
        var trialExpireUtc = buildTimeUtc.AddDays(PublicModeBuildTrialDays);
        return DateTime.UtcNow <= trialExpireUtc;
    }
}

