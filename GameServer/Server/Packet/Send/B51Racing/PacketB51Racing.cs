using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.B51Racing;

public class PacketB51Racing : BasePacket
{
    public PacketB51Racing(int cmdId, IMessage message) : base((ushort)cmdId)
    {
        SetData(message);
    }
}
