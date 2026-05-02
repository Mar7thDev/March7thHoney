using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketDeleteFriendScRsp : BasePacket
{
    public PacketDeleteFriendScRsp() : base(CmdIds.DeleteFriendScRsp)
    {
        var proto = new DeleteFriendScRsp();

        SetData(proto);
    }

    public PacketDeleteFriendScRsp(uint uid) : base(CmdIds.DeleteFriendScRsp)
    {
        var proto = new DeleteFriendScRsp
        {
            Uid = uid
        };

        SetData(proto);
    }
}