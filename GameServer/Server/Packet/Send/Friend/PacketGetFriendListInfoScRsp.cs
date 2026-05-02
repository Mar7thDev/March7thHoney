using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendListInfoScRsp : BasePacket
{
    public PacketGetFriendListInfoScRsp(Connection connection) : base(CmdIds.GetFriendListInfoScRsp)
    {
        SetData(connection.Player!.FriendManager!.ToProto());
    }
}