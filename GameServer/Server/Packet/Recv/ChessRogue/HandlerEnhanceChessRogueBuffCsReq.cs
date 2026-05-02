using CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.ChessRogue;

[Opcode(CmdIds.EnhanceChessRogueBuffCsReq)]
public class HandlerEnhanceChessRogueBuffCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = EnhanceChessRogueBuffCsReq.Parser.ParseFrom(data);

        await connection.Player!.ChessRogueManager!.RogueInstance!.EnhanceBuff((int)req.MazeBuffId,
            RogueCommonActionResultSourceType.Enhance);
        await connection.SendPacket(
            new PacketEnhanceChessRogueBuffScRsp(connection.Player!.ChessRogueManager!.RogueInstance!, req.MazeBuffId));
    }
}