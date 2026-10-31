using March7thHoney.GameServer.Server.Packet.Send.Friend;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendAssistListCsReq)]
public class HandlerGetFriendAssistListCsReq : Handler<GetFriendAssistListCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, GetFriendAssistListCsReq req)
    {
        var rows = player.FriendManager!.BuildFriendAssistList((int)req.TargetSide);
        await connection.SendPacket(new PacketGetFriendAssistListScRsp(rows, req.TargetSide));
    }
}
