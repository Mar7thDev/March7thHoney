using March7thHoney.GameServer.Server.Packet.Send.Friend;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetAssistListCsReq)]
public class HandlerGetAssistListCsReq : Handler<GetAssistListCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, GetAssistListCsReq req)
    {
        // LKBAENBKJCE: the client's refresh button re-rolls the stranger pool.
        await connection.SendPacket(
            new PacketGetAssistListScRsp(player.FriendManager!.BuildAssistList(req.LKBAENBKJCE)));
    }
}
