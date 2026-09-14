using March7thHoney.GameServer.Server.Packet.Send.Activity.B51Racing;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.B51Racing;

[Opcode(CmdIds.B51RacingGetDataCsReq)]
public sealed class HandlerB51RacingGetDataCsReq : Handler<B51RacingGetDataCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingGetDataCsReq request)
    {
        await connection.SendPacket(new PacketB51RacingData(CmdIds.B51RacingGetDataScRsp, player.B51RacingManager!.ToProto()));
    }
}

[Opcode(CmdIds.B51RacingChallengeUpdateCarCsReq)]
public sealed class HandlerB51RacingChallengeUpdateCarCsReq : Handler<B51RacingChallengeUpdateCarCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingChallengeUpdateCarCsReq request)
    {
        await connection.SendPacket(new PacketB51RacingData(CmdIds.B51RacingChallengeUpdateCarScRsp, player.B51RacingManager!.UpdateCar(request)));
    }
}

[Opcode(CmdIds.B51RacingSetDriverCsReq)]
public sealed class HandlerB51RacingSetDriverCsReq : Handler<B51RacingSetDriverCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingSetDriverCsReq request)
    {
        await connection.SendPacket(new PacketB51RacingData(CmdIds.B51RacingSetDriverScRsp, player.B51RacingManager!.SetDriver(request.DriverId)));
    }
}

[Opcode(CmdIds.B51RacingCarLiveryCsReq)]
public sealed class HandlerB51RacingCarLiveryCsReq : Handler<B51RacingCarLiveryCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingCarLiveryCsReq request)
    {
        await connection.SendPacket(new PacketB51RacingData(CmdIds.B51RacingCarLiveryScRsp, player.B51RacingManager!.SetLivery(request.Livery)));
    }
}

[Opcode(CmdIds.B51RacingChallengeStartMatchCsReq)]
public sealed class HandlerB51RacingChallengeStartMatchCsReq : Handler<B51RacingChallengeStartMatchCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingChallengeStartMatchCsReq request)
    {
        await connection.SendPacket(new PacketB51RacingData(CmdIds.B51RacingChallengeStartMatchScRsp, player.B51RacingManager!.StartMatch(request)));
    }
}

[Opcode(CmdIds.B51RacingChallengeEndMatchCsReq)]
public sealed class HandlerB51RacingChallengeEndMatchCsReq : Handler<B51RacingChallengeEndMatchCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, B51RacingChallengeEndMatchCsReq request)
    {
        await connection.SendPacket(new PacketB51RacingData(CmdIds.B51RacingChallengeEndMatchScRsp, player.B51RacingManager!.EndMatch(request)));
    }
}
