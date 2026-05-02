using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketChallengePeakGroupDataUpdateScNotify : BasePacket
{
    public PacketChallengePeakGroupDataUpdateScNotify(ChallengePeakLevelInfo info) : base(
        CmdIds.ChallengePeakGroupDataUpdateScNotify)
    {
        var proto = new ChallengePeakGroupDataUpdateScNotify
        {
            UpdatePeakData = info
        };

        SetData(proto);
    }
}