using March7thHoney.Database;
using March7thHoney.Database.Friend;
using March7thHoney.GameServer.Server.Packet.Send.Friend;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendDevelopmentInfoCsReq)]
public class HandlerGetFriendDevelopmentInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetFriendDevelopmentInfoCsReq.Parser.ParseFrom(data);
        var uid = req.Uid;

        // get data
        var recordData = DatabaseHelper.Instance!.GetInstance<FriendRecordData>((int)uid);
        if (recordData == null)
        {
            await connection.SendPacket(new PacketGetFriendDevelopmentInfoScRsp(Retcode.RetFriendPlayerNotFound));
            return;
        }

        await connection.SendPacket(new PacketGetFriendDevelopmentInfoScRsp(recordData));
    }
}