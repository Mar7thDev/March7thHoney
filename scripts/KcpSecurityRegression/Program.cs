using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using March7thHoney.Kcp.KcpSharp;

// All packets are delivered in memory. No sockets, listener, accounts or game resources are used.
var expected = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 41000);
var wrongIp = new IPEndPoint(IPAddress.Parse("192.0.2.11"), 41000);
var wrongPort = new IPEndPoint(expected.Address, 41001);
var transport = new NullTransport();
var checks = 0;
var failures = 0;
void Check(string name, bool success)
{
    checks++;
    if (!success) failures++;
    Console.WriteLine($"{(success ? "PASS" : "FAIL")} {name}");
}

byte[] Packet(long? id, bool reliable, byte value)
{
    var overhead = (id.HasValue ? 8 : 0) + (reliable ? 20 : 0);
    var packet = new byte[overhead + 1];
    var offset = 0;
    if (id.HasValue)
    {
        BinaryPrimitives.WriteInt64BigEndian(packet, id.Value);
        offset = 8;
    }
    if (reliable)
    {
        packet[offset] = 81; // KCP PUSH, one fragment, sequence 0.
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(offset + 2), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(offset + 16), 1);
    }
    packet[overhead] = value;
    return packet;
}

foreach (var useId in new[] { true, false })
foreach (var attacker in new[] { wrongIp, wrongPort })
{
    long? id = useId ? 12345 : null;
    using var conv = id.HasValue
        ? new KcpConversation(expected, transport, id.Value, new KcpConversationOptions { NoDelay = true, UpdateInterval = 10 })
        : new KcpConversation(expected, transport, new KcpConversationOptions { NoDelay = true, UpdateInterval = 10 });
    await conv.InputPakcetAsync(new UdpReceiveResult(Packet(id, true, 0xE1), attacker));
    await conv.InputPakcetAsync(new UdpReceiveResult(Packet(id, true, 0xA2), new IPEndPoint(expected.Address, expected.Port)));
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    await conv.WaitToReceiveAsync(timeout.Token);
    var buffer = new byte[16];
    Check($"KCP {(useId ? "with" : "without")} ID rejects foreign {(attacker.Port == expected.Port ? "IP" : "port")} and accepts legitimate sequence0",
        conv.TryReceive(buffer, out var result) && result.BytesReceived == 1 && buffer[0] == 0xA2);
}

foreach (var useId in new[] { true, false })
foreach (var attacker in new[] { wrongIp, wrongPort })
{
    long? id = useId ? 12345 : null;
    using var raw = id.HasValue ? new KcpRawChannel(expected, transport, id.Value, null) : new KcpRawChannel(expected, transport, null);
    await raw.InputPakcetAsync(new UdpReceiveResult(Packet(id, false, 0xE1), attacker));
    await raw.InputPakcetAsync(new UdpReceiveResult(Packet(id, false, 0xA2), new IPEndPoint(expected.Address, expected.Port)));
    var buffer = new byte[16];
    Check($"Raw {(useId ? "with" : "without")} ID rejects foreign {(attacker.Port == expected.Port ? "IP" : "port")} and accepts legitimate payload",
        raw.TryReceive(buffer, out var result) && result.BytesReceived == 1 && buffer[0] == 0xA2);
    Check("Rejected raw packet does not consume receive-queue space", !raw.TryReceive(buffer, out _));
}

using (var multiplex = new KcpMultiplexConnection<object>(transport))
{
    var conv = multiplex.CreateConversation(12345, expected, new KcpConversationOptions { NoDelay = true, UpdateInterval = 10 });
    await multiplex.InputPakcetAsync(new UdpReceiveResult(Packet(12345, true, 0xE1), wrongPort));
    await multiplex.InputPakcetAsync(new UdpReceiveResult(Packet(12346, true, 0xE2), expected));
    await multiplex.InputPakcetAsync(new UdpReceiveResult(Packet(12345, true, 0xA2), expected));
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    await conv.WaitToReceiveAsync(timeout.Token);
    var buffer = new byte[16];
    Check("Multiplex routing cannot bypass endpoint and conversation ID validation",
        conv.TryReceive(buffer, out var result) && result.BytesReceived == 1 && buffer[0] == 0xA2);
}

Console.WriteLine($"KCP security checks: {checks}, failures: {failures}");
return failures == 0 ? 0 : 1;

sealed class NullTransport : IKcpTransport
{
    public ValueTask SendPacketAsync(Memory<byte> packet, IPEndPoint remoteEndpoint, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
