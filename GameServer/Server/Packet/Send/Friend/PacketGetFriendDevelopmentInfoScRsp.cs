using March7thHoney.Database.Friend;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendDevelopmentInfoScRsp : BasePacket
{
    public PacketGetFriendDevelopmentInfoScRsp(Retcode code) : base(CmdIds.GetFriendDevelopmentInfoScRsp)
    {
        var proto = new GetFriendDevelopmentInfoScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }

    public PacketGetFriendDevelopmentInfoScRsp(FriendRecordData data) : base(CmdIds.GetFriendDevelopmentInfoScRsp)
    {
        foreach (var friendDevelopmentInfoPb in data.DevelopmentInfos.ToArray())
            if (Extensions.GetUnixSec() - friendDevelopmentInfoPb.Time >=
                TimeSpan.TicksPerDay * 7 / TimeSpan.TicksPerSecond)
                data.DevelopmentInfos.Remove(friendDevelopmentInfoPb);

        var proto = new GetFriendDevelopmentInfoScRsp
        {
            FriendDevelopmentData = { data.DevelopmentInfos.Select(x => x.ToProto()) },
            Uid = (uint)data.Uid
        };

        SetData(proto);
    }
}

