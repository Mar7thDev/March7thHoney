using March7thHoney.GameServer.Game.Tutorial;
using March7thHoney.GameServer.Server.Packet.Send.Tutorial;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.UnlockTutorialGuideCsReq)]
public class HandlerUnlockTutorialGuideCsReq : Handler<UnlockTutorialGuideCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, UnlockTutorialGuideCsReq req)
    {
        TutorialHelper.RecordGuide(player, (int)req.GroupId, TutorialStatus.TutorialUnlock, req.Type);

        await connection.SendPacket(new PacketUnlockTutorialGuideScRsp(req.GroupId,
            player.TutorialGuideData!.Tutorials.GetValueOrDefault((int)req.GroupId, TutorialStatus.TutorialUnlock),
            TutorialHelper.GuideTypeOf(player, (int)req.GroupId)));
    }
}
