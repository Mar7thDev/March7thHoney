using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mail;

public class PacketDelMailScRsp : BasePacket
{
    public PacketDelMailScRsp(List<uint> ids) : base(CmdIds.DelMailScRsp)
    {
        var proto = new DelMailScRsp
        {
            IdList = { ids }
        };

        SetData(proto);
    }
}