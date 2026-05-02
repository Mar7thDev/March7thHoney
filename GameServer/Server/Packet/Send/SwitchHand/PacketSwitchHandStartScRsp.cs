using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.SwitchHand;

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