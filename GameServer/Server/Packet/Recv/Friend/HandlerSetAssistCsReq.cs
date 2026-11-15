using March7thHoney.GameServer.Server.Packet.Send.Friend;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.SetAssistCsReq)]
public class HandlerSetAssistCsReq : Handler<SetAssistCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, SetAssistCsReq req)
    {
        var retcode = await player.FriendManager!.SetAssist((int)req.Uid, (int)req.AvatarId);
        await connection.SendPacket(new PacketSetAssistScRsp(retcode, req.Uid, req.AvatarId));
    }
}
