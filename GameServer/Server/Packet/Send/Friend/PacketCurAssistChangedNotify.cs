using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketCurAssistChangedNotify : BasePacket
{
    public PacketCurAssistChangedNotify(PlayerAssistInfo info) : base(CmdIds.CurAssistChangedNotify)
    {
        SetData(new CurAssistChangedNotify { ODHMGPNECFO = info });
    }
}
