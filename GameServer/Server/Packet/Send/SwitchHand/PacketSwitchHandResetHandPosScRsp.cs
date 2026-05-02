using March7thHoney.Database.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.SwitchHand;

public class PacketSwitchHandResetHandPosScRsp : BasePacket
{
    public PacketSwitchHandResetHandPosScRsp(SwitchHandInfo info) : base(CmdIds.SwitchHandResetHandPosScRsp)
    {
        var proto = new SwitchHandResetHandPosScRsp
        {
            TargetHandInfo = info.ToProto()
        };

        SetData(proto);
    }

    public PacketSwitchHandResetHandPosScRsp(Retcode ret) : base(CmdIds.SwitchHandResetHandPosScRsp)
    {
        var proto = new SwitchHandResetHandPosScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}

