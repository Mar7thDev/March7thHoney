using March7thHoney.GameServer.Server.Packet.Send.ChessRogue;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.ChessRogue;

[Opcode(CmdIds.ChessRogueQueryCsReq)]
public class HandlerChessRogueQueryCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketChessRogueQueryScRsp(connection.Player!));
    }
}