using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChallengePeak;

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