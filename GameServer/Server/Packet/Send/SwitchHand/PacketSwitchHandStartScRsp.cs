using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.SwitchHand;

public class PacketSwitchHandStartScRsp : BasePacket
{
    public PacketSwitchHandStartScRsp(uint configId) : base(CmdIds.SwitchHandStartScRsp)
    {
        var proto = new SwitchHandStartScRsp
        {
            ConfigId = configId
        };

        SetData(proto);
    }
}