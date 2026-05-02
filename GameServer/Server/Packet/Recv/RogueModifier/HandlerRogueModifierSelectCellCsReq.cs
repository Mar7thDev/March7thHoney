using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueModifier;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueModifier;

[Opcode(CmdIds.RogueModifierSelectCellCsReq)]
public class HandlerRogueModifierSelectCellCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RogueModifierSelectCellCsReq.Parser.ParseFrom(data);

        if (connection.Player!.ChessRogueManager?.RogueInstance == null) return;

        await connection.Player!.ChessRogueManager!.RogueInstance.ApplyModifier((int)req.CellId);

        await connection.SendPacket(new PacketRogueModifierSelectCellScRsp(req.CellId));
    }
}