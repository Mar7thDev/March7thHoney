using March7thHoney.GameServer.Server.Packet.Send.RelicPreset;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RelicPreset;

/// <summary>
///     打开智能装配时客户端要方案列表。4.5v3 的 dump 没能还原这条请求的名字，它在协议里叫 PCFJAHNECFM；
///     按 4.5 dump 里同名消息（cmdid 9845 = RelicPresetGetPlanCsReq）对上，唯一字段 KOEOEHIMOEI 即 avatar_id。
///     没有这个处理器时服务端一个字节都不回（名字不含 CsReq，Connection 的按名兜底应答也不会触发），
///     客户端就一直等着 —— 表现就是点了智能装配没反应。
/// </summary>
[Opcode(CmdIds.RelicPresetGetPlanCsReq)]
public class HandlerRelicPresetGetPlanCsReq : Handler<PCFJAHNECFM>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, PCFJAHNECFM req)
    {
        var avatarId = req.KOEOEHIMOEI;
        var planList = player.AvatarManager!.GetRelicPresetPlans((int)avatarId);
        await connection.SendPacket(new PacketRelicPresetGetPlanScRsp(avatarId, planList));
    }
}
