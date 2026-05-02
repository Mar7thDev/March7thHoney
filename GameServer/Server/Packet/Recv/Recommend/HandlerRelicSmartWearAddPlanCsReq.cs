using CastoriceHoney.Server.GameServer.Server.Packet.Send.Recommend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Recommend;

[Opcode(CmdIds.RelicSmartWearAddPlanCsReq)]
public class HandlerRelicSmartWearAddPlanCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RelicSmartWearAddPlanCsReq.Parser.ParseFrom(data);
        var plan = connection.Player!.AvatarManager!.AddRelicPlan(req.RelicPlan);
        await connection.SendPacket(new PacketRelicSmartWearAddPlanScRsp(plan));
    }
}