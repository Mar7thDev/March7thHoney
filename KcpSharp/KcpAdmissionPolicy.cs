using System.Net;

namespace March7thHoney.Kcp;

/// <summary>Bounded registry-scan admission check. Call while holding the registry lock through registration.</summary>
public static class KcpAdmissionPolicy
{
    public const int DefaultMaxConnections = 4096;
    public const int DefaultMaxPendingConnections = 256;
    public const int DefaultMaxPendingConnectionsPerIp = 16;

    public static bool CanAccept<T>(IEnumerable<T> connections, IPEndPoint remote,
        int maxConnections, int maxPendingConnections, int maxPendingConnectionsPerIp,
        Func<T, IPEndPoint> getRemote, Func<T, bool> isActive)
    {
        // Invalid configuration must not silently turn off an externally reachable resource bound.
        maxConnections = maxConnections > 0 ? maxConnections : DefaultMaxConnections;
        maxPendingConnections = maxPendingConnections > 0 ? maxPendingConnections : DefaultMaxPendingConnections;
        maxPendingConnectionsPerIp = maxPendingConnectionsPerIp > 0
            ? maxPendingConnectionsPerIp : DefaultMaxPendingConnectionsPerIp;
        var address = NormalizeAddress(remote.Address);
        var total = 0;
        var pending = 0;
        var pendingForIp = 0;
        foreach (var connection in connections)
        {
            var endpoint = getRemote(connection);
            // The caller's earlier endpoint lookup can race with another handshake.
            if (endpoint.Port == remote.Port && NormalizeAddress(endpoint.Address).Equals(address)) return false;
            if (++total >= maxConnections) return false;
            if (isActive(connection)) continue;
            if (++pending >= maxPendingConnections) return false;
            if (NormalizeAddress(endpoint.Address).Equals(address) && ++pendingForIp >= maxPendingConnectionsPerIp)
                return false;
        }
        return true;
    }

    private static IPAddress NormalizeAddress(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}
