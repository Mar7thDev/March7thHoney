using March7thHoney.GameServer.Server.Packet.Send.RelicPreset;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RelicPreset;

[Opcode(CmdIds.RelicPresetUpdatePlanCsReq)]
public class HandlerRelicPresetUpdatePlanCsReq : Handler<RelicPresetUpdatePlanCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, RelicPresetUpdatePlanCsReq req)
    {
        var updated = player.AvatarManager!.UpdateRelicPresetPlan((int)req.UniqueId, req.RelicList, req.Name);
        await connection.SendPacket(new PacketRelicPresetUpdatePlanScRsp(req,
            updated ? Retcode.RetSucc : Retcode.RetFail));
    }
}
