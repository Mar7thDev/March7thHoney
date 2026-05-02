using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Challenge;

public class PacketChallengeLineupNotify : BasePacket
{
    public PacketChallengeLineupNotify(ExtraLineupType type) : base(CmdIds.ChallengeLineupNotify)
    {
        var proto = new ChallengeLineupNotify
        {
            ExtraLineupType = type
        };

        SetData(proto);
    }
}