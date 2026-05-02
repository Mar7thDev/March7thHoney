using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

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