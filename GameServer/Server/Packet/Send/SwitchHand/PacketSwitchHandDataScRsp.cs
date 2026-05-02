using March7thHoney.Database.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.SwitchHand;

public class PacketSwitchHandDataScRsp : BasePacket
{
    public PacketSwitchHandDataScRsp(SwitchHandInfo info) : base(CmdIds.SwitchHandDataScRsp)
    {
        var proto = new SwitchHandDataScRsp
        {
            TargetHandInfo = { info.ToProto() }
        };

        SetData(proto);
    }

    public PacketSwitchHandDataScRsp(List<SwitchHandInfo> infos) : base(CmdIds.SwitchHandDataScRsp)
    {
        var proto = new SwitchHandDataScRsp
        {
            TargetHandInfo = { infos.Select(x => x.ToProto()) }
        };

        SetData(proto);
    }

    public PacketSwitchHandDataScRsp(Retcode code) : base(CmdIds.SwitchHandDataScRsp)
    {
        var proto = new SwitchHandDataScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}

