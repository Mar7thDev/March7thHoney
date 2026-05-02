using CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.AddBlacklistCsReq)]
public class HandlerAddBlacklistCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = AddBlacklistCsReq.Parser.ParseFrom(data);

        var player = await connection.Player!.FriendManager!.AddBlackList((int)req.Uid);

        if (player != null)
            await connection.SendPacket(new PacketAddBlacklistScRsp(player));
        else
            await connection.SendPacket(new PacketAddBlacklistScRsp());
    }
}