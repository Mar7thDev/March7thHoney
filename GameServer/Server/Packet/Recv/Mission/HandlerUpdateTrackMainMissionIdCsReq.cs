using March7thHoney.GameServer.Server.Packet.Send.Mission;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.UpdateTrackMainMissionIdCsReq)]
public class HandlerUpdateTrackMainMissionIdCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = UpdateTrackMainMissionCsReq.Parser.ParseFrom(data);

        var prev = connection.Player!.MissionManager!.Data.TrackingMainMissionId;
        connection.Player!.MissionManager!.Data.TrackingMainMissionId = (int)req.TrackMissionId;

        await connection.SendPacket(new PacketUpdateTrackMainMissionIdScRsp(prev,
            connection.Player!.MissionManager!.Data.TrackingMainMissionId));
    }
}

