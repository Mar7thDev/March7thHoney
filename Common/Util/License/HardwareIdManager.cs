using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace March7thHoney.Util.License;

public static class HardwareIdManager
{
    public static string GetHwid()
    {
        var raw = string.Join("|",
            GetMachineGuid(),
            Environment.MachineName,
            Environment.UserDomainName,
            Environment.OSVersion.VersionString);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static string GetMachineGuid()
    {
        if (!OperatingSystem.IsWindows())
            return "unknown-guid";

        return GetMachineGuidWindows();
    }

    [SupportedOSPlatform("windows")]
    private static string GetMachineGuidWindows()
    {
        try
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var guid = key?.GetValue("MachineGuid")?.ToString();
            return string.IsNullOrWhiteSpace(guid) ? "unknown-guid" : guid.Trim();
        }
        catch
        {
            return "unknown-guid";
        }
    }
}

