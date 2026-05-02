using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketSyncDeleteFriendScNotify : BasePacket
{
    public PacketSyncDeleteFriendScNotify(int uid)
        : base(CmdIds.SyncDeleteFriendScNotify)
    {
        var proto = new SyncDeleteFriendScNotify
        {
            Uid = (uint)uid
        };

        SetData(proto);
    }
}