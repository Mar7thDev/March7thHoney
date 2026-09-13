using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTourn : BasePacket
{
    public PacketRogueTourn(int cmdId, IMessage data) : base((ushort)cmdId)
    {
        SetData(data);
    }
}
