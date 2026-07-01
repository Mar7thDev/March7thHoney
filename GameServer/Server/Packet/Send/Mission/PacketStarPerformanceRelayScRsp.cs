using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketStarPerformanceRelayScRsp : BasePacket
{
    public PacketStarPerformanceRelayScRsp() : base(CmdIds.StarPerformanceRelayScRsp)
    {
        SetData(new StarPerformanceRelayScRsp());
    }
}
