using March7thHoney.Database.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.SwitchHand;

public class PacketSwitchHandUpdateScRsp : BasePacket
{
    public PacketSwitchHandUpdateScRsp(SwitchHandInfo info, HandOperationInfo? operationInfo) : base(
        CmdIds.SwitchHandUpdateScRsp)
    {
        var proto = new SwitchHandUpdateScRsp
        {
            HandInfo = info.ToProto(),
            HandOperationInfo = operationInfo ?? new HandOperationInfo()
        };
        SetData(proto);
    }

    public PacketSwitchHandUpdateScRsp(Retcode ret, HandOperationInfo? operationInfo) : base(
        CmdIds.SwitchHandUpdateScRsp)
    {
        var proto = new SwitchHandUpdateScRsp
        {
            Retcode = (uint)ret,
            HandOperationInfo = operationInfo ?? new HandOperationInfo()
        };

        SetData(proto);
    }
}