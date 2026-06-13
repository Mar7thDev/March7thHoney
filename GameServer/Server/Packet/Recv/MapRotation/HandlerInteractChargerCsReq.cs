using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.InteractChargerCsReq)]
public class HandlerInteractChargerCsReq : Handler<InteractChargerCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, InteractChargerCsReq req)
    {
        player.ChargerNum = 5;
        await connection.SendPacket(new PacketInteractChargerScRsp(req.ChargerInfo));
        await connection.SendPacket(new PacketUpdateEnergyScNotify(player.ChargerNum, 5));
    }
}
