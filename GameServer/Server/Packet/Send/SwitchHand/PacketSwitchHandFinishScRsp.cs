using CastoriceHoney.Server.Database.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.SwitchHand;

public class PacketSwitchHandFinishScRsp : BasePacket
{
    public PacketSwitchHandFinishScRsp(SwitchHandInfo info) : base(CmdIds.SwitchHandFinishScRsp)
    {
        var proto = new SwitchHandFinishScRsp
        {
            OMHAENBIKCN = info.ToProto()
        };

        SetData(proto);
    }

    public PacketSwitchHandFinishScRsp(Retcode ret) : base(CmdIds.SwitchHandFinishScRsp)
    {
        var proto = new SwitchHandFinishScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}

