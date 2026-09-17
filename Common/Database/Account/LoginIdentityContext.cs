using System.Collections;

namespace March7thHoney.Database.Account;

// Retains observed values for the authenticated admin view; matching continues to use hashed keys.
public sealed class LoginIdentityContext(IReadOnlyCollection<string> keys, string? ipAddress,
    IReadOnlyCollection<string> devices) : IReadOnlyCollection<string>
{
    public string? IpAddress { get; } = ipAddress;
    public IReadOnlyCollection<string> Devices { get; } = devices;
    public int Count => keys.Count;
    public IEnumerator<string> GetEnumerator() => keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
