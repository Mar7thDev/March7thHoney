using March7thHoney.GameServer.Server.Packet.Send.Mission;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.FinishTalkMissionCsReq)]
public class HandlerFinishTalkMissionCsReq : Handler<FinishTalkMissionCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, FinishTalkMissionCsReq req)
    {
        var hasCustomValues = req.CustomValueList != null && req.CustomValueList.Count > 0;
        var resolvedSubId = (int)req.SubMissionId;
        if (resolvedSubId <= 0 && !string.IsNullOrEmpty(req.TalkStr))
            resolvedSubId = player.MissionManager!.GetRunningSubMissionList()
                .FirstOrDefault(m => m.FinishType == Enums.Mission.MissionFinishTypeEnum.Talk &&
                                     m.ParamStr1 == req.TalkStr)?.ID ?? 0;

        // A talk string that matches no gate (e.g. a plain NPC dialogue reporting "0") is ACK-only, as on the
        // official server: finishing a gate the client never reported tears down its running performance.
        await player.MissionManager!.HandleTalkStr(req.TalkStr);

        if (hasCustomValues && resolvedSubId > 0)
            await player.MissionManager!.HandleCustomValue([.. req.CustomValueList!], resolvedSubId);

        await connection.SendPacket(new PacketFinishTalkMissionScRsp(req.TalkStr));
    }
}
