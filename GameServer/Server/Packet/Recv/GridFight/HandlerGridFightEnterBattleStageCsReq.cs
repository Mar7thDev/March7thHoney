using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightEnterBattleStageCsReq)]
public class HandlerGridFightEnterBattleStageCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        _ = GridFightEnterBattleStageCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager!.GridFightInstance;
        if (inst != null)
        {
            var battleAvatarIds = inst.BuildBattleAvatarIds();
            if (battleAvatarIds.Count > 0)
            {
                var lineupIndex = player.LineupManager!.LineupData.GetCurLineupIndex();
                await player.LineupManager.ReplaceLineup(lineupIndex, battleAvatarIds);
            }
        }

        var battle = inst?.StartBattle();
        await connection.SendPacket(new PacketGridFightEnterBattleStageScRsp(battle));
    }
}
