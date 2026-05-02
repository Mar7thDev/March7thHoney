using CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightHandlePendingActionCsReq)]
public class HandlerGridFightHandlePendingActionCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightHandlePendingActionCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst != null)
            await inst.HandleResultRequest(req);

        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(req.QueuePosition));
    }
}