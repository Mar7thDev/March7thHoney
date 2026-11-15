using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Avatar;

public class PacketAddAvatarScNotify : BasePacket
{
    public PacketAddAvatarScNotify(int avatarId, bool isGacha) : this(avatarId, isGacha, true, null)
    {
    }

    public PacketAddAvatarScNotify(int avatarId, bool isGacha, bool isNew, ItemList? reward)
        : base(CmdIds.AddAvatarScNotify)
    {
        var packet = new AddAvatarScNotify
        {
            BaseAvatarId = (uint)avatarId,
            IsNew = isNew,
            Src = isGacha ? AddAvatarSrcState.AddAvatarSrcGacha : AddAvatarSrcState.AddAvatarSrcNone
        };
        if (reward != null) packet.Reward = reward;

        SetData(packet);
    }
}
