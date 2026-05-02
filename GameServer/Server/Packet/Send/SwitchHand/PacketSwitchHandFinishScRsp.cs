using March7thHoney.Database.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.SwitchHand;

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

