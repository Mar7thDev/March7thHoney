using March7thHoney.GameServer.Server.Packet.Send.Activity.B51Racing;
using March7thHoney.GameServer.Server.Packet.Send.Raid;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Raid;

[Opcode(CmdIds.StartRaidCsReq)]
public class HandlerStartRaidCsReq : Handler<StartRaidCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, StartRaidCsReq req)
    {
        if (player.B51RacingManager?.IsRacingRaid(req.RaidId) == true)
        {
            var response = player.B51RacingManager.EnterRaid(req);
            await player.B51RacingManager.SendRaidStart(response);
            await connection.SendPacket(new PacketB51RacingData(CmdIds.StartRaidScRsp, response));
            return;
        }

        var record = await player.RaidManager!.EnterRaid((int)req.RaidId, (int)req.WorldLevel,
            req.AvatarList.Select(x => (int)x).ToList(),
            req.IsSave == 1);

        if (record == null)
            await connection.SendPacket(new PacketStartRaidScRsp(Retcode.RetReqParaInvalid));
        else
            await connection.SendPacket(new PacketStartRaidScRsp(record, player));
    }
}
