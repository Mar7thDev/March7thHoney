using CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.RefreshTriggerByClientCsReq)]
public class HandlerRefreshTriggerByClientCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RefreshTriggerByClientCsReq.Parser.ParseFrom(data);

        var player = connection.Player!;
        var ret = await player.SceneInstance!.TriggerSummonUnit((int)req.TriggerEntityId, req.TriggerName,
            req.TriggerTargetIdList.ToList());

        await connection.SendPacket(new PacketRefreshTriggerByClientScRsp(ret, req.TriggerName, req.TriggerEntityId));
    }
}