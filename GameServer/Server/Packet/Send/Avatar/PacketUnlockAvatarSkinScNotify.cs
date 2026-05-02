using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Avatar;

public class PacketUnlockAvatarSkinScNotify : BasePacket
{
    public PacketUnlockAvatarSkinScNotify(int skinId) : base(CmdIds.UnlockAvatarSkinScNotify)
    {
        var proto = new UnlockAvatarSkinScNotify
        {
            SkinId = (uint)skinId
        };

        SetData(proto);
    }
}