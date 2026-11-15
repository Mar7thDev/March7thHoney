using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Lineup;

[Opcode(CmdIds.ReplaceLineupCsReq)]
public class HandlerReplaceLineupCsReq : Handler<ReplaceLineupCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, ReplaceLineupCsReq req)
    {
        // The slot list carries assist_uid for a support character; the full overload keeps it.
        await player.LineupManager!.ReplaceLineup(req);
        await connection.SendPacket(CmdIds.JoinLineupScRsp);
    }
}
