using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lineup;

public class PacketExtraLineupDestroyNotify : BasePacket
{
    public PacketExtraLineupDestroyNotify(ExtraLineupType type) : base(CmdIds.ExtraLineupDestroyNotify)
    {
        var proto = new ExtraLineupDestroyNotify
        {
            ExtraLineupType = type
        };

        SetData(proto);
    }
}