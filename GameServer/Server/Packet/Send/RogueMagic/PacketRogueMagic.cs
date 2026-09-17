using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueMagic;

public class PacketRogueMagic : BasePacket
{
    public PacketRogueMagic(int cmdId, IMessage data) : base((ushort)cmdId)
    {
        SetData(data);
    }
}
