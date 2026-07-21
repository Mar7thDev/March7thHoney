using March7thHoney.GameServer.Server.Packet.Send.B51Racing;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.B51Racing;

[Opcode(CmdIds.B51RacingGetDataCsReq)]
public class HandlerB51RacingGetDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingGetDataScRsp,
            player.B51RacingManager!.BuildGetDataResponse()));
    }
}

[Opcode(CmdIds.B51RacingStartCycleCsReq)]
public class HandlerB51RacingStartCycleCsReq : Handler<B51RacingStartCycleCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingStartCycleCsReq req)
    {
        var manager = player.B51RacingManager!;
        var response = manager.StartCycle(req);
        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingStartCycleScRsp, response));
        if (response.Retcode == 0)
            await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingDriverUnlockScNotify,
                manager.BuildDriverUnlockNotify()));
    }
}

[Opcode(CmdIds.B51RacingCarLiveryCsReq)]
public class HandlerB51RacingCarLiveryCsReq : Handler<B51RacingCarLiveryCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingCarLiveryCsReq req)
    {
        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingCarLiveryScRsp,
            player.B51RacingManager!.SetLivery(req)));
    }
}

[Opcode(CmdIds.B51RacingSetDriverCsReq)]
public class HandlerB51RacingSetDriverCsReq : Handler<B51RacingSetDriverCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingSetDriverCsReq req)
    {
        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingSetDriverScRsp,
            player.B51RacingManager!.SetDriver(req)));
    }
}

[Opcode(CmdIds.B51RacingStartMatchCsReq)]
public class HandlerB51RacingStartMatchCsReq : Handler<B51RacingStartMatchCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingStartMatchCsReq req)
    {
        var manager = player.B51RacingManager!;
        if (!await manager.EnterRacingScene(req.TrackId))
        {
            await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingStartMatchScRsp,
                new B51RacingStartMatchScRsp { Retcode = (uint)Retcode.RetNotInCorrectScene }));
            return;
        }

        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingStartMatchScRsp,
            manager.StartMatch(req)));
    }
}

[Opcode(CmdIds.B51RacingEndMatchCsReq)]
public class HandlerB51RacingEndMatchCsReq : Handler<B51RacingEndMatchCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingEndMatchCsReq req)
    {
        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingEndMatchScRsp,
            player.B51RacingManager!.EndMatch(req)));
    }
}

[Opcode(CmdIds.B51RacingReplacePartCsReq)]
public class HandlerB51RacingReplacePartCsReq : Handler<B51RacingReplacePartCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingReplacePartCsReq req)
    {
        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingReplacePartScRsp,
            player.B51RacingManager!.ReplaceParts(req)));
    }
}

[Opcode(CmdIds.B51RacingSelectDevelopPartCsReq)]
public class HandlerB51RacingSelectDevelopPartCsReq : Handler<B51RacingSelectDevelopPartCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        B51RacingSelectDevelopPartCsReq req)
    {
        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingSelectDevelopPartScRsp,
            player.B51RacingManager!.SelectDevelopPart(req)));
    }
}

[Opcode(CmdIds.B51RacingSelectDevelopActionCsReq)]
public class HandlerB51RacingSelectDevelopActionCsReq : Handler<B51RacingSelectDevelopActionCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        B51RacingSelectDevelopActionCsReq req)
    {
        await connection.SendPacket(new PacketB51Racing(CmdIds.B51RacingSelectDevelopActionScRsp,
            player.B51RacingManager!.SelectDevelopAction(req)));
    }
}
