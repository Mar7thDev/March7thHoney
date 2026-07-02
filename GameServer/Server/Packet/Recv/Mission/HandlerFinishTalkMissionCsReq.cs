using March7thHoney.GameServer.Server.Packet.Send.Mission;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.FinishTalkMissionCsReq)]
public class HandlerFinishTalkMissionCsReq : Handler<FinishTalkMissionCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, FinishTalkMissionCsReq req)
    {
        await player.MissionManager!.HandleTalkStr(req.TalkStr);

        if (req.CustomValueList != null && req.CustomValueList.Count > 0)
            await player.MissionManager!.HandleCustomValue([.. req.CustomValueList], (int)req.SubMissionId);

        await connection.SendPacket(new PacketFinishTalkMissionScRsp(req.TalkStr));
    }
}
