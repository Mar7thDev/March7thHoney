using March7thHoney.GameServer.Server.Packet.Send.RelicPreset;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RelicPreset;

[Opcode(CmdIds.RelicPresetDeletePlanCsReq)]
public class HandlerRelicPresetDeletePlanCsReq : Handler<RelicPresetDeletePlanCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, RelicPresetDeletePlanCsReq req)
    {
        var uniqueId = req.ONIAFLNBNPN;
        var deleted = player.AvatarManager!.DeleteRelicPresetPlan((int)uniqueId);
        await connection.SendPacket(new PacketRelicPresetDeletePlanScRsp(uniqueId,
            deleted ? Retcode.RetSucc : Retcode.RetFail));
    }
}
