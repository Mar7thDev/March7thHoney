using March7thHoney.GameServer.Server.Packet.Send.RelicPreset;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RelicPreset;

/// <summary>
///     同 <see cref="HandlerRelicPresetGetPlanCsReq" />：4.5v3 dump 里这条请求叫 DCJGPJGJFIF，
///     对应 4.5 dump 的 cmdid 9849 = RelicPresetDeletePlanCsReq，唯一字段 JJDOFLLPDJE 即 unique_id。
/// </summary>
[Opcode(CmdIds.RelicPresetDeletePlanCsReq)]
public class HandlerRelicPresetDeletePlanCsReq : Handler<DCJGPJGJFIF>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, DCJGPJGJFIF req)
    {
        var uniqueId = req.JJDOFLLPDJE;
        var deleted = player.AvatarManager!.DeleteRelicPresetPlan((int)uniqueId);
        await connection.SendPacket(new PacketRelicPresetDeletePlanScRsp(uniqueId,
            deleted ? Retcode.RetSucc : Retcode.RetFail));
    }
}
