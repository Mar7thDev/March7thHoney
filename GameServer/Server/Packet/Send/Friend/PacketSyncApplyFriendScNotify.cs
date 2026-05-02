using CastoriceHoney.Server.Database.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketSyncApplyFriendScNotify : BasePacket
{
    public PacketSyncApplyFriendScNotify(PlayerData player) : base(CmdIds.SyncApplyFriendScNotify)
    {
        var proto = new SyncApplyFriendScNotify
        {
            FriendApplyInfo = new FriendApplyInfo
            {
                ApplyTime = Extensions.GetUnixSec(),
                PlayerInfo = player.ToSimpleProto(FriendOnlineStatus.Online)
            }
        };

        SetData(proto);
    }
}

