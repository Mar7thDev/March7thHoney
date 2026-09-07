using March7thHoney.GameServer.Game.Tutorial;
using March7thHoney.GameServer.Server.Packet.Send.Tutorial;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.UnlockTutorialCsReq)]
public class HandlerUnlockTutorialCsReq : Handler<UnlockTutorialCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, UnlockTutorialCsReq req)
    {
        var tutorialId = (int)req.TutorialId;

        // Input-lock guard: a tutorial whose finish trigger the server cannot guarantee (EnterBattle-only, an
        // unreachable FinishMainMission, an empty finish list without AutoFinish) is closed on arrival so it never
        // grabs input. Everything else opens and is persisted as UNLOCK.
        var status = TutorialHelper.CanClose(player, tutorialId)
            ? TutorialStatus.TutorialUnlock
            : TutorialStatus.TutorialFinish;
        TutorialHelper.RecordTutorial(player, tutorialId, status);

        await connection.SendPacket(new PacketUnlockTutorialScRsp(req.TutorialId,
            player.TutorialData!.Tutorials.GetValueOrDefault(tutorialId, status)));
    }
}
