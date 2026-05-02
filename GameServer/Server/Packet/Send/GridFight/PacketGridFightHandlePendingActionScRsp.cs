using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightHandlePendingActionScRsp : BasePacket
{
    public PacketGridFightHandlePendingActionScRsp(uint pos) : base(CmdIds.GridFightHandlePendingActionScRsp)
    {
        var proto = new GridFightHandlePendingActionScRsp
        {
            QueuePosition = pos
        };

        SetData(proto);
    }
}