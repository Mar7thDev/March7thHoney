using March7thHoney.Util;
using March7thHoney.Util.Security;
using System.Security.Cryptography;

namespace March7thHoney.WebServer.Handler;

public static class LoginCryptoHelper
{
    private static readonly Logger Logger = new("LoginCrypto");
    private static string SecurityDir => Path.Combine(ConfigManager.Config.Path.ConfigPath, "Security");
    private static string PrivateKeyPath => Path.Combine(SecurityDir, "LoginRsaPrivateKey.pem");

    public static (string account, string password) TryDecryptCredentials(string? account, string? password, bool decryptRequested)
    {
        var rawAccount = account ?? string.Empty;
        var rawPassword = password ?? string.Empty;

        if (!decryptRequested) return (rawAccount, rawPassword);

        var privatePem = LoadPrivateKey();
        if (string.IsNullOrWhiteSpace(privatePem))
            return (rawAccount, rawPassword);

        privatePem = NormalizePem(privatePem);
        return (TryDecrypt(rawAccount, privatePem), TryDecrypt(rawPassword, privatePem));
    }

    private static string TryDecrypt(string raw, string privatePem)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        if (!LooksLikeBase64(raw)) return raw;

        var paddings = new[]
        {
            RSAEncryptionPadding.Pkcs1,
            RSAEncryptionPadding.OaepSHA1,
            RSAEncryptionPadding.OaepSHA256
        };

        foreach (var padding in paddings)
        {
            try
            {
                return March7thHoney.Util.Security.RSA.DecryptFromBase64(raw, privatePem, null, padding);
            }
            catch (Exception e)
            {
                Logger.Debug($"RSA login decrypt with {padding.Mode} failed, fallback next. len={raw.Length}", e);
            }
        }

        Logger.Debug($"RSA login decrypt failed for all paddings, fallback plaintext. len={raw.Length}");
        return raw;
    }

    private static bool LooksLikeBase64(string value)
    {
        if (value.Length < 16 || value.Length % 4 != 0) return false;
        return Convert.TryFromBase64String(value, new Span<byte>(new byte[value.Length]), out _);
    }

    private static string NormalizePem(string pem)
    {
        var normalized = pem.Replace("\\n", "\n").Trim();

        // Accept raw base64 key material in config and wrap it as PEM.
        if (!normalized.Contains("BEGIN", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "-----BEGIN RSA PRIVATE KEY-----\n" + normalized + "\n-----END RSA PRIVATE KEY-----";
        }

        return normalized;
    }

    private static string LoadPrivateKey()
    {
        try
        {
            if (File.Exists(PrivateKeyPath))
                return File.ReadAllText(PrivateKeyPath).Trim();
        }
        catch (Exception e)
        {
            Logger.Debug($"Load private key from file failed: {PrivateKeyPath}", e);
        }

        return ConfigManager.Config.WebSecurity.RsaPrivateKeyPem;
    }
}
