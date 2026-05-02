using CastoriceHoney.Server.Enums.Rogue;
using CastoriceHoney.Server.GameServer.Game.Rogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

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