using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Phone;

public class PacketSelectPhoneCaseScRsp : BasePacket
{
    public PacketSelectPhoneCaseScRsp(uint id) : base(CmdIds.SelectPhoneCaseScRsp)
    {
        var proto = new SelectPhoneCaseScRsp
        {
            CurPhoneCase = id
        };

        SetData(proto);
    }
}