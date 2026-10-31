using March7thHoney.Database;
using March7thHoney.Database.Friend;
using March7thHoney.GameServer.Server.Packet.Send.Friend;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendBattleRecordDetailCsReq)]
public class HandlerGetFriendBattleRecordDetailCsReq : Handler<GetFriendBattleRecordDetailCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, GetFriendBattleRecordDetailCsReq req)
    {
        // A player without records gets an empty list, not an error.
        var recordData = DatabaseHelper.Instance!.GetInstance<FriendRecordData>((int)req.Uid);
        await connection.SendPacket(new PacketGetFriendBattleRecordDetailScRsp((int)req.Uid, recordData));
    }
}
