using March7thHoney.GameServer.Server.Packet.Send.Activity.B51Racing;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Raid;

[Opcode(CmdIds.LeaveRaidCsReq)]
public class HandlerLeaveRaidCsReq : Handler<LeaveRaidCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, LeaveRaidCsReq req)
    {
        if (player.B51RacingManager?.IsRacingRaid(req.RaidId) == true)
        {
            await player.B51RacingManager.LeaveRaid();
            await connection.SendPacket(new PacketB51RacingData(CmdIds.LeaveRaidScRsp, new LeaveRaidScRsp { RaidId = req.RaidId }));
            return;
        }

        await player.RaidManager!.LeaveRaid(req.IsSave);

        await connection.SendPacket(CmdIds.LeaveRaidScRsp);
    }
}
