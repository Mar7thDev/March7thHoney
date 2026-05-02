using CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.ChessRogue;

[Opcode(CmdIds.ChessRogueQueryCsReq)]
public class HandlerChessRogueQueryCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketChessRogueQueryScRsp(connection.Player!));
    }
}