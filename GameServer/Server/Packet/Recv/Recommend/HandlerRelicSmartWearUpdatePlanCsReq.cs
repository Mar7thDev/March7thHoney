using CastoriceHoney.Server.GameServer.Server.Packet.Send.Recommend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Recommend;

[Opcode(CmdIds.RelicSmartWearUpdatePlanCsReq)]
public class HandlerRelicSmartWearUpdatePlanCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RelicSmartWearUpdatePlanCsReq.Parser.ParseFrom(data);
        connection.Player!.AvatarManager!.UpdateRelicPlan(req.RelicPlan);
        await connection.SendPacket(new PacketRelicSmartWearUpdatePlanScRsp(req.RelicPlan));
    }
}