using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketGetAssistListScRsp : BasePacket
{
    public PacketGetAssistListScRsp(IEnumerable<PlayerAssistInfo> rows) : base(CmdIds.GetAssistListScRsp)
    {
        var proto = new GetAssistListScRsp();
        proto.AssistList.AddRange(rows);

        SetData(proto);
    }
}
