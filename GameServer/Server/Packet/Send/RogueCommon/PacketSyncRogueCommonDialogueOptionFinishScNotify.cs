using CastoriceHoney.Server.GameServer.Game.Rogue.Event;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

public class PacketSyncRogueCommonDialogueOptionFinishScNotify : BasePacket
{
    public PacketSyncRogueCommonDialogueOptionFinishScNotify(RogueEventInstance instance) : base(
        CmdIds.SyncRogueCommonDialogueOptionFinishScNotify)
    {
        var proto = new SyncRogueCommonDialogueOptionFinishScNotify
        {
            EventUniqueId = (uint)instance.EventUniqueId,
            OptionId = (uint)instance.SelectedOptionId,
            ResultOptionInfo = instance.Options.Find(o => o.OptionId == instance.SelectedOptionId)!.ToProto()
        };

        SetData(proto);
    }
}