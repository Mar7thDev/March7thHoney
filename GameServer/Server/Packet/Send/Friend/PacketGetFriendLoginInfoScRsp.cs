using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendLoginInfoScRsp : BasePacket
{
    public PacketGetFriendLoginInfoScRsp(List<int> friends) : base(CmdIds.GetFriendLoginInfoScRsp)
    {
        var proto = new GetFriendLoginInfoScRsp
        {
            FriendUidList = { friends.Select(x => (uint)x) }
        };

        SetData(proto);
    }
}