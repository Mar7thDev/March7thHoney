using March7thHoney.GameServer.Server.Packet.Send.RogueModifier;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueModifier;

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