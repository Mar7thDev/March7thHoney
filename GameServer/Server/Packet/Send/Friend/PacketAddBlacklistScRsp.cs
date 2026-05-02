using CastoriceHoney.Server.Database.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketAddBlacklistScRsp : BasePacket
{
    public PacketAddBlacklistScRsp() : base(CmdIds.AddBlacklistScRsp)
    {
        var proto = new AddBlacklistScRsp();

        SetData(proto);
    }

    public PacketAddBlacklistScRsp(PlayerData player) : base(CmdIds.AddBlacklistScRsp)
    {
        var status = Listener.GetActiveConnection(player.Uid) == null
            ? FriendOnlineStatus.Offline
            : FriendOnlineStatus.Online;

        var proto = new AddBlacklistScRsp
        {
            BlackInfo = player.ToSimpleProto(status)
        };

        SetData(proto);
    }
}