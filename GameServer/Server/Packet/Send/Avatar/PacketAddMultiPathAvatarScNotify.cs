using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Avatar;

public class PacketAddMultiPathAvatarScNotify : BasePacket
{
    public PacketAddMultiPathAvatarScNotify(int avatarId) : base(CmdIds.AddMultiPathAvatarScNotify)
    {
        SetData(new AddMultiPathAvatarScNotify
        {
            AvatarId = (uint)avatarId,
            IsNew = true
        });
    }
}
