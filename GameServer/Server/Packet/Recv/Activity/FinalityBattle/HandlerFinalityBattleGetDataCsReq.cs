using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Activity.FinalityBattle;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.FinalityBattle;

[Opcode(CmdIds.FinalityBattleGetDataCsReq)]
public class HandlerFinalityBattleGetDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketFinalityBattleGetDataScRsp(player));
    }
}
