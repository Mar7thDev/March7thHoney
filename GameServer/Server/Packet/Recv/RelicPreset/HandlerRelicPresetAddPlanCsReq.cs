using March7thHoney.GameServer.Server.Packet.Send.RelicPreset;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RelicPreset;

[Opcode(CmdIds.RelicPresetAddPlanCsReq)]
public class HandlerRelicPresetAddPlanCsReq : Handler<RelicPresetAddPlanCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, RelicPresetAddPlanCsReq req)
    {
        var plan = player.AvatarManager!.AddRelicPresetPlan(req.AvatarId, req.Name, req.RelicIds, req.SourceType);
        await connection.SendPacket(new PacketRelicPresetAddPlanScRsp(plan));
    }
}
