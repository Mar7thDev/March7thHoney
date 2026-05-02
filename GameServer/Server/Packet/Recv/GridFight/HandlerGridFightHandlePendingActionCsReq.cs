using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

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