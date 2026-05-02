using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.Database.Avatar;
using CastoriceHoney.Server.Database.Challenge;
using CastoriceHoney.Server.Database.Friend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendBattleRecordDetailScRsp : BasePacket
{
    public PacketGetFriendBattleRecordDetailScRsp(FriendRecordData recordData, ChallengeData challengeData,
        AvatarData avatarData) : base(
        CmdIds.GetFriendBattleRecordDetailScRsp)
    {
        var proto = new GetFriendBattleRecordDetailScRsp
        {
            Uid = (uint)recordData.Uid
        };

        SetData(proto);
    }

    public PacketGetFriendBattleRecordDetailScRsp(Retcode code) : base(CmdIds.GetFriendBattleRecordDetailScRsp)
    {
        var proto = new GetFriendBattleRecordDetailScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}

