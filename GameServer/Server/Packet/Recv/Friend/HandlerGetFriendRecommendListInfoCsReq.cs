using CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendRecommendListInfoCsReq)]
public class HandlerGetFriendRecommendListInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var friends = connection.Player!.FriendManager!.GetRandomFriend();

        await connection.SendPacket(new PacketGetFriendRecommendListInfoScRsp(friends));
    }
}