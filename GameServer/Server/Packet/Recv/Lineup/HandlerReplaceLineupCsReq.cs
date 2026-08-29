using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Lineup;

// 4.5v3 dump 里这个包还叫 JoinLineupCsReq(768)，4.6 才更名为 ReplaceLineup。
[Opcode(CmdIds.JoinLineupCsReqCmd768)]
public class HandlerReplaceLineupCsReq : Handler<JoinLineupCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, JoinLineupCsReq req)
    {
        var avatarIds = req.LineupSlotList
            .OrderBy(x => x.Slot)
            .Select(x => (int)x.Id)
            .Where(x => x > 0)
            .ToList();
        await player.LineupManager!.ReplaceLineup((int)req.Index, avatarIds, req.ExtraLineupType);
        await connection.SendPacket(CmdIds.JoinLineupScRsp);
    }
}
