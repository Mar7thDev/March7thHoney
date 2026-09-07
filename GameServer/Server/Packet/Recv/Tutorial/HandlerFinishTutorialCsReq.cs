using March7thHoney.GameServer.Game.Tutorial;
using March7thHoney.GameServer.Server.Packet.Send.Tutorial;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.FinishTutorialCsReq)]
public class HandlerFinishTutorialCsReq : Handler<FinishTutorialCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, FinishTutorialCsReq req)
    {
        // Persist FINISH even when the unlock was never recorded (client-driven tutorials may finish first).
        TutorialHelper.RecordTutorial(player, (int)req.TutorialId, TutorialStatus.TutorialFinish);

        await connection.SendPacket(new PacketFinishTutorialScRsp(req.TutorialId));
    }
}
