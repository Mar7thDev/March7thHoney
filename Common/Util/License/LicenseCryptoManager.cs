using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace March7thHoney.Util.License;

public static class LicenseCryptoManager
{
    private const string Prefix = "M7H1:";

    private static readonly byte[] Key = SHA256.HashData(
        Encoding.UTF8.GetBytes("March7thHoney.Public.License.Internal.Key"));

    public static string Decrypt(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Empty License text.");

        var payload = text.Trim();
        if (payload.StartsWith(Prefix, StringComparison.Ordinal))
            return DecryptAes(payload[Prefix.Length..]);

        var plain = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        if (plain.TrimStart().StartsWith("{", StringComparison.Ordinal))
            return plain;

        return DecryptAes(payload);
    }

    private static string DecryptAes(string base64)
    {
        var data = Convert.FromBase64String(base64);
        if (data.Length <= 16) throw new InvalidOperationException("Invalid License payload.");

        var iv = data.Take(16).ToArray();
        var cipher = data[16..];

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}

