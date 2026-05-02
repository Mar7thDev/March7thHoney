using March7thHoney.GameServer.Game.Rogue.Event;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueCommon;

public class PacketSyncRogueCommonDialogueDataScNotify : BasePacket
{
    public PacketSyncRogueCommonDialogueDataScNotify(List<RogueEventInstance> rogueEvent) : base(
        CmdIds.SyncRogueCommonDialogueDataScNotify)
    {
        var proto = new SyncRogueCommonDialogueDataScNotify();

        proto.DialogueDataList.AddRange(rogueEvent.Select(x => x.ToProto()));

        SetData(proto);
    }
}