using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Battle;

[Opcode(CmdIds.QuitBattleCsReq)]
public class HandlerQuitBattleCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var battle = player.BattleInstance;
        player.BattleInstance = null;
        // Without this the GridFight session stays InBattle and EnterBattle can never succeed again.
        if (battle?.GridFightContext != null && player.GridFightManager != null)
            await player.GridFightManager.QuitBattle(battle.GridFightContext);
        await connection.SendPacket(CmdIds.QuitBattleScRsp);
    }
}
