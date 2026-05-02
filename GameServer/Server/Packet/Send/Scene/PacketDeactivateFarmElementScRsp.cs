using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketDeactivateFarmElementScRsp : BasePacket
{
    public PacketDeactivateFarmElementScRsp(uint id) : base(CmdIds.DeactivateFarmElementScRsp)
    {
        var proto = new DeactivateFarmElementScRsp
        {
            EntityId = id
        };

        SetData(proto);
    }
}