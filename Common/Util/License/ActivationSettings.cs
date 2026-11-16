using System.Reflection;

namespace March7thHoney.Util.License;

/// <summary>
///     The activation service a limited-mode build is checked against. The values are fixed when the server is
///     built, from the MSBuild properties <c>ActivationServerUrl</c>, <c>ActivationPublicKey</c> and
///     <c>ActivationHelpUrl</c> (defaults in Common.csproj), so whoever distributes their own builds points
///     them at their own service. They are not read at run time: a limited-mode build has no configuration
///     file, and a key the operator could replace would make the signature check meaningless.
/// </summary>
public static class ActivationSettings
{
    private static readonly Dictionary<string, string> Metadata = typeof(ActivationSettings).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Where(attribute => attribute.Value != null)
        .GroupBy(attribute => attribute.Key, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Last().Value!, StringComparer.Ordinal);

    /// <summary>Base URL of the activation service, without a trailing slash.</summary>
    public static string ServerUrl { get; } = Value("ActivationServerUrl").TrimEnd('/');

    /// <summary>ECDSA P-256 public key (SubjectPublicKeyInfo DER, base64) the service signs activations with.</summary>
    public static string PublicKey { get; } = Value("ActivationPublicKey");

    /// <summary>Where an operator is sent when activation fails.</summary>
    public static string HelpUrl { get; } = Value("ActivationHelpUrl");

    public static bool IsConfigured => ServerUrl.Length > 0 && PublicKey.Length > 0;

    private static string Value(string key) => Metadata.TryGetValue(key, out var value) ? value.Trim() : "";
}
