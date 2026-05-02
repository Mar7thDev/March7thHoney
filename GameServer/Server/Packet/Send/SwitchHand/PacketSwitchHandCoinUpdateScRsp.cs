using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.SwitchHand;

public class PacketSwitchHandCoinUpdateScRsp : BasePacket
{
    public PacketSwitchHandCoinUpdateScRsp(Retcode ret) : base(CmdIds.SwitchHandCoinUpdateScRsp)
    {
        var proto = new SwitchHandCoinUpdateScRsp
        {
            Retcode = (uint)ret
        };
        SetData(proto);
    }

    public PacketSwitchHandCoinUpdateScRsp(uint coinNum) : base(CmdIds.SwitchHandCoinUpdateScRsp)
    {
        var proto = new SwitchHandCoinUpdateScRsp
        {
            CKNFABPOMBL = coinNum
        };
        SetData(proto);
    }
}

