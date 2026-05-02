using CastoriceHoney.Server.Database.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketSearchPlayerScRsp : BasePacket
{
    public PacketSearchPlayerScRsp() : base(CmdIds.SearchPlayerScRsp)
    {
        var proto = new SearchPlayerScRsp
        {
            Retcode = 3612
        };

        SetData(proto);
    }

    public PacketSearchPlayerScRsp(List<PlayerData> data) : base(CmdIds.SearchPlayerScRsp)
    {
        var proto = new SearchPlayerScRsp();

        proto.ResultUidList.AddRange(data.Select(x => (uint)x.Uid));
        proto.SimpleInfoList.AddRange(data.Select(x => x.ToSimpleProto(FriendOnlineStatus.Online)));

        SetData(proto);
    }
}