using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.ChessRogue;

[Opcode(CmdIds.ChessRogueReRollDiceCsReq)]
public class HandlerChessRogueReRollDiceCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        if (connection.Player!.ChessRogueManager?.RogueInstance == null) return;
        await connection.Player!.ChessRogueManager.RogueInstance.ReRollDice();
    }
}