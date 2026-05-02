using CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.SetFriendMarkCsReq)]
public class HandlerSetFriendMarkCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SetFriendMarkCsReq.Parser.ParseFrom(data);

        connection.Player!.FriendManager!.MarkFriend((int)req.Uid, req.KBNKNAHGPHG);

        await connection.SendPacket(new PacketSetFriendMarkScRsp(req.Uid, req.KBNKNAHGPHG));
    }
}

