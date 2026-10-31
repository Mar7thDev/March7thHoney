using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Avatar;

public class PacketAddMultiPathAvatarScNotify : BasePacket
{
    public PacketAddMultiPathAvatarScNotify(int avatarId) : base(CmdIds.AddMultiPathAvatarScNotify)
    {
        // GCALLOJPPIJ_BDEFGIPPKNM (1) is the story-unlock source on the official wire.
        var proto = new AddMultiPathAvatarScNotify
        {
            AvatarId = (uint)avatarId,
            IsNew = true,
            ALKDBJEGAGG = (GCALLOJPPIJ)1
        };

        SetData(proto);
    }
}
