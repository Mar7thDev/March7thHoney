using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mission;

public class PacketUpdateTrackMainMissionIdScRsp : BasePacket
{
    public PacketUpdateTrackMainMissionIdScRsp(int prev, int cur) : base(CmdIds.UpdateTrackMainMissionIdScRsp)
    {
        var proto = new UpdateTrackMainMissionScRsp
        {
            PrevTrackMissionId = (uint)prev,
            TrackMissionId = (uint)cur
        };

        SetData(proto);
    }
}

