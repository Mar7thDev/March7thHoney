using CastoriceHoney.Server.Database;
using CastoriceHoney.Server.Database.Friend;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Friend;

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