using March7thHoney.GameServer.Server.Packet.Send.Quest;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Quest;

// 4.5v3 dump 里该 cmdid 名为 FinishClientAchievementCsReq；4.6 才拆出 FinishQuestCsReq。
[Opcode(CmdIds.FinishClientAchievementCsReq)]
public class HandlerFinishQuestCsReq : Handler<FinishQuestCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, FinishQuestCsReq req)
    {
        var retCode = await player.QuestManager!.FinishQuestByClient((int)req.QuestId);
        await connection.SendPacket(new PacketFinishQuestScRsp(retCode));
    }
}
