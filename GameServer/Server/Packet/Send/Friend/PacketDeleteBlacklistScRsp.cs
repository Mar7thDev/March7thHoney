using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketDeleteBlacklistScRsp : BasePacket
{
    public PacketDeleteBlacklistScRsp(uint uid) : base(CmdIds.DeleteBlacklistScRsp)
    {
        var proto = new DeleteBlacklistScRsp
        {
            Uid = uid
        };

        SetData(proto);
    }
}