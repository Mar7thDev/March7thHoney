using March7thHoney.Database.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.SwitchHand;

public class PacketSwitchHandResetGameScRsp : BasePacket
{
    public PacketSwitchHandResetGameScRsp(SwitchHandInfo info) : base(CmdIds.SwitchHandResetGameScRsp)
    {
        var proto = new SwitchHandResetGameScRsp
        {
            TargetHandInfo = info.ToProto()
        };

        SetData(proto);
    }

    public PacketSwitchHandResetGameScRsp(Retcode ret) : base(CmdIds.SwitchHandResetGameScRsp)
    {
        var proto = new SwitchHandResetGameScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}

