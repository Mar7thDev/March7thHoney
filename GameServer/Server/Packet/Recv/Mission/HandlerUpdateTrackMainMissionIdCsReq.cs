using March7thHoney.GameServer.Server.Packet.Send.Mission;
using March7thHoney.Database;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.UpdateTrackMainMissionCsReq)]
public class HandlerUpdateTrackMainMissionIdCsReq : Handler<UpdateTrackMainMissionCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, UpdateTrackMainMissionCsReq req)
    {
        var missionData = player.MissionManager!.Data;
        var prev = missionData.TrackingMainMissionId;
        var target = ResolveTrackMissionId(req, missionData, prev);
        missionData.TrackingMainMissionId = target;
        DatabaseHelper.MarkDirty(player.Uid);

        await connection.SendPacket(new PacketUpdateTrackMainMissionIdScRsp(prev,
            missionData.TrackingMainMissionId));
    }

    private static int ResolveTrackMissionId(UpdateTrackMainMissionCsReq req, March7thHoney.Database.Quests.MissionData data,
        int prev)
    {
        if (req.TrackMissionId > 1) return (int)req.TrackMissionId;
        if (req.JFONDEBDIOO > 1) return (int)req.JFONDEBDIOO;
        if (prev > 0 && data.RunningMainMissionIds.Contains(prev)) return prev;
        return data.RunningMainMissionIds.Count > 0 ? data.RunningMainMissionIds[0] : 0;
    }
}
