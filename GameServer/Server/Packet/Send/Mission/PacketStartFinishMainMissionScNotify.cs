using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mission;

public class PacketStartFinishMainMissionScNotify : BasePacket
{
    public PacketStartFinishMainMissionScNotify(int missionId) : base(CmdIds.StartFinishMainMissionScNotify)
    {
        var proto = new StartFinishMainMissionScNotify
        {
            MainMissionId = (uint)missionId
        };

        SetData(proto);
    }
}