using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lineup;

public class PacketChangeLineupLeaderScRsp : BasePacket
{
    public PacketChangeLineupLeaderScRsp(uint slot) : base(CmdIds.ChangeLineupLeaderScRsp)
    {
        var proto = new ChangeLineupLeaderScRsp
        {
            Slot = slot
        };

        SetData(proto);
    }

    public PacketChangeLineupLeaderScRsp() : base(CmdIds.ChangeLineupLeaderScRsp)
    {
        var proto = new ChangeLineupLeaderScRsp
        {
            Retcode = 1
        };

        SetData(proto);
    }
}