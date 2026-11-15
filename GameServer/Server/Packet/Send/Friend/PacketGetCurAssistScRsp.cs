using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketGetCurAssistScRsp : BasePacket
{
    // Always carries the info message, empty when nothing is selected.
    public PacketGetCurAssistScRsp(PlayerAssistInfo info) : base(CmdIds.GetCurAssistScRsp)
    {
        SetData(new GetCurAssistScRsp { ODHMGPNECFO = info });
    }
}
