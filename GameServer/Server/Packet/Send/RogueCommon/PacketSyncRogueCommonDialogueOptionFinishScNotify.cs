using March7thHoney.GameServer.Game.Rogue.Event;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueCommon;

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