using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lineup;

public class PacketSwitchLineupIndexScRsp : BasePacket
{
    public PacketSwitchLineupIndexScRsp(uint index) : base(CmdIds.SwitchLineupIndexScRsp)
    {
        var proto = new SwitchLineupIndexScRsp
        {
            Index = index
        };

        SetData(proto);
    }
}