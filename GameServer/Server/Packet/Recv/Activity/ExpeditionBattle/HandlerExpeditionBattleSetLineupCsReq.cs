using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.ExpeditionBattle;

[Opcode(CmdIds.ExpeditionBattleSetLineupCsReq)]
public class HandlerExpeditionBattleSetLineupCsReq : Handler<ExpeditionBattleSetLineupCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, ExpeditionBattleSetLineupCsReq req)
    {
        var retcode = player.ExpeditionBattleManager!.SetLineup((int)req.MapId, req.LineupList);
        await connection.SendPacket(new PacketExpeditionBattleSetLineupScRsp(retcode, req));
    }
}
