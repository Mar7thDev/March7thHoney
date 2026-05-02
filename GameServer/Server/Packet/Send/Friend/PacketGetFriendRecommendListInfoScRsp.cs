using CastoriceHoney.Server.Database.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendRecommendListInfoScRsp : BasePacket
{
    public PacketGetFriendRecommendListInfoScRsp(List<PlayerData> friends)
        : base(CmdIds.GetFriendRecommendListInfoScRsp)
    {
        var proto = new GetFriendRecommendListInfoScRsp
        {
            FriendRecommendList =
            {
                friends.Select(x => new FriendRecommendInfo
                {
                    PlayerInfo = x.ToSimpleProto(FriendOnlineStatus.Online)
                })
            }
        };

        SetData(proto);
    }
}

