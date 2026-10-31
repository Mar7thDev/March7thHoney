using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendAssistListScRsp : BasePacket
{
    public PacketGetFriendAssistListScRsp(IEnumerable<PlayerAssistInfo> rows, uint targetSide)
        : base(CmdIds.GetFriendAssistListScRsp)
    {
        var proto = new GetFriendAssistListScRsp { TargetSide = targetSide };
        proto.AssistList.AddRange(rows);

        SetData(proto);
    }
}
