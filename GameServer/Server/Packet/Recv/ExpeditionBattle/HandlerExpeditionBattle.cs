using March7thHoney.GameServer.Server.Packet.Send.ExpeditionBattle;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.ExpeditionBattle;

[Opcode(CmdIds.GetExpeditionBattleDataCsReq)]
public class HandlerGetExpeditionBattleDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketGetExpeditionBattleDataScRsp(
            player.ExpeditionBattleManager!.BuildGetDataResponse()));
    }
}

[Opcode(CmdIds.ExpeditionBattleSetLineupCsReq)]
public class HandlerExpeditionBattleSetLineupCsReq : Handler<ExpeditionBattleSetLineupCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        ExpeditionBattleSetLineupCsReq req)
    {
        var retcode = player.ExpeditionBattleManager!.SetLineup(req);
        await connection.SendPacket(new PacketExpeditionBattleSetLineupScRsp(req, retcode));
    }
}

[Opcode(CmdIds.ExpeditionBattleStartCsReq)]
public class HandlerExpeditionBattleStartCsReq : Handler<ExpeditionBattleStartCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, ExpeditionBattleStartCsReq req)
    {
        var manager = player.ExpeditionBattleManager!;
        if (!manager.TryStart(req.MapId))
        {
            await connection.SendPacket(new PacketExpeditionBattleStartScRsp(Retcode.RetReqParaInvalid));
            return;
        }

        for (var levelIndex = 0; levelIndex < manager.GetLevelCount(req.MapId); levelIndex++)
        {
            await connection.SendPacket(
                new PacketExpeditionBattleSettleScNotify(manager.BuildSettleNotify(req.MapId, levelIndex)));
        }

        manager.Complete(req.MapId);
        await connection.SendPacket(new PacketExpeditionBattleEndScNotify(manager.BuildEndNotify()));
        await connection.SendPacket(new PacketExpeditionBattleStartScRsp());
    }
}
