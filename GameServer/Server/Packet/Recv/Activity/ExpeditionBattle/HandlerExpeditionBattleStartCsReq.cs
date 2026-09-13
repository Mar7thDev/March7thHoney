using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.ExpeditionBattle;

[Opcode(CmdIds.ExpeditionBattleStartCsReq)]
public class HandlerExpeditionBattleStartCsReq : Handler<ExpeditionBattleStartCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, ExpeditionBattleStartCsReq req)
    {
        var retcode = player.ExpeditionBattleManager!.Start((int)req.MapId);
        await connection.SendPacket(new PacketExpeditionBattleStartScRsp(retcode));
        if (retcode == Retcode.RetSucc) await player.ExpeditionBattleManager.RunAndNotify((int)req.MapId);
    }
}
