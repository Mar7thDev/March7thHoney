using CastoriceHoney.Server.Database;
using CastoriceHoney.Server.Database.Avatar;
using CastoriceHoney.Server.Database.Challenge;
using CastoriceHoney.Server.Database.Friend;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendBattleRecordDetailCsReq)]
public class HandlerGetFriendBattleRecordDetailCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetFriendBattleRecordDetailCsReq.Parser.ParseFrom(data);
        var uid = req.Uid;

        // get data from db
        var recordData = DatabaseHelper.Instance!.GetInstance<FriendRecordData>((int)uid);
        var challengeData = DatabaseHelper.Instance!.GetInstance<ChallengeData>((int)uid);
        var avatarData = DatabaseHelper.Instance!.GetInstance<AvatarData>((int)uid);

        if (recordData == null || challengeData == null || avatarData == null)
        {
            await connection.SendPacket(new PacketGetFriendBattleRecordDetailScRsp(Retcode.RetFriendPlayerNotFound));
            return;
        }

        await connection.SendPacket(new PacketGetFriendBattleRecordDetailScRsp(recordData, challengeData, avatarData));
    }
}