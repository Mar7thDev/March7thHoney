using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Raid;

[Opcode(CmdIds.LeaveRaidCsReq)]
public class HandlerLeaveRaidCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;
        var req = LeaveRaidCsReq.Parser.ParseFrom(data);
        await player.RaidManager!.LeaveRaid(req.IsSave);

        await connection.SendPacket(CmdIds.LeaveRaidScRsp);
    }
}