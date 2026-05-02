using CastoriceHoney.Server.GameServer.Server.Packet.Send.Chat;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Chat;

[Opcode(CmdIds.GetChatFriendHistoryCsReq)]
public class HandlerGetChatFriendHistoryCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var history = connection.Player!.FriendManager!.FriendData.ChatHistory;

        await connection.SendPacket(new PacketGetChatFriendHistoryScRsp(history));
    }
}