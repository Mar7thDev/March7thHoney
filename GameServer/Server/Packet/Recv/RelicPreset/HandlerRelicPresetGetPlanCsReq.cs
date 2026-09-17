using March7thHoney.GameServer.Server.Packet.Send.RelicPreset;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RelicPreset;

[Opcode(CmdIds.RelicPresetGetPlanCsReq)]
public class HandlerRelicPresetGetPlanCsReq : Handler<RelicPresetGetPlanCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, RelicPresetGetPlanCsReq req)
    {
        var avatarId = req.AvatarId;
        var planList = player.AvatarManager!.GetRelicPresetPlans((int)avatarId);
        await connection.SendPacket(new PacketRelicPresetGetPlanScRsp(avatarId, planList));
    }
}
