using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketSetAvatarPathScRsp : BasePacket
{
    public PacketSetAvatarPathScRsp(int avatarId) : base(CmdIds.SetAvatarPathScRsp)
    {
        var proto = new SetAvatarPathScRsp
        {
            AvatarId = (MultiPathAvatarType)avatarId
        };

        SetData(proto);
    }
}