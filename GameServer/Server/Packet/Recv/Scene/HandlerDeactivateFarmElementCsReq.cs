using CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.DeactivateFarmElementCsReq)]
public class HandlerDeactivateFarmElementCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = DeactivateFarmElementCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(new PacketDeactivateFarmElementScRsp(req.EntityId));
    }
}