using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketAvatarPathChangedNotify : BasePacket
{
    public PacketAvatarPathChangedNotify(uint baseAvatarId, MultiPathAvatarType type) : base(
        CmdIds.AvatarPathChangedNotify)
    {
        var proto = new AvatarPathChangedNotify
        {
            BaseAvatarId = baseAvatarId,
            CurMultiPathAvatarType = type
        };

        SetData(proto);
    }
}