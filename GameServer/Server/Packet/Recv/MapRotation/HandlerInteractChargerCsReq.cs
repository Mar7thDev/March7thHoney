using CastoriceHoney.Server.GameServer.Server.Packet.Send.MapRotation;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.InteractChargerCsReq)]
public class HandlerInteractChargerCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = InteractChargerCsReq.Parser.ParseFrom(data);

        connection.Player!.ChargerNum = 5;
        await connection.SendPacket(new PacketInteractChargerScRsp(req.ChargerInfo));
        await connection.SendPacket(new PacketUpdateEnergyScNotify(connection.Player!.ChargerNum, 5));
    }
}