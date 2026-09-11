using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightGetFormationCsReq)]
public sealed class HandlerGridFightGetFormationCsReq : PlayerHandler
{
    protected override Task OnHandle(Connection connection, PlayerInstance player)
    {
        return connection.SendPacket(GridFightPacketFactory.CreatePacket(
            CmdIds.GetGameFormationScRsp,
            new GetGameFormationScRsp()));
    }
}

[Opcode(CmdIds.GridFightFinishTutorialCsReq)]
public sealed class HandlerGridFightFinishTutorialCsReq : PlayerHandler
{
    protected override Task OnHandle(Connection connection, PlayerInstance player)
    {
        return Task.CompletedTask;
    }
}

[Opcode(CmdIds.MCDBJHOCGHI)]
public sealed class HandlerGridFightTraitTrackCsReq : Handler<MCDBJHOCGHI>
{
    protected override async Task OnHandle(
        Connection connection,
        PlayerInstance player,
        MCDBJHOCGHI request)
    {
        var manager = player.GridFightManager!;
        var retcode = manager.Session?.UpdateTraitTrack(request.TraitId, request.BJDOINAHDLN) ??
                      (uint)Retcode.RetFail;
        if (retcode == 0)
            await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
                manager.BuildTraitTrackSync()));
        await connection.SendPacket(GridFightPacketFactory.CreatePacket(
            CmdIds.IIFPPBCFBBC,
            new IIFPPBCFBBC { Retcode = retcode }));
    }
}
