using March7thHoney.GameServer.Game.Tutorial;
using March7thHoney.GameServer.Server.Packet.Send.Tutorial;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.FinishTutorialGuideCsReq)]
public class HandlerFinishTutorialGuideCsReq : Handler<FinishTutorialGuideCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, FinishTutorialGuideCsReq req)
    {
        var groupId = (int)req.GroupId;
        var alreadyFinished = player.TutorialGuideData!.Tutorials.GetValueOrDefault(groupId) ==
                              TutorialStatus.TutorialFinish;

        // The first finish of a guide pays the 1-jade reward (official part1.json 667-698), whether or not the
        // unlock was recorded before.
        if (!alreadyFinished) await player.InventoryManager!.AddItem(1, 1, false);
        TutorialHelper.RecordGuide(player, groupId, TutorialStatus.TutorialFinish, req.Type);

        await connection.SendPacket(new PacketFinishTutorialGuideScRsp(req.GroupId,
            TutorialHelper.GuideTypeOf(player, groupId)));
    }
}
