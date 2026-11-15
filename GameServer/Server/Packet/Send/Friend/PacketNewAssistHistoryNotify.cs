using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketNewAssistHistoryNotify : BasePacket
{
    public PacketNewAssistHistoryNotify(uint pendingUnits) : base(CmdIds.NewAssistHistoryNotify)
    {
        SetData(new NewAssistHistoryNotify { OFEKHCHHOFD = pendingUnits });
    }
}
