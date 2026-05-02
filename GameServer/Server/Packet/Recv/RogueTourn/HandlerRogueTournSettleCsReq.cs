using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueTourn;

[Opcode(CmdIds.RogueTournSettleCsReq)]
public class HandlerRogueTournSettleCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var inst = connection.Player!.RogueTournManager?.RogueTournInstance;

        if (inst == null)
        {
            await connection.SendPacket(new PacketRogueTournSettleScRsp());
            return;
        }

        await inst.QuitRogue();
        await connection.SendPacket(new PacketRogueTournSettleScRsp(inst));
    }
}