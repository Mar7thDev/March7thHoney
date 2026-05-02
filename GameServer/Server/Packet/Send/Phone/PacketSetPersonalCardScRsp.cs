using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Phone;

public class PacketSetPersonalCardScRsp : BasePacket
{
    public PacketSetPersonalCardScRsp(uint id) : base(CmdIds.SetPersonalCardScRsp)
    {
        var proto = new SetPersonalCardScRsp
        {
            CurrentPersonalCardId = id
        };

        SetData(proto);
    }
}

