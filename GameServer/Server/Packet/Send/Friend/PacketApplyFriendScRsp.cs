using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketApplyFriendScRsp : BasePacket
{
    public PacketApplyFriendScRsp(Retcode ret, uint uid) : base(CmdIds.ApplyFriendScRsp)
    {
        var proto = new ApplyFriendScRsp
        {
            Retcode = (uint)ret,
            Uid = uid
        };

        SetData(proto);
    }
}