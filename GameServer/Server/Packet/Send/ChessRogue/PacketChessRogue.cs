using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogue : BasePacket
{
    public PacketChessRogue(int cmdId, IMessage data) : base((ushort)cmdId)
    {
        SetData(data);
    }
}
