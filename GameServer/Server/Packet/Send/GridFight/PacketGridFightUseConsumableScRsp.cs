using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightUseConsumableScRsp : BasePacket
{
    public PacketGridFightUseConsumableScRsp(Retcode ret) : base(CmdIds.GridFightUseConsumableScRsp)
    {
        var proto = new GridFightUseConsumableScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}