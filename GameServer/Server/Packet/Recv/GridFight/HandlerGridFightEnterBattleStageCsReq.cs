using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightEnterBattleStageCsReq)]
public class HandlerGridFightEnterBattleStageCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var gridFight = connection.Player?.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(
                new PacketGridFightEnterBattleStageScRsp(Retcode.RetGridFightNotInGameplay));
            return;
        }

        var battle = gridFight.StartBattle();
        await connection.SendPacket(
            new PacketGridFightEnterBattleStageScRsp(Retcode.RetSucc, battle));
    }
}