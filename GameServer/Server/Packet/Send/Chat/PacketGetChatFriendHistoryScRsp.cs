using CastoriceHoney.Server.Database.Friend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Chat;

public class PacketGetChatFriendHistoryScRsp : BasePacket
{
    public PacketGetChatFriendHistoryScRsp(Dictionary<int, FriendChatHistory> history)
        : base(CmdIds.GetChatFriendHistoryScRsp)
    {
        var proto = new GetChatFriendHistoryScRsp();

        foreach (var item in history)
            proto.FriendHistoryInfo.Add(new FriendHistoryInfo
            {
                ContactSide = (uint)item.Key,
                LastSendTime = item.Value.MessageList.Last().SendTime
            });

        SetData(proto);
    }
}