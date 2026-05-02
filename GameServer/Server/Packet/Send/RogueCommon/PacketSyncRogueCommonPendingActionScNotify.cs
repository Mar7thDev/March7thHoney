using March7thHoney.Enums.Rogue;
using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueCommon;

public class PacketSyncRogueCommonPendingActionScNotify : BasePacket
{
    public PacketSyncRogueCommonPendingActionScNotify(RogueActionInstance actionInstance, RogueSubModeEnum rogueSubmode)
        : base(
            CmdIds.SyncRogueCommonPendingActionScNotify)
    {
        var proto = new SyncRogueCommonPendingActionScNotify
        {
            Action = actionInstance.ToProto(),
            RogueSubMode = (uint)rogueSubmode
        };

        SetData(proto);
    }
}