using CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.ChessRogue;

[Opcode(CmdIds.ChessRogueNousEditDiceCsReq)]
public class HandlerChessRogueNousEditDiceCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;
        var req = ChessRogueNousEditDiceCsReq.Parser.ParseFrom(data);

        var diceData = player.ChessRogueManager!.SetDice(req.QueryDiceInfo);

        await connection.SendPacket(new PacketChessRogueNousEditDiceScRsp(diceData));
    }
}