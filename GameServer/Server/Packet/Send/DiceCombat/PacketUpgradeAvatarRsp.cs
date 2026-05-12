using March7thHoney.GameServer.Game.Activity.Activities.DiceCombat;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.DiceCombat;

public class PacketUpgradeAvatarRsp : BasePacket
{
    public PacketUpgradeAvatarRsp(DiceCombatActivityInstance instance, uint avatarId) : base(CmdIds.UpgradeAvatarRsp)
    {
        var state = instance.GetOrCreateAvatar(avatarId);
        SetData(new UpgradeAvatarRsp
        {
            Retcode = 0,
            OJCBGGOEEIE = instance.BuildAvatarProto(avatarId, state)
        });
    }
}
