using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketSetFriendMarkScRsp : BasePacket
{
    public PacketSetFriendMarkScRsp(uint uid, bool isMark) : base(CmdIds.SetFriendMarkScRsp)
    {
        var proto = new SetFriendMarkScRsp
        {
            Uid = uid,
            KBNKNAHGPHG = isMark
        };

        SetData(proto);
    }
}

