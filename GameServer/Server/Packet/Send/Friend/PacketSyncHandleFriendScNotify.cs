using CastoriceHoney.Server.Database.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketSyncHandleFriendScNotify : BasePacket
{
    public PacketSyncHandleFriendScNotify(uint uid, bool isAccept, PlayerData playerData) : base(
        CmdIds.SyncHandleFriendScNotify)
    {
        var status = Listener.GetActiveConnection((int)uid) == null
            ? FriendOnlineStatus.Offline
            : FriendOnlineStatus.Online;
        var proto = new SyncHandleFriendScNotify
        {
            Uid = uid,
            IsAccept = isAccept,
            FriendInfo = new FriendSimpleInfo
            {
                PlayerInfo = playerData.ToSimpleProto(status)
            }
        };

        SetData(proto);
    }
}