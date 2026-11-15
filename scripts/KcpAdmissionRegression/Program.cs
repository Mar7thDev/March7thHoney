using System.Net;
using March7thHoney.Kcp;

var failures = 0;
var checks = 0;
void Check(string name, bool condition)
{
    checks++;
    if (!condition) { failures++; Console.Error.WriteLine($"FAIL: {name}"); }
}
IPEndPoint Ep(string address, int port) => new(IPAddress.Parse(address), port);
bool Accept(IEnumerable<(IPEndPoint Remote, bool Active)> snapshot, IPEndPoint remote,
    int total = 8, int pending = 4, int perIp = 2) =>
    KcpAdmissionPolicy.CanAccept(snapshot, remote, total, pending, perIp,
        static c => c.Remote, static c => c.Active);

var ip = "192.0.2.1";
var other = "192.0.2.2";
List<(IPEndPoint Remote, bool Active)> connections = [];
Check("first connection accepted", Accept(connections, Ep(ip, 1000)));
connections.Add((Ep(ip, 1000), false));
Check("duplicate endpoint rejected atomically", !Accept(connections, Ep(ip, 1000)));
Check("another pending behind NAT admitted below quota", Accept(connections, Ep(ip, 1001)));
connections.Add((Ep(ip, 1001), false));
Check("per-IP pending cap enforced across ports", !Accept(connections, Ep(ip, 1002)));
Check("unrelated IP unaffected by other IP pending cap", Accept(connections, Ep(other, 1000)));
Check("IPv4-mapped IPv6 cannot bypass same-IP cap", !Accept(connections, Ep("::ffff:192.0.2.1", 1002)));
connections[0] = (connections[0].Remote, true);
Check("authentication releases a pending slot without eviction", Accept(connections, Ep(ip, 1002)));
connections.Clear();
for (var i = 0; i < 4; i++) connections.Add((Ep($"192.0.2.{i + 1}", 1000), false));
Check("global pending cap enforced", !Accept(connections, Ep("192.0.2.9", 1000)));
connections.RemoveAt(0);
Check("unregister naturally releases pending capacity", Accept(connections, Ep("192.0.2.9", 1000)));
connections.Clear();
for (var i = 0; i < 8; i++) connections.Add((Ep(ip, 1000 + i), true));
Check("active clients count toward global cap", !Accept(connections, Ep(other, 1000)));
connections.RemoveAt(0);
Check("authenticated NAT users do not consume pending-IP quota", Accept(connections, Ep(ip, 2000)));
Check("configured limit one permits empty registry", Accept([], Ep(ip, 1000), 1, 1, 1));
Check("configured limit one rejects occupied registry", !Accept([(Ep(other, 1000), true)], Ep(ip, 1000), 1, 1, 1));
connections.Clear();
for (var i = 0; i < KcpAdmissionPolicy.DefaultMaxPendingConnections; i++)
    connections.Add((Ep($"198.51.100.{i % 254 + 1}", 1000 + i), false));
Check("nonpositive configuration does not disable bounds", !Accept(connections, Ep(ip, 5000), 0, -1, 0));

// Same lock/check/register sequence as Listener.AcceptConnection: no actual sockets or KCP queues.
var gate = new object();
connections.Clear();
var accepted = 0;
Parallel.For(0, 1000, i =>
{
    lock (gate)
    {
        if (!Accept(connections, Ep(ip, 10000 + i), total: 32, pending: 16, perIp: 7)) return;
        connections.Add((Ep(ip, 10000 + i), false));
        accepted++;
    }
});
Check("parallel same-IP churn cannot overrun bound", accepted == 7 && connections.Count == 7);
connections.Clear(); accepted = 0;
Parallel.For(0, 1000, i =>
{
    lock (gate)
    {
        if (!Accept(connections, Ep($"198.51.100.{i % 254 + 1}", 10000 + i), 32, 16, 7)) return;
        connections.Add((Ep($"198.51.100.{i % 254 + 1}", 10000 + i), false));
        accepted++;
    }
});
Check("parallel distributed churn cannot overrun pending bound", accepted == 16 && connections.Count == 16);
Console.WriteLine($"KCP admission checks: {checks}, failures: {failures}");
return failures == 0 ? 0 : 1;
