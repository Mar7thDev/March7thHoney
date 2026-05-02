using CastoriceHoney.Server.Database.Avatar;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Avatar;

public class PacketMarkAvatarScRsp : BasePacket
{
    public PacketMarkAvatarScRsp(FormalAvatarInfo avatar) : base(CmdIds.MarkAvatarScRsp)
    {
        var proto = new MarkAvatarScRsp
        {
            AvatarId = (uint)avatar.AvatarId,
            IsMarked = avatar.IsMarked
        };

        SetData(proto);
    }
}