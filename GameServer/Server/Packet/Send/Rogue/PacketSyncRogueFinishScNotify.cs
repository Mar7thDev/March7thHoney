using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueFinishScNotify : BasePacket
{
    public PacketSyncRogueFinishScNotify(RogueFinishInfo info) : base(CmdIds.SyncRogueFinishScNotify)
    {
        var proto = new SyncRogueFinishScNotify
        {
            RogueFinishInfo = info
        };

        SetData(proto);
    }
}