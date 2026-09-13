using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.ExpeditionBattle;

[Opcode(CmdIds.GetExpeditionBattleDataCsReq)]
public class HandlerGetExpeditionBattleDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketGetExpeditionBattleDataScRsp(player));
    }
}
