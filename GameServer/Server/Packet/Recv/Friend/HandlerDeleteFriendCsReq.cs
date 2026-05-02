using CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.DeleteFriendCsReq)]
public class HandlerDeleteFriendCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = DeleteFriendCsReq.Parser.ParseFrom(data);

        var uid = await connection.Player!.FriendManager!.RemoveFriend((int)req.Uid);
        if (uid == null)
            await connection.SendPacket(new PacketDeleteFriendScRsp());
        else
            await connection.SendPacket(new PacketDeleteFriendScRsp((uint)uid));
    }
}