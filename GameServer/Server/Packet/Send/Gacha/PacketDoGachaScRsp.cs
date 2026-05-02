using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Gacha;

public class PacketDoGachaScRsp : BasePacket
{
    public PacketDoGachaScRsp(DoGachaScRsp rsp) : base(CmdIds.DoGachaScRsp)
    {
        SetData(rsp);
    }

    public PacketDoGachaScRsp() : base(CmdIds.DoGachaScRsp)
    {
        var rsp = new DoGachaScRsp
        {
            Retcode = 1
        };
        SetData(rsp);
    }
}