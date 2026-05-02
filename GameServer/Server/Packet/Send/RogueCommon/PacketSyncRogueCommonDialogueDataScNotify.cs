using CastoriceHoney.Server.GameServer.Game.Rogue.Event;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

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