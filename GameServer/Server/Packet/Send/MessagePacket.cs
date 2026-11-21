using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send;

/// <summary>
///     A packet that carries one already-built protobuf message under its command id, for responses and notifies
///     that need no packet-specific assembly.
/// </summary>
public class MessagePacket : BasePacket
{
    public MessagePacket(int cmdId, IMessage data) : base((ushort)cmdId)
    {
        SetData(data);
    }
}
