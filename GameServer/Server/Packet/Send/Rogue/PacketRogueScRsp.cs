using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketRogueScRsp : BasePacket
{
    public PacketRogueScRsp(int cmdId, IMessage data) : base((ushort)cmdId)
    {
        SetData(data);
    }
}
