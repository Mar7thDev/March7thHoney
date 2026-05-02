using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketSetChallengePeakBossHardModeScRsp : BasePacket
{
    public PacketSetChallengePeakBossHardModeScRsp(uint groupId, bool isHard) : base(
        CmdIds.SetChallengePeakBossHardModeScRsp)
    {
        var proto = new SetChallengePeakBossHardModeScRsp
        {
            IsHard = isHard,
            PeakGroupId = groupId
        };

        SetData(proto);
    }
}